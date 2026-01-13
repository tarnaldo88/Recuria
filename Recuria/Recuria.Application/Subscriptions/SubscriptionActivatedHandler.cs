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
        private readonly IProcessedEventStore _store;

        public SubscriptionActivatedHandler(IProcessedEventStore store)
        {
            _store = store;
        }

        public Task HandleAsync(SubscriptionActivatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Placeholder for real behavior:
            // - Send welcome email
            // - Emit integration event
            // - Provision tenant resources

            Console.WriteLine($"Subscription {domainEvent.SubscriptionId} activated for Org {domainEvent.OrganizationId}");

            return Task.CompletedTask;
        }


        public async Task Handle(SubscriptionActivated evt, CancellationToken ct)
        {
            if (await _store.ExistsAsync(evt.Id, ct))
                return;

            // business side effects here

            await _store.MarkProcessedAsync(evt.Id, ct);
        }
    }
}
