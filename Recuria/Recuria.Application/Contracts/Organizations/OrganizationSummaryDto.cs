using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Organizations
{
    public sealed record OrganizationSummaryDto(
        Guid Id,
        string Name,
        int UserCount,
        string SubscriptionStatus
    );
}
