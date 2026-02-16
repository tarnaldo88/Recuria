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

        [Fact]
        public void UsersTable_Should_Show_Error_State()
        {
            var orgId = Guid.NewGuid();
            var usersApi = BuildUsersApi(orgId, AppResult<Recuria.Client.UserSummaryDtoPagedResult>.Fail("Unable to load users."));

            RegisterUsersPageServices(orgId, usersApi.Object);

            var cut = RenderComponent<Users>();

            cut.WaitForAssertion(() => cut.Markup.Should().Contain("Unable to load users."));
        }

        [Fact]
        public void UsersTable_Should_Transition_From_Loading_To_Empty()
        {
            var orgId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<AppResult<Recuria.Client.UserSummaryDtoPagedResult>>();
            var usersApi = new Mock<IUserAppService>();

            usersApi.Setup(x => x.GetPageAsync(
                    orgId,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool>()))
                .Returns(() => tcs.Task);

            RegisterUsersPageServices(orgId, usersApi.Object);

            var cut = RenderComponent<Users>();

            cut.WaitForAssertion(() =>
                usersApi.Verify(x => x.GetPageAsync(
                    orgId,
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool>()), Times.Once));

            cut.Markup.Should().NotContain("No users found.");

            tcs.SetResult(AppResult<Recuria.Client.UserSummaryDtoPagedResult>.Ok(
                new Recuria.Client.UserSummaryDtoPagedResult
                {
                    Items = Array.Empty<Recuria.Client.UserSummaryDto>(),
                    TotalCount = 0,
                    Page = 1,
                    PageSize = 10
                }));

            cut.WaitForAssertion(() => cut.Markup.Should().Contain("No users found."));
        }


    }
}
