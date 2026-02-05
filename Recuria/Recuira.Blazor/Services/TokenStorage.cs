using Microsoft.JSInterop;


namespace Recuira.Blazor.Services
{
    //localStorage helper
    public sealed class TokenStorage
    {
        private const string TokenKey = "recuria.jwt";
        private const string OrgKey = "recuria.orgId";

        private readonly IJSRuntime _js;

        public TokenStorage(IJSRuntime js) => _js = js;

        public ValueTask SetTokenAsync(string token) => _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

        public ValueTask<string?> GetTokenAsync() => _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

        public ValueTask ClearTokenAsync() => _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);

        public ValueTask SetOrgIdAsync(string orgId) => _js.InvokeVoidAsync("localStorage.setItem", OrgKey, orgId);

        public ValueTask<string?> GetOrgIdAsync() => _js.InvokeAsync<string?>("localStorage.getItem", OrgKey);

        public ValueTask ClearOrgIdAsync() => _js.InvokeVoidAsync("localStorage.removeItem", OrgKey);
    }
}
