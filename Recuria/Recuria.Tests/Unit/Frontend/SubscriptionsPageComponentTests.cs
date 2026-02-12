using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor.Services;
using Recuria.Blazor.Pages;
using Recuria.Blazor.Services;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class SubscriptionsPageComponentTests : TestContext
    {
        [Fact]
        public void Render_Should_Show_ReadOnly_Billing_Message_For_Member()
        {
            var orgId = Guid.NewGuid();
            var subscriptionsApi = new Mock<ISubscriptionAppService>();
            subscriptionsApi.Setup(x => x.GetCurrentAsync(orgId, false))
                .ReturnsAsync(AppResult<Recuria.Client.SubscriptionDetailsDto>.Ok(new Recuria.Client.SubscriptionDetailsDto
                {
                    Subscription = new Recuria.Client.SubscriptionDto
                    {
                        Id = Guid.NewGuid(),
                        PlanCode = Recuria.Client.PlanType.Free,
                        Status = Recuria.Client.SubscriptionStatus.Trial,
                        IsTrial = true,
                        IsPastDue = false,
                        PeriodStart = DateTimeOffset.UtcNow.AddDays(-1),
                        PeriodEnd = DateTimeOffset.UtcNow.AddDays(13)
                    },
                    Actions = new Recuria.Client.SubscriptionActionAvailabilityDto
                    {
                        CanCancel = true,
                        CanUpgrade = true,
                        CanActivate = true
                    }
                }));

            Services.AddMudServices();
            Services.AddSingleton(CreateAuthState(orgId));
            Services.AddSingleton<ISubscriptionAppService>(subscriptionsApi.Object);
            Services.AddSingleton<IUserContextService>(new FakeUserContextService(new UserContext
            {
                IsAuthenticated = true,
                Role = "Member"
            }));

            var cut = RenderComponent<Subscriptions>();

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Plan changes require Admin or Owner role."));
        }

        private static AuthState CreateAuthState(Guid orgId)
        {
            var js = new Mock<IJSRuntime>();
            js.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
                .Returns((string _, object?[] args) =>
                {
                    var key = args.Length > 0 ? args[0]?.ToString() : null;
                    return key == "recuria.orgId"
                        ? new ValueTask<string?>(orgId.ToString())
                        : new ValueTask<string?>((string?)null);
                });

            return new AuthState(new TokenStorage(js.Object));
        }

        private sealed class FakeUserContextService : IUserContextService
        {
            private readonly UserContext _context;

            public FakeUserContextService(UserContext context)
            {
                _context = context;
            }

            public Task<UserContext> GetAsync(bool forceRefresh = false) =>
                Task.FromResult(_context);
        }
    }
}
