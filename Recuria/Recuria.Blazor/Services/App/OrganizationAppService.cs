namespace Recuria.Blazor.Services.App
{
    public interface IOrganizationAppService
    {
        Task<AppResult<Recuria.Client.OrganizationDto>> GetByIdAsync(Guid id, bool notifyError = true);
        Task<AppResult<Recuria.Client.OrganizationDto>> GetMineAsync(bool notifyError = true);
    }

    public sealed class OrganizationAppService : IOrganizationAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public OrganizationAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public Task<AppResult<Recuria.Client.OrganizationDto>> GetByIdAsync(Guid id, bool notifyError = true) =>
            _runner.RunAsync(() => _api.OrganizationsGETAsync(id), errorPrefix: "Unable to load organization", notifyError: notifyError);

        public Task<AppResult<Recuria.Client.OrganizationDto>> GetMineAsync(bool notifyError = true) =>
            _runner.RunAsync(() => _api.MeAsync(), errorPrefix: "Unable to load organization", notifyError: notifyError);
    }
}
