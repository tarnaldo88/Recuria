using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Infrastructure.Persistence;

namespace Recuria.Infrastructure.Repositories
{
    public sealed class FeatureFlagRepository : IFeatureFlagRepository
    {
        private readonly RecuriaDbContext _db;

        public FeatureFlagRepository(RecuriaDbContext db)
        {
            _db = db;
        }

        public Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _db.FeatureFlags.FirstOrDefaultAsync(f => f.Id == id, ct);

        public Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken ct = default) =>
            _db.FeatureFlags.FirstOrDefaultAsync(f => f.Name == name, ct);

        public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.FeatureFlags
                .OrderBy(f => f.Name)
                .ToListAsync(ct);
        }

        public async Task AddAsync(FeatureFlag flag, CancellationToken ct = default)
        {
            await _db.FeatureFlags.AddAsync(flag, ct);
        }

        public void Update(FeatureFlag flag)
        {
            _db.FeatureFlags.Update(flag);
        }

        public void Remove(FeatureFlag flag)
        {
            _db.FeatureFlags.Remove(flag);
        }
    }
}
