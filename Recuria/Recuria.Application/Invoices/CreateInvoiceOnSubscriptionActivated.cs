using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain.Events.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Invoices
{
    public sealed class CreateInvoiceOnSubscriptionActivated : IDomainEventHandler<SubscriptionActivatedDomainEvent>
    {
        private readonly IInvoiceService _invoices;
        private readonly ISubscriptionRepository _subscriptions;

        public CreateInvoiceOnSubscriptionActivated(
            IInvoiceService invoices,
            ISubscriptionRepository subscriptions)
        {
            _invoices = invoices;
            _subscriptions = subscriptions;
        }

        public async Task HandleAsync(
            SubscriptionActivatedDomainEvent @event,
            CancellationToken ct)
        {
            var subscription =
                await _subscriptions.GetByIdAsync(@event.SubscriptionId, ct);

            if (subscription == null)
                return;

            await _invoices.GenerateFirstInvoice(subscription, ct);
        }
    }
}
