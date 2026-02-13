using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recuria.Api.Auth;
using Recuria.Api.Logging;
using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Invoice;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Infrastructure.Persistence;
using Microsoft.Extensions.Primitives;
using Recuria.Application.Interface.Idempotency;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Recuria.Api.Configuration;


namespace Recuria.Api.Invoices
{
    /// <summary>
    /// Invoice endpoints.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "MemberOrAbove")]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceQueries _invoiceQueries;
        private readonly ISubscriptionQueries _subscriptionQueries;
        private readonly IInvoiceService _invoiceService;
        private readonly IAuditLogger _audit;
        private readonly RecuriaDbContext _db;
        private readonly IApiIdempotencyStore _idempotency;
        private readonly IdempotencyOptions _idempotencyOptions;

        public InvoiceController(
            IInvoiceQueries invoiceQueries,
            ISubscriptionQueries subscriptionQueries,
            IInvoiceService invoiceService,
            IAuditLogger audit,
            RecuriaDbContext db,
            IApiIdempotencyStore idempotency,
            IOptions<IdempotencyOptions> idempotencyOptions)
        {
            _invoiceQueries = invoiceQueries;
            _subscriptionQueries = subscriptionQueries;
            _invoiceService = invoiceService;
            _audit = audit;
            _db = db;
            _idempotency = idempotency;
            _idempotencyOptions = idempotencyOptions.Value;
        }

        /// <summary>
        /// Create an invoice for the organization's current subscription.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOrOwner")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateInvoiceRequest request,
            CancellationToken ct)
        {
            if (!IsSameOrganization(request.OrganizationId))
                return Forbid();

            if (!Request.Headers.TryGetValue("Idempotency-Key", out StringValues headerValue) ||
                StringValues.IsNullOrEmpty(headerValue))
            {
                return BadRequest("Idempotency-Key header is required.");
            }

            var idemKey = headerValue.ToString().Trim();
            if (idemKey.Length is < 8 or > 120)
                return BadRequest("Idempotency-Key must be between 8 and 120 characters.");

            const string operation = "invoice.create";
            var requestHash = ComputeInvoiceCreateHash(request);

            var existing = await _idempotency.GetAsync(request.OrganizationId, operation, idemKey, ct);
            if (existing is not null)
            {
                var ttl = TimeSpan.FromHours(_idempotencyOptions.InvoiceCreateTtlHours);
                var isExpired = DateTime.UtcNow - existing.CreatedOnUtc > ttl;

                if (isExpired)
                {
                    await _idempotency.DeleteAsync(request.OrganizationId, operation, idemKey, ct);
                }
                else
                {
                    if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                        return Conflict("Idempotency-Key was already used with a different request payload.");

                    return Ok(existing.ResourceId);
                }
            }

            var current = await _subscriptionQueries.GetCurrentAsync(request.OrganizationId, ct);
            if (current == null)
                return BadRequest("No active subscription for organization.");

            var invoiceId = await _invoiceService.CreateInvoiceAsync(
                current.Subscription.Id,
                new MoneyDto(request.Amount, "USD"),
                request.Description,
                ct);

            try
            {
                await _idempotency.SaveAsync(
                    request.OrganizationId,
                    operation,
                    idemKey,
                    requestHash,
                    invoiceId,
                    ct);
            }
            catch (DbUpdateException)
            {
                // Handles race condition where another request inserted same key first
                var raced = await _idempotency.GetAsync(request.OrganizationId, operation, idemKey, ct);
                if (raced is not null && string.Equals(raced.RequestHash, requestHash, StringComparison.Ordinal))
                    return Ok(raced.ResourceId);

                throw;
            }

            _audit.Log(HttpContext, "invoice.create", new
            {
                organizationId = request.OrganizationId,
                amount = request.Amount,
                description = request.Description,
                invoiceId,
                idempotencyKey = idemKey
            });

            return CreatedAtAction(nameof(GetDetails), new { invoiceId }, invoiceId);
        }

        /// <summary>
        /// Mark an invoice as paid.
        /// </summary>
        [HttpPost("{invoiceId:guid}/pay")]
        [Authorize(Policy = "AdminOrOwner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkPaid(Guid invoiceId, CancellationToken ct)
        {
            var orgId = await GetOrganizationIdForInvoiceAsync(invoiceId, ct);
            if (orgId == null)
                return NotFound();

            if (!IsSameOrganization(orgId.Value))
                return Forbid();

            await _invoiceService.MarkPaidAsync(invoiceId, ct);

            _audit.Log(HttpContext, "invoice.mark_paid", new
            {
                invoiceId,
                organizationId = orgId.Value
            });

            return NoContent();
        }

        /// <summary>
        /// List invoices for an organization.
        /// </summary>
        [HttpGet("organization/{organizationId:guid}")]
        public async Task<ActionResult<IReadOnlyList<InvoiceListItemDto>>> GetForOrganization(
            Guid organizationId,
            CancellationToken ct)
        {
            if (!IsSameOrganization(organizationId))
                return Forbid();

            var invoices = await _invoiceQueries.GetForOrganizationAsync(organizationId, ct);
            return Ok(invoices);
        }

        /// <summary>
        /// Get invoice details.
        /// </summary>
        [HttpGet("{invoiceId:guid}")]
        public async Task<ActionResult<InvoiceDetailsDto>> GetDetails(
            Guid invoiceId,
            CancellationToken ct)
        {
            var orgId = await GetOrganizationIdForInvoiceAsync(invoiceId, ct);
            if (orgId == null)
                return NotFound();

            if (!IsSameOrganization(orgId.Value))
                return Forbid();

            var invoice = await _invoiceQueries.GetDetailsAsync(invoiceId, ct);
            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }

        private async Task<Guid?> GetOrganizationIdForInvoiceAsync(Guid invoiceId, CancellationToken ct)
        {
            var orgId = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Id == invoiceId)
                .Select(i => i.Subscription.OrganizationId)
                .FirstOrDefaultAsync(ct);

            return orgId == Guid.Empty ? null : orgId;
        }

        private bool IsSameOrganization(Guid organizationId)
        {
            return User.IsInOrganization(organizationId);
        }
        
        private static string ComputeInvoiceCreateHash(CreateInvoiceRequest request)
        {
            var canonical = $"{request.OrganizationId:N}|{request.Amount:0.00}|{(request.Description ?? string.Empty).Trim()}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes);
        }
    }
}
