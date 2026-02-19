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

    public sealed class StripeWebhookInbox : IStripeWebhookInbox
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


        public Task<List<StripeWebhookInboxMessage>> GetPendingAsync(int take, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            return _db.StripeWebhookInboxMessages
                .Where(x => x.ProcessedOnUtc == null && (x.NextAttemptOnUtc == null || x.NextAttemptOnUtc <= now))
                .OrderBy(x => x.ReceivedOnUtc)
                .Take(take)
                .ToListAsync(ct);
        }

        public async Task MarkProcessedAsync(Guid id, CancellationToken ct)
        {
            var msg = await _db.StripeWebhookInboxMessages.SingleAsync(x => x.Id == id, ct);
            msg.ProcessedOnUtc = DateTime.UtcNow;
            msg.LastError = null;
            await _db.SaveChangesAsync(ct);
        }
        public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct)
        {
            var msg = await _db.StripeWebhookInboxMessages.SingleAsync(x => x.Id == id, ct);
            msg.AttemptCount += 1;
            msg.LastError = error.Length > 2000 ? error[..2000] : error;
            msg.NextAttemptOnUtc = DateTime.UtcNow.AddMinutes(Math.Min(30, Math.Pow(2, msg.AttemptCount)));
            await _db.SaveChangesAsync(ct);
        }
    }

    public sealed class StripeWebhookWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StripeWebhookWorker> _logger;

        public StripeWebhookWorker(IServiceScopeFactory scopeFactory, ILogger<StripeWebhookWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var inbox = scope.ServiceProvider.GetRequiredService<IStripeWebhookInbox>();
                    var processor = scope.ServiceProvider.GetRequiredService<IStripeWebhookProcessor>();

                    var batch = await inbox.GetPendingAsync(25, stoppingToken);
                    foreach (var item in batch)
                    {
                        try
                        {
                            var stripeEvent = EventUtility.ParseEvent(item.Payload);
                            await processor.ProcessAsync(stripeEvent, stoppingToken);
                            await inbox.MarkProcessedAsync(item.Id, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            await inbox.MarkFailedAsync(item.Id, ex.Message, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stripe webhook worker loop failed.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

}
