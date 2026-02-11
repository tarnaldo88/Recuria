namespace Recuria.Blazor.Services.App
{
    public interface ISubscriptionAppService
    {
        Task<AppResult<Recuria.Client.SubscriptionDetailsDto>> GetCurrentAsync(Guid orgId, bool notifyError = true);
        Task<AppResult<Recuria.Client.SubscriptionDetailsDto>> CreateTrialAsync(Guid orgId, bool notifySuccess = true);
        Task<AppResult> UpgradeAsync(Guid subscriptionId, Recuria.Client.UpgradeSubscriptionRequest request, bool notifySuccess = true);
        Task<AppResult> CancelAsync(Guid subscriptionId, bool notifySuccess = true);
    }

    public sealed class SubscriptionAppService : ISubscriptionAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public SubscriptionAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public Task<AppResult<Recuria.Client.SubscriptionDetailsDto>> GetCurrentAsync(Guid orgId, bool notifyError = true) =>
            _runner.RunAsync(() => _api.CurrentAsync(orgId), errorPrefix: "Unable to load subscription", notifyError: notifyError);

        public Task<AppResult<Recuria.Client.SubscriptionDetailsDto>> CreateTrialAsync(Guid orgId, bool notifySuccess = true) =>
            _runner.RunAsync(
                () => _api.TrialAsync(orgId),
                successMessage: "Trial started.",
                errorPrefix: "Unable to start trial",
                notifySuccess: notifySuccess,
                notifyError: true);

        public async Task<AppResult> UpgradeAsync(Guid subscriptionId, Recuria.Client.UpgradeSubscriptionRequest request, bool notifySuccess = true)
        {
            try
            {
                await _api.UpgradeAsync(subscriptionId, request);
                return _runner.Ok("Subscription updated.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("Subscription updated.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to update subscription", notifyError: true);
            }
        }

        public async Task<AppResult> CancelAsync(Guid subscriptionId, bool notifySuccess = true)
        {
            try
            {
                await _api.CancelAsync(subscriptionId);
                return _runner.Ok("Subscription canceled.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("Subscription canceled.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to cancel subscription", notifyError: true);
            }
        }
    }
}
