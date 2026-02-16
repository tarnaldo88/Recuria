using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using Recuria.Blazor.Pages;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class OpsServerTableBehaviorTests : TestContext
    {
        public OpsServerTableBehaviorTests()
        {
            Services.AddMudServices();
        }

        [Fact]
        public void OpsTable_Search_And_Sort_Should_Trigger_Server_Reload()
        {
            var api = new Mock<IOpsAppService>();
            api.Setup(x => x.GetDeadLetteredPageAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(AppResult<Recuria.Client.DeadLetteredOutboxItemPagedResult>.Ok(
                    new Recuria.Client.DeadLetteredOutboxItemPagedResult
                    {
                        Items = Array.Empty<Recuria.Client.DeadLetteredOutboxItem>(),
                        TotalCount = 0,
                        Page = 1,
                        PageSize = 10
                    }));

            Services.AddSingleton<IOpsAppService>(api.Object);
            Services.AddSingleton<IUserContextService>(new FakeUserContextService(new UserContext { IsAuthenticated = true, Role = "Admin" }));

            var cut = RenderComponent<Ops>();

            cut.WaitForAssertion(() => api.Verify(x => x.GetDeadLetteredPageAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.AtLeastOnce));

            var input = cut.Find("input[placeholder='Search type or error']");
            input.Input("billing");
            input.Blur();

            cut.WaitForAssertion(() => api.Verify(x => x.GetDeadLetteredPageAsync(
                It.IsAny<int>(), It.IsAny<int>(), "billing", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.AtLeastOnce));

            var typeSort = cut.FindAll(".mud-table-sort-label")
                .First(x => x.TextContent.Contains("Type", StringComparison.OrdinalIgnoreCase));
            typeSort.Click();

            cut.WaitForAssertion(() => api.Verify(x => x.GetDeadLetteredPageAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), "type", It.IsAny<string?>(), It.IsAny<bool>()), Times.AtLeastOnce));
        }

        private sealed class FakeUserContextService : IUserContextService
        {
            private readonly UserContext _ctx;
            public FakeUserContextService(UserContext ctx) => _ctx = ctx;
            public Task<UserContext> GetAsync(bool forceRefresh = false) => Task.FromResult(_ctx);
        }
    }
}
