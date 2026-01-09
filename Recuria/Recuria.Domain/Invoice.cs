using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public class Invoice
    {
        public Guid Id { get; init; }
        public Guid SubscriptionId { get; private set; }
        public Subscription Subscription { get; private set; } = null!;

        public DateTime InvoiceDate { get; private set; } = DateTime.UtcNow;
        public decimal Amount { get; private set; }
        public bool Paid { get; private set; }

        public Invoice(Guid subscriptionId, decimal amount)
        {
            SubscriptionId = subscriptionId;
            Amount = amount;
            Paid = false;
        }

        public void MarkAsPaid()
        {
            Paid = true;
        }
    }
}
