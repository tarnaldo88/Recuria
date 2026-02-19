using Microsoft.EntityFrameworkCore;
using Recuria.Domain.Entities;
using Recuria.Infrastructure.Persistence;
using Stripe;

namespace Recuria.Api.Payments
{
    public interface IStripeWebhookInbox
    {
        Task EnqueueAsync(string eventId, string eventType, string payload, CancellationToken ct);
        Task<List<StripeWebhookInboxMessage>> GetPendingAsync(int take, CancellationToken ct);
        Task MarkProcessedAsync(Guid id, CancellationToken ct);
        Task MarkFailedAsync(Guid id, string error, CancellationToken ct);
    }

    public class StripeWebhookInbox
    {
        private readonly RecuriaDbContext _db;

        public StripeWebhookInbox(RecuriaDbContext db) => _db = db;

        public async Task EnqueueAsync(string eventId, string eventType, string payload, CancellationToken ct)
        {
            var exists = await _db.StripeWebhookInboxMessages.AnyAsync(x => x.StripeEventId == eventId, ct);
            if (exists) return;

            _db.StripeWebhookInboxMessages.Add(new StripeWebhookInboxMessage
            {
                StripeEventId = eventId,
                EventType = eventType,
                Payload = payload,
                NextAttemptOnUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

    }
}
