using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class UserAppServiceTests
    {
        private readonly Mock<Recuria.Client.IRecuriaApiClient> _api = new();
        private readonly ApiCallRunner _runner;

        public UserAppServiceTests()
        {
            var snackbar = new Mock<ISnackbar>();
            _runner = new ApiCallRunner(snackbar.Object);
        }

        [Fact]
        public async Task ChangeRoleAsync_Should_Treat_204_Exception_As_Success()
        {
            _api.Setup(x => x.RoleAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Recuria.Client.ChangeUserRoleRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(204, string.Empty));

            var service = new UserAppService(_api.Object, _runner);
            var result = await service.ChangeRoleAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Recuria.Client.ChangeUserRoleRequest { NewRole = Recuria.Client.UserRole.Admin });

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveAsync_Should_Treat_204_Exception_As_Success()
        {
            _api.Setup(x => x.UsersDELETEAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(204, string.Empty));

            var service = new UserAppService(_api.Object, _runner);
            var result = await service.RemoveAsync(Guid.NewGuid(), Guid.NewGuid());

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Data_When_Api_Succeeds()
        {
            var users = new List<Recuria.Client.UserSummaryDto>
            {
                new() { Id = Guid.NewGuid(), Email = "a@recuria.local", Name = "A", Role = Recuria.Client.UserRole.Member }
            };

            _api.Setup(x => x.UsersAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            var service = new UserAppService(_api.Object, _runner);
            var result = await service.GetAllAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(1);
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
