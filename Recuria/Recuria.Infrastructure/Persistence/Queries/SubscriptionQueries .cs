using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
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
                    s.Status,
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

        public async Task<Organization> GetDomainAsync(Guid organizationId)
        {
            var org = await _db.Organizations
                .Include(o => o.Users)
                .Include(o => o.Subscriptions)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            return org;
        }

        public async Task<Subscription> GetDomainByIdAsync(Guid subscriptionId)
        {
            var subscription = await _db.Subscriptions
                .Include(s => s.Organization)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found.");

            return subscription;
        }
    }
}
