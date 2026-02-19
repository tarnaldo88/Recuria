using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Entities
{
    public sealed class StripeWebhookInboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StripeEventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;

        public DateTime ReceivedOnUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedOnUtc { get; set; }

        public int AttemptCount { get; set; }
        public DateTime? NextAttemptOnUtc { get; set; }
        public string? LastError { get; set; }
    }
}
