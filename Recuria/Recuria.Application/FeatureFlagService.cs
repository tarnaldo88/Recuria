using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Recuria.Application.Interface;
using Recuria.Domain;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly RecuriaDbContext _db;
        private readonly ILogger<FeatureFlagService> _logger;
        private readonly IWebHostEnvironment _env;

        public FeatureFlagService(
            RecuriaDbContext db,
            ILogger<FeatureFlagService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
        }

        public async Task<bool> IsEnabledAsync(string featureName, string? userEmail = null, string? organizationId = null)
        {
            var flag = await _db.FeatureFlags
                .FirstOrDefaultAsync(f => f.Name == featureName);

            if (flag == null)
                return false;

            // Check environment
            if (flag.Environment != "all" && flag.Environment != _env.EnvironmentName)
                return false;

            // Check date range
            var now = DateTime.UtcNow;
            if (flag.StartDate.HasValue && now < flag.StartDate.Value)
                return false;
            if (flag.EndDate.HasValue && now > flag.EndDate.Value)
                return false;

            // If globally enabled
            if (flag.IsEnabled && string.IsNullOrEmpty(flag.EnabledFor))
                return true;

            // Check specific users/orgs
            if (!string.IsNullOrEmpty(flag.EnabledFor))
            {
                var enabledFor = JsonSerializer.Deserialize<List<string>>(flag.EnabledFor) ?? new List<string>();
                
                if (userEmail != null && enabledFor.Contains(userEmail))
                    return true;
                if (organizationId != null && enabledFor.Contains($"org:{organizationId}"))
                    return true;
            }

            return false;
        }

        public async Task<IReadOnlyList<FeatureFlag>> GetAllAsync()
        {
            return await _db.FeatureFlags
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        public async Task<FeatureFlag?> GetByNameAsync(string name)
        {
            return await _db.FeatureFlags
                .FirstOrDefaultAsync(f => f.Name == name);
        }

        public async Task CreateAsync(FeatureFlag flag)
        {
            flag.Id = Guid.NewGuid();
            flag.CreatedAt = DateTime.UtcNow;
            
            _db.FeatureFlags.Add(flag);
            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Created feature flag {FeatureName}", flag.Name);
        }

        public async Task UpdateAsync(FeatureFlag flag)
        {
            var existing = await _db.FeatureFlags.FindAsync(flag.Id);
            if (existing == null)
                throw new InvalidOperationException($"Feature flag {flag.Id} not found");

            existing.Description = flag.Description;
            existing.IsEnabled = flag.IsEnabled;
            existing.EnabledFor = flag.EnabledFor;
            existing.StartDate = flag.StartDate;
            existing.EndDate = flag.EndDate;
            existing.Environment = flag.Environment;
            existing.ModifiedAt = DateTime.UtcNow;
            
            await _db.SaveChangesAsync();
            _logger.LogInformation("Updated feature flag {FeatureName}", flag.Name);
        }

        public async Task DeleteAsync(Guid id)
        {
            var flag = await _db.FeatureFlags.FindAsync(id);
            if (flag != null)
            {
                _db.FeatureFlags.Remove(flag);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Deleted feature flag {FeatureId}", id);
            }
        }

        public async Task ToggleAsync(string name, bool enabled, string modifiedBy)
        {
            var flag = await _db.FeatureFlags
                .FirstOrDefaultAsync(f => f.Name == name);

            if (flag == null)
                throw new InvalidOperationException($"Feature flag {name} not found");

            flag.IsEnabled = enabled;
            flag.ModifiedAt = DateTime.UtcNow;
            flag.ModifiedBy = modifiedBy;
            
            await _db.SaveChangesAsync();
            
            _logger.LogInformation(
                "Feature flag {FeatureName} {Action} by {ModifiedBy}",
                name,
                enabled ? "enabled" : "disabled",
                modifiedBy);
        }
    }
}