using Recuria.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IFeatureFlagService
    {
        Task<bool> IsEnabledAsync(string featureName, string? userEmail = null, string? organizationId = null);
        Task<IReadOnlyList<FeatureFlag>> GetAllAsync();
        Task<FeatureFlag?> GetByNameAsync(string name);
        Task CreateAsync(FeatureFlag flag);
        Task UpdateAsync(FeatureFlag flag);
        Task DeleteAsync(Guid id);
        Task ToggleAsync(string name, bool enabled, string modifiedBy);
    }
}