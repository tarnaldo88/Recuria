using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events.Subscription
{
    public sealed class SubscriptionActivatedDomainEvent : IDomainEvent
    {
        public Guid SubscriptionId { get; }
        public Guid OrganizationId { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();

        public SubscriptionActivatedDomainEvent(Guid subscriptionId, Guid organizationId)
        {
            SubscriptionId = subscriptionId;
            OrganizationId = organizationId;
        }
    }
}
