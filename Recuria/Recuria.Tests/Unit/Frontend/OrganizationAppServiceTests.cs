using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class OrganizationAppServiceTests
    {
        private readonly Mock<Recuria.Client.IRecuriaApiClient> _api = new();
        private readonly ApiCallRunner _runner;

        public OrganizationAppServiceTests()
        {
            var snackbar = new Mock<ISnackbar>();
            _runner = new ApiCallRunner(snackbar.Object);
        }

        [Fact]
        public async Task GetMineAsync_Should_Return_Success_When_Api_Succeeds()
        {
            var org = new Recuria.Client.OrganizationDto
            {
                Id = Guid.NewGuid(),
                Name = "Acme",
                OwnerEmail = "owner@acme.local",
                UserCount = 3
            };

            _api.Setup(x => x.MeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(org);

            var service = new OrganizationAppService(_api.Object, _runner);
            var result = await service.GetMineAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Acme");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Api_Throws()
        {
            _api.Setup(x => x.OrganizationsGETAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(404, "{\"title\":\"Not Found\"}"));

            var service = new OrganizationAppService(_api.Object, _runner);
            var result = await service.GetByIdAsync(Guid.NewGuid());

            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Unable to load organization");
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
