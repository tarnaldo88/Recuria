using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class OpsAppServiceTests
    {
        private readonly Mock<Recuria.Client.IRecuriaApiClient> _api = new();
        private readonly ApiCallRunner _runner;

        public OpsAppServiceTests()
        {
            var snackbar = new Mock<ISnackbar>();
            _runner = new ApiCallRunner(snackbar.Object);
        }

        [Fact]
        public async Task RetryAsync_Should_Treat_204_Exception_As_Success()
        {
            _api.Setup(x => x.RetryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(204, string.Empty));

            var service = new OpsAppService(_api.Object, _runner);
            var result = await service.RetryAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetDeadLetteredPageAsync_Should_Return_Data_When_Api_Succeeds()
        {
            var items = new List<Recuria.Client.DeadLetteredOutboxItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "SubscriptionActivated",
                    Error = "None",
                    RetryCount = 1,
                    DeadLetteredOnUtc = DateTime.UtcNow
                }
            };

            const int page = 1;
            const int pageSize = 50;
            const string search = "sub";
            const string sortBy = "deadLetteredOnUtc";
            const string sortDir = "desc";

            _api.Setup(x => x.DeadLetteredAsync(
                    page,
                    pageSize,
                    search,
                    sortBy,
                    sortDir,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Recuria.Client.DeadLetteredOutboxItemPagedResult
                {
                    Items = items,
                    Page = 1,
                    PageSize = 50,
                    TotalCount = items.Count
                });

            var service = new OpsAppService(_api.Object, _runner);
            var result = await service.GetDeadLetteredPageAsync(page, pageSize, search, sortBy, sortDir);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeNull();
            result.Data.Items!.Count.Should().Be(1);
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
