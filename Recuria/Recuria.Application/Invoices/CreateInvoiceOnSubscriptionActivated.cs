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
        private readonly IUnitOfWork _uow;

        public CreateInvoiceOnSubscriptionActivated(
            IInvoiceService invoices,
            ISubscriptionRepository subscriptions,
            IUnitOfWork uow)
        {
            _invoices = invoices;
            _subscriptions = subscriptions;
            _uow = uow;
        }

        public async Task HandleAsync(
            SubscriptionActivatedDomainEvent @event,
            CancellationToken ct)
        {
            var subscription =
                await _subscriptions.GetByIdAsync(@event.SubscriptionId, ct);

            if (subscription == null)
                return;

            _invoices.GenerateFirstInvoice(subscription);

            await _uow.CommitAsync(ct);
        }
    }
}
