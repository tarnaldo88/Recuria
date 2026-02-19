using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Entities
{
    public sealed class StripeWebhookEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StripeEventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime ReceivedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
