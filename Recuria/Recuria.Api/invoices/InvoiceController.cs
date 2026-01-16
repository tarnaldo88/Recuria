using Microsoft.AspNetCore.Mvc;
using Recuria.Application.Contracts.Invoice;
using Recuria.Application.Interface;

namespace Recuria.Api.invoices
{
    public class InvoiceController : Controller
    {
        private readonly IInvoiceQueries _invoiceQueries;

        public InvoiceController(IInvoiceQueries invoiceQueries)
        {
            _invoiceQueries = invoiceQueries;
        }

        [HttpGet("organization/{organizationId:guid}")]
        public async Task<ActionResult<IReadOnlyList<InvoiceListItemDto>>> GetForOrganization(Guid organizationId, CancellationToken ct)
        {
            var invoices = await _invoiceQueries.GetForOrganizationAsync(organizationId, ct);
            return Ok(invoices);
        }

        [HttpGet("{invoiceId:guid}")]
        public async Task<ActionResult<InvoiceDetailsDto>> GetDetails(Guid invoiceId, CancellationToken ct)
        {
            var invoice = await _invoiceQueries.GetDetailsAsync(invoiceId, ct);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }
    }
}
