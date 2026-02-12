namespace Recuria.Blazor.Services.App
{
    public sealed class UserContext
    {
        public bool IsAuthenticated { get; init; }
        public string? Role { get; init; }

        public bool IsOwner => string.Equals(Role, "Owner", StringComparison.OrdinalIgnoreCase);
        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsMember => string.Equals(Role, "Member", StringComparison.OrdinalIgnoreCase);

        public bool CanViewOps => IsOwner || IsAdmin;
        public bool CanManageUsers => IsOwner || IsAdmin;
        public bool CanManageBilling => IsOwner || IsAdmin; // invoices/subscription actions
    }

    public interface IUserContextService
    {
        Task<UserContext> GetAsync(bool forceRefresh = false);
    }

    public sealed class UserContextService : IUserContextService
    {
        private readonly IAuthAppService _authApi;
        private readonly AuthState _auth;
        private UserContext? _cached;

        public UserContextService(IAuthAppService authApi, AuthState auth)
        {
            _authApi = authApi;
            _auth = auth;
        }

        public async Task<UserContext> GetAsync(bool forceRefresh = false)
        {
            if(!forceRefresh && _cached is not null)
            {
                return _cached;
            }

            var isAuth = await _auth.IsAuthenticatedAsync();
            if (!isAuth)
            {
                _cached = new UserContext { IsAuthenticated = false };
                return _cached;
            }

            var who = await _authApi.WhoAmIAsync(notifyError: false);
            _cached = new UserContext
            {
                IsAuthenticated = true,
                Role = who.Success ? who.Data?.Role : null
            };

            return _cached;
        }
    }

    

}
