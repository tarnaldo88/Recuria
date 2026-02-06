using System.Net.Http.Headers;

namespace Recuira.Blazor.Services
{
    public sealed class AuthHeaderHandler : DelegatingHandler
    {
        private readonly AuthState _auth;

        public AuthHeaderHandler(AuthState auth)
        {
            _auth = auth;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _auth.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); 
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
