namespace Recuria.Blazor.Services.App
{
    public interface IAuthAppService
    {
        Task<AppResult<Recuria.Client.AuthResponse>> LoginAsync(Recuria.Client.LoginRequest request);
        Task<AppResult<Recuria.Client.AuthResponse>> RegisterAsync(Recuria.Client.RegisterRequest request);
        Task<AppResult<Recuria.Client.WhoAmIResponse>> WhoAmIAsync(bool notifyError = true);
        Task<AppResult> LogoutAsync(bool notifySuccess = false, bool notifyError = false);
    }

    public sealed class AuthAppService : IAuthAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public AuthAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public Task<AppResult<Recuria.Client.AuthResponse>> LoginAsync(Recuria.Client.LoginRequest request)
        {
            return _runner.RunAsync(
                () => _api.LoginAsync(request),
                errorPrefix: "Sign in failed",
                notifyError: true);
        }

        public Task<AppResult<Recuria.Client.AuthResponse>> RegisterAsync(Recuria.Client.RegisterRequest request)
        {
            return _runner.RunAsync(
                () => _api.RegisterAsync(request),
                errorPrefix: "Create account failed",
                notifyError: true);
        }

        public Task<AppResult<Recuria.Client.WhoAmIResponse>> WhoAmIAsync(bool notifyError = true)
        {
            return _runner.RunAsync(
                () => _api.WhoamiAsync(),
                errorPrefix: "Unable to load session",
                notifyError: notifyError);
        }

        public async Task<AppResult> LogoutAsync(bool notifySuccess = false, bool notifyError = false)
        {
            try
            {
                await _api.LogoutAsync();
                return _runner.Ok("Signed out.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("Signed out.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Sign out failed", notifyError);
            }
        }
    }
}
