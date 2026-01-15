using Recuria.Application.Contracts.Organizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Queries.Interface
{
    public interface IOrganizationQueries
    {
        Task<OrganizationSummaryDto?> GetAsync( Guid organizationId, CancellationToken ct);
        Orga GetByIdAsync
    }
}
