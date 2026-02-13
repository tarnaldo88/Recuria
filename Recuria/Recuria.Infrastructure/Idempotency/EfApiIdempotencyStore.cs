using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface.Idempotency;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Idempotency
{
    public sealed class EfApiIdempotencyStore : IApiIdempotencyStore
    {
        private readonly RecuriaDbContext _db;

        public EfApiIdempotencyStore(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<ApiIdempotencyHit?> GetAsync(Guid organizationId, string operation, string key, CancellationToken ct)
        {
            var row = await _db.ApiIdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.OrganizationId == organizationId &&
                    x.Operation == operation &&
                    x.IdempotencyKey == key, ct);

            return row is null ? null : new ApiIdempotencyHit(row.ResourceId, row.RequestHash);
        }

        public async Task SaveAsync(
            Guid organizationId,
            string operation,
            string key,
            string requestHash,
            Guid resourceId,
            CancellationToken ct)
        {
            _db.ApiIdempotencyRecords.Add(new ApiIdempotencyRecord(
                organizationId, operation, key, requestHash, resourceId));

            await _db.SaveChangesAsync(ct);
        }
    }
}
