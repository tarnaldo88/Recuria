using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class SubscriptionAppServiceTests
    {
        private readonly Mock<Recuria.Client.IRecuriaApiClient> _api = new();
        private readonly ApiCallRunner _runner;

        public SubscriptionAppServiceTests()
        {
            var snackbar = new Mock<ISnackbar>();
            _runner = new ApiCallRunner(snackbar.Object);
        }

        [Fact]
        public async Task UpgradeAsync_Should_Treat_204_Exception_As_Success()
        {
            _api.Setup(x => x.UpgradeAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Recuria.Client.UpgradeSubscriptionRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(204, string.Empty));

            var service = new SubscriptionAppService(_api.Object, _runner);
            var result = await service.UpgradeAsync(
                Guid.NewGuid(),
                new Recuria.Client.UpgradeSubscriptionRequest { NewPlan = Recuria.Client.PlanType.Pro });

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CancelAsync_Should_Treat_204_Exception_As_Success()
        {
            _api.Setup(x => x.CancelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(CreateApiException(204, string.Empty));

            var service = new SubscriptionAppService(_api.Object, _runner);
            var result = await service.CancelAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetCurrentAsync_Should_Return_Data_When_Successful()
        {
            var details = new Recuria.Client.SubscriptionDetailsDto
            {
                Subscription = new Recuria.Client.SubscriptionDto
                {
                    Id = Guid.NewGuid(),
                    PlanCode = Recuria.Client.PlanType.Free,
                    Status = Recuria.Client.SubscriptionStatus.Trial,
                    PeriodStart = DateTime.UtcNow.AddDays(-1),
                    PeriodEnd = DateTime.UtcNow.AddDays(13),
                    IsTrial = true,
                    IsPastDue = false
                },
                Actions = new Recuria.Client.SubscriptionActionsDto
                {
                    CanActivate = true,
                    CanCancel = true,
                    CanUpgrade = true
                }
            };

            _api.Setup(x => x.CurrentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(details);

            var service = new SubscriptionAppService(_api.Object, _runner);
            var result = await service.GetCurrentAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Subscription.Status.Should().Be(Recuria.Client.SubscriptionStatus.Trial);
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
