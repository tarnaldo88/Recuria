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
    public sealed class UsersServerTableBehaviorTests : TestContext
    {
        public UsersServerTableBehaviorTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddMudServices();
        }

        [Fact]
        public void UsersTable_Should_Show_Empty_State()
        {
            var orgId = Guid.NewGuid();
            var usersApi = BuildUsersApi(orgId, AppResult<Recuria.Client.UserSummaryDtoPagedResult>.Ok(
                new Recuria.Client.UserSummaryDtoPagedResult
                { 
                    Items = Array.Empty<Recuria.Client.UserSummaryDto>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 10
                }));

            RegisterUsersPageServices(orgId, usersApi.Object);

            var cut = RenderComponent<Users>();

            cut.WaitForAssertion(() => cut.Markup.Should().Contain("No users found."));

        }
    }
}
