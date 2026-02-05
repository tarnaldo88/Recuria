namespace Recuira.Blazor.Services
{
    //token + org id + role
    public sealed class AuthState
    {
        private readonly TokenStorage _storage;

        public AuthState(TokenStorage storage) => _storage = storage;

        public async Task<string?> GetTokenAsync() => await _storage.GetTokenAsync();
        public async Task<string?> GetOrgIdAsync() => await _storage.GetOrgIdAsync();

        public async Task SetAuthAsync(string token, string orgId)
        {
            await _storage.SetTokenAsync(token);
            await _storage.SetOrgIdAsync(orgId);
        }

        public async Task ClearAsync()
        {
            await _storage.ClearTokenAsync();
            await _storage.ClearOrgIdAsync();
        }
    }
}
