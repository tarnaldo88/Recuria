using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain;

namespace Recuria.Application
{
    public interface IInvoiceRepository
    {
        Task AddAsync(Invoice invoice);
        Task<IReadOnlyList<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId);
    }
}
