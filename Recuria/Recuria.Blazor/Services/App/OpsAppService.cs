namespace Recuria.Blazor.Services.App
{
    public interface IOpsAppService
    {
        Task<AppResult<ICollection<Recuria.Client.DeadLetteredOutboxItem>>> GetDeadLetteredAsync(int take, bool notifyError = true);
        Task<AppResult> RetryAsync(Guid id, bool notifySuccess = true);

        Task<AppResult<Recuria.Client.PagedResultOfDeadLetteredOutboxItem>> GetDeadLetteredPageAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true);
    }

    public sealed class OpsAppService : IOpsAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public OpsAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public Task<AppResult<ICollection<Recuria.Client.DeadLetteredOutboxItem>>> GetDeadLetteredAsync(int take, bool notifyError = true) =>
            _runner.RunAsync(() => _api.DeadLetteredAsync(take), errorPrefix: "Unable to load dead-letter queue", notifyError: notifyError);

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

        public async Task<AppResult<ICollection<Recuria.Client.DeadLetteredOutboxItem>>> GetDeadLetteredPageAsync(
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true)
        {
            var result = await _runner.RunAsync(
                () => _api.DeadLetteredAsync(take: 200),
                errorPrefix: "Unable to load dead-letter queue",
                notifyError: notifyError);

            if (!result.Success || result.Data is null)
                return AppResult<ICollection<Recuria.Client.DeadLetteredOutboxItem>>.Fail(result.Error ?? "Unable to load dead-letter queue");

            IEnumerable<Recuria.Client.DeadLetteredOutboxItem> query = result.Data;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x =>
                    (x.Type ?? string.Empty).Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    (x.Error ?? string.Empty).Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            query = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
            {
                ("type", "desc") => query.OrderByDescending(x => x.Type),
                ("type", _) => query.OrderBy(x => x.Type),
                ("retrycount", "desc") => query.OrderByDescending(x => x.RetryCount),
                ("retrycount", _) => query.OrderBy(x => x.RetryCount),
                ("deadletteredonutc", "desc") => query.OrderByDescending(x => x.DeadLetteredOnUtc),
                ("deadletteredonutc", _) => query.OrderBy(x => x.DeadLetteredOnUtc),
                _ => query.OrderByDescending(x => x.DeadLetteredOnUtc)
            };

            var safePage = Math.Max(1, page);
            var safePageSize = Math.Max(1, pageSize);
            var paged = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();

            return AppResult<ICollection<Recuria.Client.DeadLetteredOutboxItem>>.Ok(paged);
        }


    }
}
