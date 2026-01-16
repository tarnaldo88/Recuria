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
        Task<bool> ExistsAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<OrganizationDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<OrganizationDto?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<OrganizationSummaryDto?> GetAsync(
            Guid organizationId,
            CancellationToken cancellationToken);
    }
}
