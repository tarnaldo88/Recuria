using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;

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

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _auth.ClearAsync();

                var current = _nav.ToBaseRelativePath(_nav.Uri);
                var returnUrl = "/" + current.TrimStart('/');
                _nav.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: false);
            }

            return response;
        }
    }
}
