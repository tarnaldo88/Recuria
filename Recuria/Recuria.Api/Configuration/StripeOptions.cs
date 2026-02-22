namespace Recuria.Api.Configuration;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;

    public List<StripePlanOption> Plans { get; init; } = new();
}

public sealed class StripePlanOption
{
    public string Code { get; init; } = string.Empty;          // ex: pro_monthly
    public string Name { get; init; } = string.Empty;          // ex: Pro Monthly
    public string StripePriceId { get; init; } = string.Empty; // ex: price_...
    public long AmountCents { get; init; }
    public string Currency { get; init; } = "usd";
    public string Interval { get; init; } = "month";
    public bool Active { get; init; } = true;
}
