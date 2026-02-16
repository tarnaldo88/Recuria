using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Contracts.Common;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Recuria.Tests.IntegrationTests.Organizations;

public sealed class OrganizationUsersPagingContractTests : IntegrationTestBase
{
    public OrganizationUsersPagingContractTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetUsers_PagingAndTotalCount_Should_Respect_Bounds()
    {
        var ownerId = Guid.NewGuid();
        var orgId = await SeedOrganizationWithUsersAsync(ownerId, userCount: 8, emailPrefix: "bounds");
        SetAuthHeader(ownerId, orgId, UserRole.Owner);

        var response = await Client.GetAsync($"/api/organizations/{orgId}/users?page=0&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>(JsonOptions);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(5);
        page.TotalCount.Should().Be(8);
        page.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetUsers_SortAndSearch_Should_Return_Correct_Set()
    {
        var ownerId = Guid.NewGuid();
        var orgId = await SeedOrganizationWithUsersAsync(ownerId, userCount: 8, emailPrefix: "sort");
        SetAuthHeader(ownerId, orgId, UserRole.Owner);

        var searchResponse = await Client.GetAsync($"/api/organizations/{orgId}/users?page=1&pageSize=10&search=sort1&sortBy=email&sortDir=desc");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchPage = await searchResponse.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>(JsonOptions);
        searchPage.Should().NotBeNull();
        searchPage!.TotalCount.Should().Be(1);
        searchPage.Items.Should().ContainSingle();
        searchPage.Items[0].Email.Should().Contain("sort1@test.local");

        var sortResponse = await Client.GetAsync($"/api/organizations/{orgId}/users?page=1&pageSize=10&sortBy=email&sortDir=desc");
        sortResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sortPage = await sortResponse.Content.ReadFromJsonAsync<PagedResult<UserSummaryDto>>(JsonOptions);
        sortPage.Should().NotBeNull();
        sortPage!.Items.Select(x => x.Email).Should().BeInDescendingOrder();
        sortPage.TotalCount.Should().Be(8);
    }

    private async Task<Guid> SeedOrganizationWithUsersAsync(Guid ownerId, int userCount, string emailPrefix)
    {
        using var scope = Factory.Services.CreateScope();
        var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User($"{emailPrefix}-owner@test.local", "Owner User") { Id = ownerId };
        await users.AddAsync(owner, CancellationToken.None);

        var org = new Organization($"Org-{Guid.NewGuid()}");
        org.AddUser(owner, UserRole.Owner);

        for (var i = 1; i < userCount; i++)
        {
            var user = new User($"{emailPrefix}{i}@test.local", $"User {i}") { Id = Guid.NewGuid() };
            await users.AddAsync(user, CancellationToken.None);
            org.AddUser(user, UserRole.Member);
        }

        await orgs.AddAsync(org, CancellationToken.None);
        await uow.CommitAsync(CancellationToken.None);

        return org.Id;
    }
}
