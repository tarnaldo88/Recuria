using Recuria.Application.Contracts.Organizations;
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

        public async Task<OrganizationSummaryDto?> GetAsync(
            Guid organizationId,
            CancellationToken ct)
        {
            return await _db.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => new OrganizationSummaryDto(
                    o.Id,
                    o.Name,
                    o.Users.Count,
                    o.Subscriptions
                        .OrderByDescending(s => s.CreatedOnUtc)
                        .Select(s => s.Status.ToString())
                        .FirstOrDefault() ?? "None"
                ))
                .FirstOrDefaultAsync(ct);
        }
    }
}
