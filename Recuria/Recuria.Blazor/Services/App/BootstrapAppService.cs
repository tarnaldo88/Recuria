namespace Recuria.Blazor.Services.App
{
    public interface IBootstrapAppService
    {
        Task<AppResult<Recuria.Client.BootstrapResponse>> BootstrapAsync(Recuria.Client.BootstrapRequest request);
    }

    public sealed class BootstrapAppService : IBootstrapAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public BootstrapAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public Task<AppResult<Recuria.Client.BootstrapResponse>> BootstrapAsync(Recuria.Client.BootstrapRequest request) =>
            _runner.RunAsync(
                () => _api.BootstrapAsync(request),
                successMessage: "Tenant bootstrapped.",
                errorPrefix: "Bootstrap failed",
                notifySuccess: true,
                notifyError: true);
    }
}
