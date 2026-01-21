using Recuria.Application.Interface;
using Recuria.Application.Interface.Idempotency;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Subscriptions
{
    public sealed class SubscriptionExpiredHandler : IDomainEventHandler<SubscriptionExpiredDomainEvent>
    {
        private readonly IProcessedEventStore _store;

        public SubscriptionExpiredHandler(IProcessedEventStore store)
        {
            _store = store;
        }        

        public async Task HandleAsync(SubscriptionExpiredDomainEvent @evt, CancellationToken cancellationToken)
        {
            var handlerName = nameof(SubscriptionExpiredHandler);

            if (await _store.ExistsAsync(evt.EventId, handlerName, cancellationToken))
                return;

            // Side effects here
            // - send email
            // - publish integration event
            // - provision tenant

            await _store.MarkProcessedAsync(evt.EventId, handlerName, cancellationToken);
            return;
        }
    }
}
