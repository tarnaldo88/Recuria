using Recuria.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Organizations
{
    public sealed record UserSummaryDto(
        Guid Id,
        string? Name,
        string? Email,
        UserRole Role,
        Guid? OrganizationId
    );
}
