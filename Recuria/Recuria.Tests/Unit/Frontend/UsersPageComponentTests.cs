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
    public sealed class UsersPageComponentTests : TestContext
    {
        public UsersPageComponentTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Render_Should_Show_ForbiddenState_When_User_Cannot_Manage_Users()
        {
            Services.AddMudServices();
            Services.AddSingleton(CreateAuthState(Guid.NewGuid()));
            Services.AddSingleton<IUserContextService>(new FakeUserContextService(new UserContext
            {
                IsAuthenticated = true,
                Role = "Member"
            }));
            Services.AddSingleton(Mock.Of<IOrganizationAppService>());
            Services.AddSingleton(Mock.Of<IUserAppService>());

            var cut = RenderComponent<Users>();

            cut.Markup.Should().Contain("Users management requires Admin or Owner role.");
        }

        [Fact]
        public void Render_Should_Load_Users_For_Admin()
        {
            var orgId = Guid.NewGuid();
            var usersApi = new Mock<IUserAppService>();
            usersApi.Setup(x => x.GetAllAsync(orgId, It.IsAny<bool>()))
                .ReturnsAsync(AppResult<ICollection<Recuria.Client.UserSummaryDto>>.Ok(new List<Recuria.Client.UserSummaryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Alice",
                        Email = "alice@example.com",
                        Role = Recuria.Client.UserRole.Admin
                    }
                }));

            Services.AddMudServices();
            Services.AddSingleton(CreateAuthState(orgId));
            Services.AddSingleton<IUserContextService>(new FakeUserContextService(new UserContext
            {
                IsAuthenticated = true,
                Role = "Admin"
            }));
            Services.AddSingleton(Mock.Of<IOrganizationAppService>());
            Services.AddSingleton(usersApi.Object);

            var cut = RenderComponent<Users>();

            cut.WaitForAssertion(() =>
            {
                cut.Markup.Should().Contain("alice@example.com");
                cut.Markup.Should().Contain("Edit Role");
            });
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
