using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events.Organization
{
    public sealed class UserRoleChangedDomainEvent : IDomainEvent
    {
        public Guid OrganizationId { get; }
        public Guid UserId { get; }
        public UserRole OldRole { get; }
        public UserRole NewRole { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();

        public UserRoleChangedDomainEvent(
            Guid organizationId,
            Guid userId,
            UserRole oldRole,
            UserRole newRole)
        {
            OrganizationId = organizationId;
            UserId = userId;
            OldRole = oldRole;
            NewRole = newRole;
        }
    }
}
