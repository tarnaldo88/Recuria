using FluentAssertions;
using Microsoft.JSInterop;
using Moq;
using Recuria.Blazor.Services;
using Recuria.Blazor.Services.App;
using System.Text;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class UserContextServiceTests
    {
        [Fact]
        public async Task GetAsync_Should_Return_Unauthenticated_When_No_Token()
        {
            var auth = CreateAuthStateWithToken(token: null);
            var authApi = new Mock<IAuthAppService>(MockBehavior.Strict);
            var sut = new UserContextService(authApi.Object, auth);

            var context = await sut.GetAsync();

            context.IsAuthenticated.Should().BeFalse();
            context.CanViewOps.Should().BeFalse();
            context.CanManageUsers.Should().BeFalse();
            authApi.Verify(x => x.WhoAmIAsync(It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetAsync_Should_Map_Admin_Role_Capabilities()
        {
            var auth = CreateAuthStateWithToken(CreateJwt(expiryUtc: DateTimeOffset.UtcNow.AddHours(1)));
            var authApi = new Mock<IAuthAppService>();
            authApi.Setup(x => x.WhoAmIAsync(It.IsAny<bool>()))
                .ReturnsAsync(AppResult<Recuria.Client.WhoAmIResponse>.Ok(new Recuria.Client.WhoAmIResponse
                {
                    Role = "Admin"
                }));

            var sut = new UserContextService(authApi.Object, auth);
            var context = await sut.GetAsync();

            context.IsAuthenticated.Should().BeTrue();
            context.IsAdmin.Should().BeTrue();
            context.CanViewOps.Should().BeTrue();
            context.CanManageUsers.Should().BeTrue();
            context.CanManageBilling.Should().BeTrue();
        }

        [Fact]
        public async Task GetAsync_Should_Use_Cache_Unless_Forced()
        {
            var auth = CreateAuthStateWithToken(CreateJwt(expiryUtc: DateTimeOffset.UtcNow.AddHours(1)));
            var authApi = new Mock<IAuthAppService>();
            authApi.Setup(x => x.WhoAmIAsync(It.IsAny<bool>()))
                .ReturnsAsync(AppResult<Recuria.Client.WhoAmIResponse>.Ok(new Recuria.Client.WhoAmIResponse
                {
                    Role = "Member"
                }));

            var sut = new UserContextService(authApi.Object, auth);

            _ = await sut.GetAsync();
            _ = await sut.GetAsync();
            _ = await sut.GetAsync(forceRefresh: true);

            authApi.Verify(x => x.WhoAmIAsync(It.IsAny<bool>()), Times.Exactly(2));
        }

        private static AuthState CreateAuthStateWithToken(string? token)
        {
            var js = new Mock<IJSRuntime>();
            js.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
                .Returns((string _, object?[] args) =>
                {
                    var key = args.Length > 0 ? args[0]?.ToString() : null;
                    return key == "recuria.jwt"
                        ? new ValueTask<string?>(token)
                        : new ValueTask<string?>((string?)null);
                });

            return new AuthState(new TokenStorage(js.Object));
        }

        private static string CreateJwt(DateTimeOffset expiryUtc)
        {
            var payloadJson = $"{{\"exp\":{expiryUtc.ToUnixTimeSeconds()}}}";
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return $"header.{payload}.signature";
        }
    }
}
