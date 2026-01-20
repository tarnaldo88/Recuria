using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events.Organization
{
    public sealed class UserRemovedFromOrganizationDomainEvent : IDomainEvent
    {
        public Guid OrganizationId { get; }
        public Guid UserId { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();

        public UserRemovedFromOrganizationDomainEvent(
            Guid organizationId,
            Guid userId)
        {
            OrganizationId = organizationId;
            UserId = userId;
        }
    }
}
