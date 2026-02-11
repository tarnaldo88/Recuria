using System.Text;
using System.Text.Json;

namespace Recuria.Blazor.Services
{
    //token + org id + role
    public sealed class AuthState
    {
        private readonly TokenStorage _storage;
        private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(2);

        public event Action? AuthStateChanged;

        public AuthState(TokenStorage storage) => _storage = storage;

        public async Task<string?> GetTokenAsync() => await _storage.GetTokenAsync().AsTask();
        public async Task<string?> GetOrgIdAsync() => await _storage.GetOrgIdAsync().AsTask();

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();

            return !string.IsNullOrWhiteSpace(token) && !IsExpired(token);
        }

        public bool ShouldRefresh(string jwt)
        {
            var exp = GetExpiryUtc(jwt);
            if (exp is null)
                return false;

            return exp <= DateTimeOffset.UtcNow.Add(RefreshWindow);
        }

        public async Task SetAuthAsync(string token, string orgId)
        {
            await _storage.SetTokenAsync(token);
            await _storage.SetOrgIdAsync(orgId);
            AuthStateChanged?.Invoke();
        }

        public async Task ClearAsync()
        {
            await _storage.ClearTokenAsync();
            await _storage.ClearOrgIdAsync();
            AuthStateChanged?.Invoke();
        }

        private static bool IsExpired(string jwt)
        {
            var exp = GetExpiryUtc(jwt);
            if (exp is null)
                return true;

            return exp <= DateTimeOffset.UtcNow;
        }

        private static DateTimeOffset? GetExpiryUtc(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) { return true; }

                var payload = parts[1].Replace('-', '+').Replace('_', '/');

                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("exp", out var expElement))
                    return null;

                var exp = expElement.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(exp);
            }
            catch
            {
                return null;
            }
        }
    }
}
