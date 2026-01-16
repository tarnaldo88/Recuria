using Recuria.Application.Contracts.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Queries.Interface
{
    public interface IInvoiceQueries
    {
        Task<IReadOnlyList<InvoiceListItemDto>> GetForOrganizationAsync( Guid organizationId, CancellationToken ct);

        Task<InvoiceDetailsDto?> GetDetailsAsync(Guid invoiceId, CancellationToken ct);
    }
}
