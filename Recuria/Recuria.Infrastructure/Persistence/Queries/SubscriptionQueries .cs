using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Subscription;
using Recuria.Domain.Entities;
using Recuria.Infrastructure.Persistence.Queries.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Queries
{
    internal sealed class SubscriptionQueries : ISubscriptionQueries
    {
        private readonly RecuriaDbContext _db;

        public SubscriptionQueries(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<SubscriptionDetailsDto?> GetCurrentAsync(Guid organizationId, CancellationToken ct)
        {
            return await _db.Subscriptions
            .Where(s => s.OrganizationId == organizationId)
            .Where(s => s.Status != SubscriptionStatus.Canceled)
            .OrderByDescending(s => s.PeriodEnd)
            .Select(s => new SubscriptionDetailsDto(
                new SubscriptionDto(
                    s.Id,
                    s.Plan,
                    s.Status.ToString(),
                    s.PeriodStart,
                    s.PeriodEnd,
                    s.Status == SubscriptionStatus.Trial,
                    s.Status == SubscriptionStatus.PastDue
                ),
                new SubscriptionActionAvailabilityDto(
                    s.Status == SubscriptionStatus.Trial,
                    s.Status == SubscriptionStatus.Active
                        || s.Status == SubscriptionStatus.PastDue,
                    s.Status == SubscriptionStatus.Active
                )
            ))
            .FirstOrDefaultAsync(ct);
        }
    }
}
