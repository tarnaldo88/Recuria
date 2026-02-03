using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Outbox
{
    public sealed class OutBoxMessage
    {
        public const int MaxRetryCount = 10;
        public Guid Id { get; init; }
        public DateTime OccurredOnUtc { get; init; }
        public string Type { get; init; } = null!;
        public string Content { get; init; } = null!;

        public DateTime? ProcessedOnUtc { get; set; }
        public DateTime? DeadLetteredOnUtc { get; private set; }
        public string? Error { get; set; }

        public int AttemptCount { get; private set; }
        public DateTime? NextAttemptOnUtc { get; private set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryOnUtc { get; set; }
        public bool IsDeadLettered => DeadLetteredOnUtc != null;

        public static OutBoxMessage FromDomainEvent(IDomainEvent domainEvent)
        {
            return new OutBoxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = domainEvent.OccurredOn,
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = System.Text.Json.JsonSerializer.Serialize(domainEvent)
            };
        }

        public void MarkFailed(string error)
        {
            AttemptCount++;
            Error = error;
            NextAttemptOnUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, AttemptCount));
        }

        public void MarkProcessed()
        {
            ProcessedOnUtc = DateTime.UtcNow;
            Error = null;
        }

        public void MarkDeadLettered(string error)
        {
            Error = error;
            DeadLetteredOnUtc = DateTime.UtcNow;
        }

        public void Revive()
        {
            DeadLetteredOnUtc = null;
            Error = null;
            NextRetryOnUtc = DateTime.UtcNow;
        }
    }
}
