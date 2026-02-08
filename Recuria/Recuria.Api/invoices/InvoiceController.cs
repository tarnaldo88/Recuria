using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Invoice;
using Recuria.Application.Contracts.Common;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Api.Logging;
using Recuria.Infrastructure.Persistence;

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

        public InvoiceController(
            IInvoiceQueries invoiceQueries,
            ISubscriptionQueries subscriptionQueries,
            IInvoiceService invoiceService,
            IAuditLogger audit,
            RecuriaDbContext db)
        {
            _invoiceQueries = invoiceQueries;
            _subscriptionQueries = subscriptionQueries;
            _invoiceService = invoiceService;
            _audit = audit;
            _db = db;
        }

        /// <summary>
        /// Create an invoice for the organization's current subscription.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOrOwner")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateInvoiceRequest request,
            CancellationToken ct)
        {
            if (!IsSameOrganization(request.OrganizationId))
                return Forbid();

            var current = await _subscriptionQueries.GetCurrentAsync(request.OrganizationId, ct);
            if (current == null)
                return BadRequest("No active subscription for organization.");

            var invoiceId = await _invoiceService.CreateInvoiceAsync(
                current.Subscription.Id,
                new MoneyDto(request.Amount, "USD"),
                ct);

            _audit.Log(HttpContext, "invoice.create", new
            {
                organizationId = request.OrganizationId,
                amount = request.Amount,
                description = request.Description,
                invoiceId
            });

            return CreatedAtAction(nameof(GetDetails), new { invoiceId }, invoiceId);
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
            var subscriptionId = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Id == invoiceId)
                .Select(i => i.SubscriptionId)
                .FirstOrDefaultAsync(ct);

            if (subscriptionId == Guid.Empty)
                return NotFound();

            var orgId = await _db.Subscriptions
                .AsNoTracking()
                .Where(s => s.Id == subscriptionId)
                .Select(s => s.OrganizationId)
                .FirstOrDefaultAsync(ct);

            if (orgId == Guid.Empty)
                return NotFound();

            if (!IsSameOrganization(orgId))
                return Forbid();

            var invoice = await _invoiceQueries.GetDetailsAsync(invoiceId, ct);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        private bool IsSameOrganization(Guid organizationId)
        {
            var orgClaim = User.FindFirst("org_id")?.Value;
            return Guid.TryParse(orgClaim, out var orgId) && orgId == organizationId;
        }
    }
}
