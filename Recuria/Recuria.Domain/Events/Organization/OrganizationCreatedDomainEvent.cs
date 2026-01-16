using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events.Organization
{
    public sealed record OrganizationCreatedDomainEvent(
    Guid OrganizationId
    ) : DomainEvent;
}
