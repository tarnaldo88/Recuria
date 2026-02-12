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
    public sealed class InvoicesPageComponentTests : TestContext
    {
        public InvoicesPageComponentTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Render_Should_Show_ReadOnly_Invoice_Message_For_Member()
        {
            var orgId = Guid.NewGuid();
            var invoicesApi = new Mock<IInvoiceAppService>();
            invoicesApi.Setup(x => x.GetByOrganizationAsync(orgId, It.IsAny<bool>()))
                .ReturnsAsync(AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>.Ok(new List<Recuria.Client.InvoiceListItemDto>()));

            Services.AddMudServices();
            Services.AddSingleton(CreateAuthState(orgId));
            Services.AddSingleton<IInvoiceAppService>(invoicesApi.Object);
            Services.AddSingleton<IUserContextService>(new FakeUserContextService(new UserContext
            {
                IsAuthenticated = true,
                Role = "Member"
            }));

            var cut = RenderComponent<Invoices>();

            cut.WaitForAssertion(() =>
                cut.Markup.Should().Contain("Creating invoices requires Admin or Owner role."));
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
