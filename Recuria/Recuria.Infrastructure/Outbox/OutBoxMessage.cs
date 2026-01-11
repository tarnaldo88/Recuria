using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Outbox
{
    public sealed class OutBoxMessage
    {
        public Guid Id { get; init; }
        public DateTime OccurredOnUtc { get; init; }
        public string Type { get; init; } = null!;
        public string Content { get; init; } = null!;

        public DateTime? ProcessedOnUtc { get; set; }
        public string? Error { get; set; }
    }
}
