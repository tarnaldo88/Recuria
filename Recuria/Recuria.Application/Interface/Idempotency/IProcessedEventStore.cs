using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface.Idempotency
{
    public interface IProcessedEventStore
    {
        Task<bool> ExistsAsync(Guid eventId, string handler, CancellationToken ct);
        Task MarkProcessedAsync(Guid eventId, string handler, CancellationToken ct);
    }
}
