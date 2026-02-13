using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface.Idempotency
{
    public sealed record ApiIdempotencyHit(Guid ResourceId, string RequestHash);

    public interface IApiIdempotencyStore
    {
        Task<ApiIdempotencyHit?> GetAsync(Guid organizationId, string operation, string key, CancellationToken ct);

        Task SaveAsync(
            Guid organizationId,
            string operation,
            string key,
            string requestHash,
            Guid resourceId,
            CancellationToken ct);

        Task DeleteAsync(Guid organizationId, string operation, string key, CancellationToken ct);

        Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct);
    }
}
