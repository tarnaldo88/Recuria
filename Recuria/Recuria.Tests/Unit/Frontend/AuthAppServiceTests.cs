using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class AuthAppServiceTests
    {
        private readonly Mock<Recuria.Client.IRecuriaApiClient> _api = new();
        private readonly ApiCallRunner _runner;

        public AuthAppServiceTests()
        {
            var snackbar = new Mock<ISnackbar>();
            _runner = new ApiCallRunner(snackbar.Object);
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Success_When_Api_Succeeds()
        {
            var response = new Recuria.Client.AuthResponse
            {
                AccessToken = "token",
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Email = "owner@recuria.local",
                Name = "Owner",
                Role = "Owner"
            };

            _api.Setup(x => x.LoginAsync(It.IsAny<Recuria.Client.LoginRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var service = new AuthAppService(_api.Object, _runner);

            var result = await service.LoginAsync(new Recuria.Client.LoginRequest
            {
                OrganizationId = response.OrganizationId,
                Email = response.Email,
                Password = "StrongPass!123"
            });

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().Be("token");
        }

        [Fact]
        public async Task LogoutAsync_Should_Treat_204_Exception_As_Success()
        {
            _api.Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(204, string.Empty));

            var service = new AuthAppService(_api.Object, _runner);
            var result = await service.LogoutAsync();

            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task LogoutAsync_Should_Return_Failure_For_Non204_Error()
        {
            _api.Setup(x => x.LogoutAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(500, "{\"title\":\"Boom\"}"));

            var service = new AuthAppService(_api.Object, _runner);
            var result = await service.LogoutAsync();

            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Sign out failed");
        }

        private static Recuria.Client.ApiException CreateApiException(int statusCode, string response)
        {
            return new Recuria.Client.ApiException(
                message: "API Error",
                statusCode: statusCode,
                response: response,
                headers: new Dictionary<string, IEnumerable<string>>(),
                innerException: null);
        }
    }
}
