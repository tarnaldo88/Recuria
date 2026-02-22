using Recuria.Client;
using System.Net.Http.Json;

namespace Recuria.Blazor.Services.App
{
    public sealed record BillingPlanVm(string Code, string Name, long AmountCents, string Currency, string Interval);

    public interface  IPaymentAppService
    {
        Task<AppResult<IReadOnlyList<BillingPlanVm>>> GetPlansAsync(bool notifyError = true);
        Task<AppResult<string>> CreateCheckoutUrlAsync(Guid organizationId, string planCode, int quantity = 1, bool notifyError = true);
    }

    public class PaymentAppService : IPaymentAppService
    {
        private readonly HttpClient _http;
        private readonly ApiCallRunner _runner;

        public PaymentAppService(HttpClient http, ApiCallRunner runner)
        {
            _http = http;
            _runner = runner;
        }

        public Task<AppResult<string>> CreateCheckoutUrlAsync(Guid organizationId, string priceId, int quantity = 1, bool notifyError = true) =>
        _runner.RunAsync(async () =>
        {
            var request = new CreateCheckoutSessionRequest
            {
                OrganizationId = organizationId,
                PriceId = priceId,
                Quantity = quantity
            };

            var response = await _http.PostAsJsonAsync("api/payments/checkout-session", request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<CreateCheckoutSessionResponse>();
            if (payload is null || string.IsNullOrWhiteSpace(payload.Url))
                throw new InvalidOperationException("Checkout URL was not returned by API.");

            return payload.Url;
        }, errorPrefix: "Unable to start checkout", notifyError: notifyError);

        public Task<AppResult<IReadOnlyList<BillingPlanVm>>> GetPlansAsync(bool notifyError = true) =>
            _runner.RunAsync(async () =>
            {
                var plans = await _http.GetFromJsonAsync<List<BillingPlanDto>>("api/payments/plans")
                            ?? new List<BillingPlanDto>();

                return (IReadOnlyList<BillingPlanVm>)plans
                    .Select(p => new BillingPlanVm(
                        p.Code ?? string.Empty,
                        p.Name ?? string.Empty,
                        p.AmountCents,
                        p.Currency ?? "usd",
                        p.Interval ?? "month"))
                    .ToList();
            }, errorPrefix: "Unable to load billing plans", notifyError: notifyError);

        private sealed class CreateCheckoutSessionRequest
        {
            public Guid OrganizationId { get; init; }
            public string PriceId { get; init; } = string.Empty;
            public int Quantity { get; init; }
        }

        private sealed class CreateCheckoutSessionResponse
        {
            public string SessionId { get; init; } = string.Empty;
            public string Url { get; init; } = string.Empty;
        }
    }
}
