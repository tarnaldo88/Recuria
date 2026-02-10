using System.Text;
using System.Text.Json;

namespace Recuria.Blazor.Services
{
    //token + org id + role
    public sealed class AuthState
    {
        private readonly TokenStorage _storage;

        public AuthState(TokenStorage storage) => _storage = storage;

        public async Task<string?> GetTokenAsync() => await _storage.GetTokenAsync().AsTask();
        public async Task<string?> GetOrgIdAsync() => await _storage.GetOrgIdAsync().AsTask();

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();

            return !string.IsNullOrWhiteSpace(token) && !IsExpired(token);
        }

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

        private static bool IsExpired(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) { return true; }

                var payload = parts[1].Replace('-', '+').Replace('_', '/');

                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("exp", out var expElement)) return true;

                var exp = expElement.GetInt64();
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                return exp <= now;
            }
            catch
            {
                return true;
            }
        }
    }
}
