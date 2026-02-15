using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IInvoiceQueries
    {
        Task<IReadOnlyList<InvoiceListItemDto>> GetForOrganizationAsync( Guid organizationId, CancellationToken ct);

        Task<InvoiceDetailsDto?> GetDetailsAsync(Guid invoiceId, CancellationToken ct);

        Task<PagedResult<InvoiceListItemDto>> GetForOrganizationPagedAsync(Guid orgId, TableQuery query, CancellationToken ct);

    }
}
