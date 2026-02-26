using Microsoft.EntityFrameworkCore;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;
using Stripe;
using Stripe.Checkout;
using DomainSubscription = Recuria.Domain.Entities.Subscription;
using StripeSubscription = Stripe.Subscription;

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
            if (alreadyProcessed)
                return;

            _db.StripeWebhookEvents.Add(new StripeWebhookEvent
            {
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type
            });

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutCompletedAsync(stripeEvent.Data.Object as Session, ct);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent.Data.Object as Stripe.Invoice, ct);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(stripeEvent.Data.Object as Stripe.Invoice, ct);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as StripeSubscription, ct);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as StripeSubscription, ct);
                    break;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Processed Stripe event {EventId} ({EventType})", stripeEvent.Id, stripeEvent.Type);
        }

        private async Task HandleCheckoutCompletedAsync(Session? session, CancellationToken ct)
        {
            if (session is null)
                return;

            if (!session.Metadata.TryGetValue("org_id", out var orgRaw) || !Guid.TryParse(orgRaw, out var orgId))
                return;

            var customerId = session.CustomerId ?? string.Empty;
            var stripeSubscriptionId = session.SubscriptionId ?? string.Empty;
            var planCode = session.Metadata.TryGetValue("plan_code", out var mappedPlanCode) ? mappedPlanCode : string.Empty;

            var existingMap = await _db.StripeSubscriptionMaps
                .SingleOrDefaultAsync(x => x.OrganizationId == orgId, ct);

            if (existingMap is null)
            {
                _db.StripeSubscriptionMaps.Add(new StripeSubscriptionMap
                {
                    OrganizationId = orgId,
                    StripeCustomerId = customerId,
                    StripeSubscriptionId = stripeSubscriptionId
                });
            }
            else
            {
                existingMap.StripeCustomerId = customerId;
                existingMap.StripeSubscriptionId = stripeSubscriptionId;
                existingMap.UpdatedOnUtc = DateTime.UtcNow;
            }

            var local = await GetCurrentLocalSubscriptionByOrganizationAsync(orgId, ct);
            if (local is null)
            {
                _logger.LogWarning("No local subscription found for organization {OrganizationId} on checkout completion.", orgId);
                return;
            }

            var now = DateTime.UtcNow;
            if (local.Status is SubscriptionStatus.Trial or SubscriptionStatus.PastDue or SubscriptionStatus.Canceled)
            {
                local.Activate(now);
            }

            if (TryMapPlan(planCode, out var plan) && local.Plan != plan)
            {
                local.UpgradePlan(plan);
            }
        }

        private async Task HandleInvoicePaymentSucceededAsync(Stripe.Invoice? invoice, CancellationToken ct)
        {
            if (invoice is null)
                return;

            var orgId = await ResolveOrganizationIdAsync(invoice.SubscriptionId, invoice.CustomerId, ct);
            if (orgId is null)
                return;

            var local = await GetCurrentLocalSubscriptionByOrganizationAsync(orgId.Value, ct);
            if (local is null)
                return;

            if (local.Status is SubscriptionStatus.Trial or SubscriptionStatus.PastDue or SubscriptionStatus.Canceled)
            {
                local.Activate(DateTime.UtcNow);
            }

            local.RecordBillingAttempt(BillingAttempt.Success(local.Id));
        }

        private async Task HandleInvoicePaymentFailedAsync(Stripe.Invoice? invoice, CancellationToken ct)
        {
            if (invoice is null)
                return;

            var orgId = await ResolveOrganizationIdAsync(invoice.SubscriptionId, invoice.CustomerId, ct);
            if (orgId is null)
                return;

            var local = await GetCurrentLocalSubscriptionByOrganizationAsync(orgId.Value, ct);
            if (local is null)
                return;

            if (local.Status == SubscriptionStatus.Active)
            {
                local.MarkPastDue();
            }

            var message = invoice.LastFinalizationError?.Message
                          ?? "Stripe payment failed.";
            local.RecordBillingAttempt(BillingAttempt.Failure(local.Id, message));
        }

        private async Task HandleSubscriptionUpdatedAsync(StripeSubscription? stripeSubscription, CancellationToken ct)
        {
            if (stripeSubscription is null)
                return;

            var orgId = await ResolveOrganizationIdAsync(stripeSubscription.Id, stripeSubscription.CustomerId, ct);
            if (orgId is null)
                return;

            var local = await GetCurrentLocalSubscriptionByOrganizationAsync(orgId.Value, ct);
            if (local is null)
                return;

            switch (stripeSubscription.Status)
            {
                case "active":
                case "trialing":
                    if (local.Status is SubscriptionStatus.Trial or SubscriptionStatus.PastDue or SubscriptionStatus.Canceled)
                        local.Activate(DateTime.UtcNow);
                    break;
                case "past_due":
                case "unpaid":
                case "incomplete":
                    if (local.Status == SubscriptionStatus.Active)
                        local.MarkPastDue();
                    break;
                case "canceled":
                case "incomplete_expired":
                    if (local.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Trial)
                        local.Cancel();
                    break;
            }
        }

        private async Task HandleSubscriptionDeletedAsync(StripeSubscription? stripeSubscription, CancellationToken ct)
        {
            if (stripeSubscription is null)
                return;

            var orgId = await ResolveOrganizationIdAsync(stripeSubscription.Id, stripeSubscription.CustomerId, ct);
            if (orgId is null)
                return;

            var local = await GetCurrentLocalSubscriptionByOrganizationAsync(orgId.Value, ct);
            if (local is null)
                return;

            if (local.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue or SubscriptionStatus.Trial)
            {
                local.Cancel();
            }
        }

        private async Task<Guid?> ResolveOrganizationIdAsync(string? stripeSubscriptionId, string? stripeCustomerId, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                var bySubscription = await _db.StripeSubscriptionMaps
                    .AsNoTracking()
                    .Where(x => x.StripeSubscriptionId == stripeSubscriptionId)
                    .Select(x => (Guid?)x.OrganizationId)
                    .FirstOrDefaultAsync(ct);
                if (bySubscription.HasValue)
                    return bySubscription.Value;
            }

            if (!string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                var byCustomer = await _db.StripeSubscriptionMaps
                    .AsNoTracking()
                    .Where(x => x.StripeCustomerId == stripeCustomerId)
                    .Select(x => (Guid?)x.OrganizationId)
                    .FirstOrDefaultAsync(ct);
                if (byCustomer.HasValue)
                    return byCustomer.Value;
            }

            return null;
        }

        private async Task<DomainSubscription?> GetCurrentLocalSubscriptionByOrganizationAsync(Guid orgId, CancellationToken ct)
        {
            return await _db.Subscriptions
                .Where(s =>
                    s.OrganizationId == orgId &&
                    s.Status != SubscriptionStatus.Expired &&
                    s.Status != SubscriptionStatus.Canceled)
                .OrderByDescending(s => s.PeriodEnd)
                .FirstOrDefaultAsync(ct);
        }

        private static bool TryMapPlan(string? planCode, out PlanType plan)
        {
            plan = PlanType.Free;
            if (string.IsNullOrWhiteSpace(planCode))
                return false;

            plan = planCode.Trim().ToLowerInvariant() switch
            {
                "pro" or "pro_monthly" => PlanType.Pro,
                "enterprise" or "enterprise_monthly" => PlanType.Enterprise,
                "free" or "free_monthly" => PlanType.Free,
                _ => PlanType.Free
            };

            return true;
        }
    }
}
