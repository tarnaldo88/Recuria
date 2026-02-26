using System.Net.Http.Json;

namespace Recuria.Blazor.Services.App
{
    public sealed record PagedVm<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

    public sealed record StripeWebhookSummaryVm(int ProcessedCount, int PendingCount, int RetryingCount, int DeadLetterLikeCount);

    public sealed record StripeWebhookInboxItemVm(
        Guid Id,
        string StripeEventId,
        string EventType,
        DateTime ReceivedOnUtc,
        DateTime? ProcessedOnUtc,
        int AttemptCount,
        DateTime? NextAttemptOnUtc,
        string? LastError,
        string Status);

    public interface IOpsAppService
    {
        Task<AppResult> RetryAsync(Guid id, bool notifySuccess = true);

        Task<AppResult<Recuria.Client.DeadLetteredOutboxItemPagedResult>> GetDeadLetteredPageAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true);

        Task<AppResult<StripeWebhookSummaryVm>> GetStripeWebhookSummaryAsync(bool notifyError = true);

        Task<AppResult<PagedVm<StripeWebhookInboxItemVm>>> GetStripeWebhookInboxPageAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true);

        Task<AppResult> RequeueStripeWebhookAsync(Guid id, bool notifySuccess = true);
    }

    public sealed class OpsAppService : IOpsAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly HttpClient _http;
        private readonly ApiCallRunner _runner;

        public OpsAppService(Recuria.Client.IRecuriaApiClient api, HttpClient http, ApiCallRunner runner)
        {
            _api = api;
            _http = http;
            _runner = runner;
        }

        public async Task<AppResult> RetryAsync(Guid id, bool notifySuccess = true)
        {
            try
            {
                await _api.RetryAsync(id);
                return _runner.Ok("Message requeued.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("Message requeued.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to retry message", notifyError: true);
            }
        }

        public Task<AppResult<Recuria.Client.DeadLetteredOutboxItemPagedResult>> GetDeadLetteredPageAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true) =>
            _runner.RunAsync(
                () => _api.DeadLetteredAsync(page, pageSize, search, sortBy, sortDir),
                errorPrefix: "Unable to load dead-letter queue",
                notifyError: notifyError);

        public Task<AppResult<StripeWebhookSummaryVm>> GetStripeWebhookSummaryAsync(bool notifyError = true) =>
            _runner.RunAsync(async () =>
            {
                var dto = await _http.GetFromJsonAsync<StripeWebhookSummaryResponse>("api/payments/webhook-summary")
                          ?? new StripeWebhookSummaryResponse();
                return new StripeWebhookSummaryVm(dto.ProcessedCount, dto.PendingCount, dto.RetryingCount, dto.DeadLetterLikeCount);
            }, errorPrefix: "Unable to load Stripe webhook summary", notifyError: notifyError);

        public Task<AppResult<PagedVm<StripeWebhookInboxItemVm>>> GetStripeWebhookInboxPageAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true) =>
            _runner.RunAsync(async () =>
            {
                var query = new List<string>
                {
                    $"Page={Uri.EscapeDataString(page.ToString())}",
                    $"PageSize={Uri.EscapeDataString(pageSize.ToString())}",
                    $"SortDir={Uri.EscapeDataString((sortDir ?? "asc"))}"
                };
                if (!string.IsNullOrWhiteSpace(search))
                    query.Add($"Search={Uri.EscapeDataString(search)}");
                if (!string.IsNullOrWhiteSpace(sortBy))
                    query.Add($"SortBy={Uri.EscapeDataString(sortBy)}");

                var url = $"api/payments/webhook-inbox?{string.Join("&", query)}";
                var dto = await _http.GetFromJsonAsync<PagedResponse<StripeWebhookInboxItemResponse>>(url)
                          ?? new PagedResponse<StripeWebhookInboxItemResponse>();

                var items = dto.Items?
                    .Select(x => new StripeWebhookInboxItemVm(
                        x.Id,
                        x.StripeEventId,
                        x.EventType,
                        x.ReceivedOnUtc,
                        x.ProcessedOnUtc,
                        x.AttemptCount,
                        x.NextAttemptOnUtc,
                        x.LastError,
                        x.Status))
                    .ToList()
                    ?? new List<StripeWebhookInboxItemVm>();

                return new PagedVm<StripeWebhookInboxItemVm>(items, dto.TotalCount, dto.Page, dto.PageSize);
            }, errorPrefix: "Unable to load Stripe webhook inbox", notifyError: notifyError);

        public async Task<AppResult> RequeueStripeWebhookAsync(Guid id, bool notifySuccess = true)
        {
            try
            {
                var response = await _http.PostAsync($"api/payments/webhook-inbox/{id}/requeue", content: null);
                response.EnsureSuccessStatusCode();
                return _runner.Ok("Webhook requeued.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to requeue webhook", notifyError: true);
            }
        }

        private sealed class StripeWebhookSummaryResponse
        {
            public int ProcessedCount { get; init; }
            public int PendingCount { get; init; }
            public int RetryingCount { get; init; }
            public int DeadLetterLikeCount { get; init; }
        }

        private sealed class PagedResponse<T>
        {
            public List<T>? Items { get; init; }
            public int TotalCount { get; init; }
            public int Page { get; init; }
            public int PageSize { get; init; }
        }

        private sealed class StripeWebhookInboxItemResponse
        {
            public Guid Id { get; init; }
            public string StripeEventId { get; init; } = string.Empty;
            public string EventType { get; init; } = string.Empty;
            public DateTime ReceivedOnUtc { get; init; }
            public DateTime? ProcessedOnUtc { get; init; }
            public int AttemptCount { get; init; }
            public DateTime? NextAttemptOnUtc { get; init; }
            public string? LastError { get; init; }
            public string Status { get; init; } = string.Empty;
        }
    }
}
