using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Invoice
{
    public sealed record CurrentUserDto(
        Guid Id,
        string Email,
        string Role,
        Guid OrganizationId
    );
}
