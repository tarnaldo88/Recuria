using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events.Subscription
{
    public sealed class SubscriptionActivatedIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

        public Guid SubscriptionId { get; }

        public SubscriptionActivatedIntegrationEvent(Guid subscriptionId)
        {
            SubscriptionId = subscriptionId;
        }
    }
}
