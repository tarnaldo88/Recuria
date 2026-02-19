namespace Recuria.Api.Configuration
{
    public sealed class StripeOptions
    {
        public const string SectionName = "Stripe";
        public string SecretKey { get; init; } = "";
        public string WebhookSecret { get; init; } = "";
        public string SuccessUrl { get; init; } = "";
        public string CancelUrl { get; init; } = "";
    }
}
