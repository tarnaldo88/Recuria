using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Recuria.Blazor.Services
{
    public sealed class AuthHeaderHandler : DelegatingHandler
    {
        private readonly AuthState _auth;
        private readonly NavigationManager _nav;

        public AuthHeaderHandler(AuthState auth, NavigationManager nav)
        {
            _auth = auth;
            _nav = nav;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _auth.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token) &&
                _auth.ShouldRefresh(token) &&
                !request.RequestUri!.AbsolutePath.Contains("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
            {
                token = await TryRefreshTokenAsync(token, request.RequestUri!, cancellationToken) ?? token;
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _auth.ClearAsync();
                RedirectToLogin();
            }

            return response;
        }

        private async Task<string?> TryRefreshTokenAsync(string currentToken, Uri requestUri, CancellationToken cancellationToken)
        {
            try
            {
                var refreshUri = new Uri($"{requestUri.Scheme}://{requestUri.Authority}/api/auth/refresh");
                using var refreshClient = new HttpClient();
                using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, refreshUri);
                refreshRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);

                using var response = await refreshClient.SendAsync(refreshRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var refreshed = JsonSerializer.Deserialize<AuthRefreshResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (refreshed is null ||
                    string.IsNullOrWhiteSpace(refreshed.AccessToken) ||
                    refreshed.OrganizationId == Guid.Empty)
                {
                    return null;
                }

                await _auth.SetAuthAsync(refreshed.AccessToken, refreshed.OrganizationId.ToString());
                return refreshed.AccessToken;
            }
            catch
            {
                return null;
            }
        }

        private void RedirectToLogin()
        {
            var current = _nav.ToBaseRelativePath(_nav.Uri);
            var returnUrl = "/" + current.TrimStart('/');
            _nav.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: false);
        }

        private sealed class AuthRefreshResponse
        {
            public string? AccessToken { get; set; }
            public Guid OrganizationId { get; set; }
        }
    }
}
