namespace Recuria.Api.Configuration
{
    public sealed class IdempotencyOptions
    {
        public const string SectionName = "Idempotency";
        public int InvoiceCreateTtlHours { get; init; } = 24;
    }
}
