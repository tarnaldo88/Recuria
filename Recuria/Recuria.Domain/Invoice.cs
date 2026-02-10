using Recuria.Domain.Entities;

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
        public DateTime? PaidOnUtc { get; private set; }
        public string InvoiceNumber { get; private set; }
        public string? Description { get; private set; }

        public Invoice(Guid subscriptionId, decimal amount, string? description = null)
        {
            SubscriptionId = subscriptionId;
            Amount = amount;
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            Paid = false;
            PaidOnUtc = null;
            InvoiceNumber = string.Empty;
        }

        public void MarkAsPaid(DateTime? paidOnUtc = null)
        {
            if (Paid)
                return;

            Paid = true;
            PaidOnUtc = (paidOnUtc ?? DateTime.UtcNow).ToUniversalTime();
        }
    }
}
