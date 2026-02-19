using Microsoft.EntityFrameworkCore;
using Recuria.Infrastructure.Persistence;
using Recuria.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace Recuria.Api.Payments
{
    public interface IStripeWebhookProcessor
    {
        Task ProcessAsync(Event stripeEvent, CancellationToken ct);
    }
    public sealed class StripeWebhookProcessor : IStripeWebhookProcessor
    {
        private readonly RecuriaDbContext _db;
        private readonly ILogger<StripeWebhookProcessor> _logger;

        public StripeWebhookProcessor(RecuriaDbContext db, ILogger<StripeWebhookProcessor> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task ProcessAsync(Event stripeEvent, CancellationToken ct)
        {
            var alreadyProcessed = await _db.StripeWebhookEvents
           .AnyAsync(x => x.StripeEventId == stripeEvent.Id, ct);

            if (alreadyProcessed) return;

            _db.StripeWebhookEvents.Add(new StripeWebhookEvent
            {
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type
            });

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    {
                        var session = stripeEvent.Data.Object as Session;
                        if (session is null) break;

                        if (!session.Metadata.TryGetValue("org_id", out var orgRaw) || !Guid.TryParse(orgRaw, out var orgId))
                            break;

                        var customerId = session.CustomerId ?? string.Empty;
                        var subscriptionId = session.SubscriptionId ?? string.Empty;

                        var existing = await _db.StripeSubscriptionMaps
                            .SingleOrDefaultAsync(x => x.OrganizationId == orgId, ct);

                        if (existing is null)
                        {
                            _db.StripeSubscriptionMaps.Add(new StripeSubscriptionMap
                            {
                                OrganizationId = orgId,
                                StripeCustomerId = customerId,
                                StripeSubscriptionId = subscriptionId
                            });
                        }
                        else
                        {
                            existing.StripeCustomerId = customerId;
                            existing.StripeSubscriptionId = subscriptionId;
                            existing.UpdatedOnUtc = DateTime.UtcNow;
                        }

                        // TODO: call your subscription service to activate/upgrade local plan.
                        break;
                    }

                case "invoice.payment_succeeded":
                    // TODO: mark invoice paid + ensure subscription active.
                    break;

                case "invoice.payment_failed":
                    // TODO: set subscription PastDue + trigger retry/grace logic.
                    break;

                case "customer.subscription.updated":
                    // TODO: sync plan/status/period from Stripe.
                    break;

                case "customer.subscription.deleted":
                    // TODO: cancel local subscription.
                    break;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Processed Stripe event {EventId} ({EventType})", stripeEvent.Id, stripeEvent.Type);
        }
    }
}
