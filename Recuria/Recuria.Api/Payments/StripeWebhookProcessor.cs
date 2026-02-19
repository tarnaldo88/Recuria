using Microsoft.EntityFrameworkCore;
using Recuria.Infrastructure.Persistence;
using Recuria.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace Recuria.Api.Payments
{
    public interface StripeWebhookProcessor
    {
        Task ProcessAsync(Event stripeEvent, CancellationToken ct);
    }
    public sealed class StripeWebhookProcessor : IStripeWebhookProcessor
    {
        private readonly RecuriaDbContext _db;
        private readonly ILogger<StripeWebhookProcessor> _logger;
    }

}
