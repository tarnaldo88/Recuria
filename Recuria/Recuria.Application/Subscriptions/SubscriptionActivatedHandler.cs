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
        private readonly IProcessedEventStore _store;
        public Task HandleAsync(SubscriptionActivatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Placeholder for real behavior:
            // - Send welcome email
            // - Emit integration event
            // - Provision tenant resources

            Console.WriteLine($"Subscription {domainEvent.SubscriptionId} activated for Org {domainEvent.OrganizationId}");

            return Task.CompletedTask;
        }
       

        public async Task Handle(
            SubscriptionActivated evt,
            CancellationToken ct)
        {
            var alreadyHandled = await _db.ProcessedEvents
                .AnyAsync(x => x.EventId == evt.Id, ct);

            if (alreadyHandled)
                return;

            // perform side effects safely here

            _db.ProcessedEvents.Add(
                new ProcessedEvent(evt.Id));

            await _db.SaveChangesAsync(ct);
        }
    }
}
