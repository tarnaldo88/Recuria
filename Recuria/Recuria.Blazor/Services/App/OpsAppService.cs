namespace Recuria.Blazor.Services.App
{
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
    }
}
