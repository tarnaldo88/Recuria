using Microsoft.EntityFrameworkCore;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;
using Recuria.Application.Contracts.Common;

namespace Recuria.Infrastructure.Persistence.Queries
{
    public sealed class OrganizationQueries : IOrganizationQueries
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
            return _db.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => new OrganizationSummaryDto(
                    o.Id,
                    o.Name,
                    o.Users.Count,
                    o.Subscriptions
                        .OrderByDescending(s => s.PeriodEnd)
                        .Select(s => s.Status.ToString())
                        .FirstOrDefault() ?? "None"
                ))
                .FirstOrDefaultAsync(ct);
            // throw new NotImplementedException();
        }

        public async Task<OrganizationDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await _db.Organizations
                .Where(o => o.Id == id)
                .Select(o => new OrganizationDto(
                    o.Id,
                    o.Name,
                    o.CreatedAt, // make sure you have a DateTime field for CreatedAt
                    o.Users
                        .Where(u => u.Role == UserRole.Owner)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? string.Empty,
                    o.Users.Count,
                    o.Subscriptions
                        .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                        .Select(s => new SubscriptionDto(
                            s.Id,
                            s.Plan,
                            s.Status,
                            s.PeriodStart,
                            s.PeriodEnd,
                            s.Status == SubscriptionStatus.Trial,
                            s.Status == SubscriptionStatus.PastDue))
                        .FirstOrDefault()
                ))
                .FirstOrDefaultAsync(cancellationToken);

        }

        public async Task<OrganizationDto?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return await _db.Organizations
                .Where(o => o.Id == userId)
                .Select(o => new OrganizationDto(
                    o.Id,
                    o.Name,
                    o.CreatedAt, // make sure you have a DateTime field for CreatedAt
                    o.Users
                        .Where(u => u.Role == UserRole.Owner)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? string.Empty,
                    o.Users.Count,
                    o.Subscriptions
                        .Where(s => s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                        .Select(s => new SubscriptionDto(
                            s.Id,
                            s.Plan,
                            s.Status,
                            s.PeriodStart,
                            s.PeriodEnd,
                            s.Status == SubscriptionStatus.Trial,
                            s.Status == SubscriptionStatus.PastDue))
                        .FirstOrDefault()
                ))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(Guid organizationId, CancellationToken ct)
        {
            return await _db.Users
                .Where(u => u.OrganizationId == organizationId)
                .OrderBy(u => u.Name)
                .Select(u => new UserSummaryDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.OrganizationId
                ))
                .ToListAsync(ct);
        }

        public Task<PagedResult<UserSummaryDto>> GetUsersPagedAsync(Guid orgId, TableQuery query, CancellationToken ct)
        {
            var q = _db.Users.AsNoTracking().Where(x => x.OrganizationId == orgId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(x => x.Name!.Contains(s) || x.Email!.Contains(s));
            }

            q = (query.SortBy?.ToLowerInvariant(), query.SortDir.ToLowerInvariant()) switch
            {
                ("email", "desc") => q.OrderByDescending(x => x.Email),
                ("email", _) => q.OrderBy(x => x.Email),
                ("role", "desc") => q.OrderByDescending(x => x.Role),
                ("role", _) => q.OrderBy(x => x.Role),
                ("name", "desc") => q.OrderByDescending(x => x.Name),
                _ => q.OrderBy(x => x.Name)
            };

            var total = await q.CountAsync(ct);
            var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .Select(x => new UserSummaryDto { /* map */ })
                .ToListAsync(ct);

            return new PagedResult<UserSummaryDto>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = total
            };
        }
    }

}
