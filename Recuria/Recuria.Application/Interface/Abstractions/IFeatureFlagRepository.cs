using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Recuria.Application.Interface.Abstractions
{
    public interface IFeatureFlagRepository
    {
        Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<IReadOnlyList<FeatureFlag>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(FeatureFlag flag, CancellationToken ct = default);
        void Update(FeatureFlag flag);
        void Remove(FeatureFlag flag);
    }
}
