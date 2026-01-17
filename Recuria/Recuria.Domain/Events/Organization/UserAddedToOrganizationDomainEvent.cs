using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events.Organization
{
    public sealed class UserAddedToOrganizationDomainEvent : IDomainEvent
    {
        public Guid OrganizationId { get; }
        public Guid UserId { get; }
        public UserRole Role { get; }

        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();

        public UserAddedToOrganizationDomainEvent(
            Guid organizationId,
            Guid userId,
            UserRole role)
        {
            OrganizationId = organizationId;
            UserId = userId;
            Role = role;
        }
    }
}
