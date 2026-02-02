using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Entities
{
    public sealed class ProcessedEvent
    {
        public Guid Id { get; set; }
        public Guid EventId { get;  set; }
        public string Handler { get;  set; } = string.Empty;
        public DateTime ProcessedOnUtc { get;  set; }

        private ProcessedEvent() { } //EF Core

        public ProcessedEvent(Guid eventId, string handler, DateTime processed)
        {
            EventId = eventId;
            ProcessedOnUtc = DateTime.UtcNow;
            Handler = handler;
            ProcessedOnUtc = processed;
        }
    }
}
