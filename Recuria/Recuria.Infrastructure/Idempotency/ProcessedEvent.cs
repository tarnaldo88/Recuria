using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Idempotency
{
    public sealed class ProcessedEvent
    {
        public Guid EventId { get; private set; }
        public DateTime ProcessedOnUtc { get; private set; }

        private ProcessedEvent() { } //EF Core

        public ProcessedEvent(Guid eventId)
        {
            EventId = eventId;
            ProcessedOnUtc = DateTime.UtcNow;
        }
    }
}
