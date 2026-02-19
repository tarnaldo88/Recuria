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
    }
}
