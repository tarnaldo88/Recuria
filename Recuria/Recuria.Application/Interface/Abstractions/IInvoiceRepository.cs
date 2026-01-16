using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain;

namespace Recuria.Application.Interface.Abstractions
{
    public interface IInvoiceRepository
    {
        Task AddAsync(Invoice invoice, CancellationToken ct);
        Task<IReadOnlyList<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId);
        Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
