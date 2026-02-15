using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Organizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IOrganizationQueries
    {
        Task<OrganizationSummaryDto?> GetAsync( Guid organizationId, CancellationToken ct);
        Task<OrganizationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

        Task<OrganizationDto?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> ExistsAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(Guid organizationId, CancellationToken ct);

        Task<PagedResult<UserSummaryDto>> GetUsersPagedAsync(Guid orgId, TableQuery query, CancellationToken ct);

    }
}
