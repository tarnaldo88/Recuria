using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Invoice;
using Recuria.Application.Interface;
using Recuria.Infrastructure.Persistence;

namespace Recuria.Api.Invoices
{
    [ApiController]
    [Authorize(Policy = "MemberOrAbove")]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceQueries _invoiceQueries;
        private readonly RecuriaDbContext _db;

        public InvoiceController(IInvoiceQueries invoiceQueries, RecuriaDbContext db)
        {
            _invoiceQueries = invoiceQueries;
            _db = db;
        }

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

        [HttpGet("{invoiceId:guid}")]
        public async Task<ActionResult<InvoiceDetailsDto>> GetDetails(
            Guid invoiceId,
            CancellationToken ct)
        {
            var orgId = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Id == invoiceId)
                .Select(i => i.Subscription.OrganizationId)
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
