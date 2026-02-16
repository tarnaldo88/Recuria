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
    public sealed class InvoicesServerTableBehaviorTests : TestContext
    {
        public InvoicesServerTableBehaviorTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddMudServices();
        }

        [Fact]
        public void InvoicesTable_Search_Should_Trigger_Server_Reload()
        {
            var orgId = Guid.NewGuid();
            var api = new Mock<IInvoiceAppService>();
            api.Setup(x => x.GetPageAsync(
                    orgId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()))
                .ReturnsAsync(AppResult<Recuria.Client.InvoiceListItemDtoPagedResult>.Ok(
                    new Recuria.Client.InvoiceListItemDtoPagedResult
                    {
                        Items = Array.Empty<Recuria.Client.InvoiceListItemDto>(),
                        TotalCount = 0,
                        Page = 1,
                        PageSize = 10
                    }));

            Services.AddSingleton(CreateAuthState(orgId));
            Services.AddSingleton<IInvoiceAppService>(api.Object);
            Services.AddSingleton<IUserContextService>(new FakeUserContextService(new UserContext { IsAuthenticated = true, Role = "Owner" }));
            Services.AddSingleton(Mock.Of<MudBlazor.IDialogService>());

            var cut = RenderComponent<Invoices>();

            cut.WaitForAssertion(() => api.Verify(x => x.GetPageAsync(
                orgId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.AtLeastOnce));

            var input = cut.Find("input[placeholder='Search by status']");
            input.Input("paid");
            input.Blur();

            cut.WaitForAssertion(() => api.Verify(x => x.GetPageAsync(
                orgId, It.IsAny<int>(), It.IsAny<int>(), "paid", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.AtLeastOnce));
        }

    }
}
