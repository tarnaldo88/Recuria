using Recuria.Application.Contracts.Common;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Validation;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;

namespace Recuria.Application
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoices;
        private readonly ISubscriptionRepository _subscriptions;
        private readonly ValidationBehavior _validator;

        public InvoiceService(
            IInvoiceRepository invoices,
            ISubscriptionRepository subscriptions,
            ValidationBehavior validator)
        {
            _invoices = invoices;
            _subscriptions = subscriptions;
            _validator = validator;
        }

        public async Task<Guid> CreateInvoiceAsync(
            Guid subscriptionId,
            MoneyDto amount,
            string? description,
            CancellationToken ct)
        {
            var subscription = await _subscriptions.GetByIdAsync(subscriptionId, ct);
            await _validator.ValidateAsync(subscription);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            var invoice = new Invoice(
                subscriptionId,
                amount.Amount,
                description);

            await _invoices.AddAsync(invoice, ct);

            return invoice.Id;
        }

        public async Task<Invoice> GenerateFirstInvoice(Subscription subscription, CancellationToken ct = default)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            decimal amount = default;

            if (subscription.Plan == PlanType.Pro)
            {
                amount = 25.0m;
            }
            else if (subscription.Plan == PlanType.Enterprise)
            {
                amount = 100m;
            }

            var invoice = new Invoice(
                subscription.Id,
                amount);

            await _invoices.AddAsync(invoice, ct);

            return invoice;
        }

        public async Task MarkPaidAsync(
            Guid invoiceId,
            CancellationToken ct)
        {
            var invoice = await _invoices.GetByIdAsync(invoiceId, ct);
            await _validator.ValidateAsync(invoice);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            invoice.MarkAsPaid();

            await _invoices.SaveChangesAsync(ct);
        }
    }
}
