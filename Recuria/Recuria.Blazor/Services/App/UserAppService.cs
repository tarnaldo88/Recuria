using Recuria.Client;

namespace Recuria.Blazor.Services.App
{
    public interface IUserAppService
    {
        Task<AppResult> AddAsync(Guid orgId, Recuria.Client.AddUserRequest request, bool notifySuccess = true);
        Task<AppResult<ICollection<Recuria.Client.UserSummaryDto>>> GetAllAsync(Guid orgId, bool notifyError = true);
        Task<AppResult> ChangeRoleAsync(Guid orgId, Guid userId, Recuria.Client.ChangeUserRoleRequest request, bool notifySuccess = true);
        Task<AppResult> RemoveAsync(Guid orgId, Guid userId, bool notifySuccess = true);

        Task<AppResult<Recuria.Client.UserSummaryDtoPagedResult>> GetPageAsync(
            Guid orgId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true); GetPageAsync(
            Guid orgId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true);
    }

    public sealed class UserAppService : IUserAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public UserAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public async Task<AppResult> AddAsync(Guid orgId, Recuria.Client.AddUserRequest request, bool notifySuccess = true)
        {
            try
            {
                await _api.UsersPOSTAsync(orgId, request);
                return _runner.Ok("User added.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("User added.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to add user", notifyError: true);
            }
        }

        public Task<AppResult<ICollection<Recuria.Client.UserSummaryDto>>> GetAllAsync(Guid orgId, bool notifyError = true) =>
            _runner.RunAsync(() => _api.UsersAllAsync(orgId), errorPrefix: "Unable to load users", notifyError: notifyError);

        public async Task<AppResult> ChangeRoleAsync(Guid orgId, Guid userId, Recuria.Client.ChangeUserRoleRequest request, bool notifySuccess = true)
        {
            try
            {
                await _api.RoleAsync(orgId, userId, request);
                return _runner.Ok("Role updated.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("Role updated.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to update role", notifyError: true);
            }
        }

        public async Task<AppResult> RemoveAsync(Guid orgId, Guid userId, bool notifySuccess = true)
        {
            try
            {
                await _api.UsersDELETEAsync(orgId, userId);
                return _runner.Ok("User removed.", notifySuccess);
            }
            catch (Recuria.Client.ApiException ex) when (ex.StatusCode == 204)
            {
                return _runner.Ok("User removed.", notifySuccess);
            }
            catch (Exception ex)
            {
                return _runner.Fail(ex, "Unable to remove user", notifyError: true);
            }
        }

        public Task<AppResult<Recuria.Client.UserSummaryDtoPagedResult>> GetPageAsync(
            Guid orgId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true) =>
            _runner.RunAsync(
                () => _api.UsersGETAsync(orgId, page, pageSize, search, sortBy, sortDir),
                errorPrefix: "Unable to load users",
                notifyError: notifyError);

    }
}
