using Recuria.Application.Interface;
using Recuria.Application.Interface.Idempotency;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events;
using Recuria.Domain.Events.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Subscriptions
{
    public sealed class SubscriptionActivatedHandler : IDomainEventHandler<SubscriptionActivatedDomainEvent>
    {
        private readonly IProcessedEventStore _processedEvents;

        public SubscriptionActivatedHandler(IProcessedEventStore processedEvents)
        {
            _processedEvents = processedEvents;
        }

        public async Task HandleAsync(
            SubscriptionActivatedDomainEvent evt,
            CancellationToken ct)
        {
            if (await _processedEvents.ExistsAsync(evt.EventId, ct))
                return;

            // Application-level side effects
            // - enqueue integration event
            // - provision tenant
            // - notify external systems

            await _processedEvents.MarkProcessedAsync(evt.EventId, ct);
        }
    }
}
