using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Recuria.Infrastructure.Persistence;

namespace Recuria.Api.Health;

public sealed class PendingMigrationsHealthCheck : IHealthCheck
{
    private readonly RecuriaDbContext _db;

    public PendingMigrationsHealthCheck(RecuriaDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var pending = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            return HealthCheckResult.Unhealthy($"Pending migrations: {string.Join(", ", pending)}");
        }

        return HealthCheckResult.Healthy("No pending migrations.");
    }
}
