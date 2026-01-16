using Recuria.Application.Contracts.Common;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoices;
        private readonly ISubscriptionRepository _subscriptions;

        public InvoiceService(
            IInvoiceRepository invoices,
            ISubscriptionRepository subscriptions)
        {
            _invoices = invoices;
            _subscriptions = subscriptions;
        }

        public async Task<Guid> CreateInvoiceAsync(
            Guid subscriptionId,
            MoneyDto amount,
            CancellationToken ct)
        {
            var subscription =
                await _subscriptions.GetByIdAsync(subscriptionId, ct);

            if (subscription == null)
                throw new InvalidOperationException("Subscription not found");

            var invoice = new Invoice(
                subscriptionId,
                amount.Amount);

            await _invoices.AddAsync(invoice, ct);

            return invoice.Id;
        }

        public async Task MarkPaidAsync(
            Guid invoiceId,
            CancellationToken ct)
        {
            var invoice = await _invoices.GetByIdAsync(invoiceId, ct);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found");

            invoice.MarkAsPaid();

            await _invoices.SaveChangesAsync(ct);
        }
    }

}
