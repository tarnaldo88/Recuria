using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure
{
    public interface IDatabaseDistributedLock
    {
        Task<bool> TryAcquireAsync(string lockName, CancellationToken ct);
        Task ReleaseAsync(string lockName, CancellationToken ct);
    }
}
