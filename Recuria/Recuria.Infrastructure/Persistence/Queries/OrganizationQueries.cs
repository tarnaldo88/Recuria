using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Contracts.Subscription;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Infrastructure.Persistence.Queries.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Queries
{
    internal sealed class OrganizationQueries : IOrganizationQueries
    {
        private readonly RecuriaDbContext _db;

        public OrganizationQueries(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ExistsAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _db.Organizations
                .AnyAsync(o => o.Id == id, cancellationToken);
        }

        public Task<OrganizationSummaryDto?> GetAsync(Guid organizationId, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<OrganizationDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _db.Organizations
                .Where(o => o.Id == id)
                .Select(o => new OrganizationDto
                {
                    Id = o.Id,
                    Name = o.Name,

                    OwnerEmail = o.Users
                        .Where(u => u.Role == UserRole.Owner)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? string.Empty,

                    UserCount = o.Users.Count,

                    ActiveSubscription = o.Subscriptions
                        .Where(s => s.Status == SubscriptionStatus.Active)
                        .Select(s => new SubscriptionDto(
                            s.Id,
                            s.Plan,
                            s.Status,
                            s.PeriodStart,
                            s.PeriodEnd,
                            s.IsTrial,
                            s.Status == SubscriptionStatus.PastDue))
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<OrganizationDto?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await _db.Organizations
                .Where(o => o.Users.Any(u => u.Id == userId))
                .Select(o => new OrganizationDto
                {
                    Id = o.Id,
                    Name = o.Name,

                    OwnerEmail = o.Users
                        .Where(u => u.Role == UserRole.Owner)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? string.Empty,

                    UserCount = o.Users.Count,

                    ActiveSubscription = o.Subscriptions
                        .Where(s => s.Status == SubscriptionStatus.Active)
                        .Select(s => new SubscriptionDto(
                            s.Id,
                            s.Plan,
                            s.Status,
                            s.PeriodStart,
                            s.PeriodEnd,
                            s.IsTrial,
                            s.Status == SubscriptionStatus.PastDue))
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

}
