using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class InvoiceAppServiceTests
    {
        private readonly Mock<Recuria.Client.IRecuriaApiClient> _api = new();
        private readonly ApiCallRunner _runner;

        public InvoiceAppServiceTests()
        {
            var snackbar = new Mock<ISnackbar>();
            _runner = new ApiCallRunner(snackbar.Object);
        }

        [Fact]
        public async Task CreateAsync_Should_Return_Id_When_Api_Succeeds()
        {
            var createdId = Guid.NewGuid();
            _api.Setup(x => x.InvoicesPOSTAsync(
                    It.IsAny<string>(),
                    It.IsAny<Recuria.Client.CreateInvoiceRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdId);

            var service = new InvoiceAppService(_api.Object, _runner);
            var result = await service.CreateAsync(new Recuria.Client.CreateInvoiceRequest
            {
                OrganizationId = Guid.NewGuid(),
                Amount = 49.0,
                Description = "Monthly"
            });

            result.Success.Should().BeTrue();
            result.Data.Should().Be(createdId);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_NotFound()
        {
            _api.Setup(x => x.InvoicesGETAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(404, "{\"title\":\"Not Found\"}"));

            var service = new InvoiceAppService(_api.Object, _runner);
            var result = await service.GetByIdAsync(Guid.NewGuid());

            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Unable to load invoice");
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
