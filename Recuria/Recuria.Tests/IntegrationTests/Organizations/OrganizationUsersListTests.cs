using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Contracts.Organizations;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Recuria.Tests.IntegrationTests.Organizations;

public sealed class OrganizationUsersListTests : IntegrationTestBase
{
    public OrganizationUsersListTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetUsers_Should_Return_Users_For_Same_Organization()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var orgId = await SeedOrganizationWithUsersAsync(ownerId, memberId);

        SetAuthHeader(ownerId, orgId, UserRole.Owner);

        var response = await Client.GetAsync($"/api/organizations/{orgId}/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<Recuria.Application.Contracts.Common.PagedResult<UserSummaryDto>>(JsonOptions);
        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(2);
        page.Items.Should().Contain(u => u.Id == ownerId && u.Role == UserRole.Owner);
        page.Items.Should().Contain(u => u.Id == memberId && u.Role == UserRole.Member);

    }

    [Fact]
    public async Task GetUsers_Should_Forbid_When_Org_Mismatch()
    {
        var ownerA = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var orgA = await SeedOrganizationWithUsersAsync(ownerA, memberA);

        var ownerB = Guid.NewGuid();
        var memberB = Guid.NewGuid();
        var orgB = await SeedOrganizationWithUsersAsync(ownerB, memberB);

        SetAuthHeader(ownerB, orgB, UserRole.Owner);

        var response = await Client.GetAsync($"/api/organizations/{orgA}/users");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    private async Task<Guid> SeedOrganizationWithUsersAsync(Guid ownerId, Guid memberId)
    {
        using var scope = Factory.Services.CreateScope();
        var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User($"{ownerId}@test.local", "Owner User") { Id = ownerId };
        var member = new User($"{memberId}@test.local", "Member User") { Id = memberId };

        await users.AddAsync(owner, CancellationToken.None);
        await users.AddAsync(member, CancellationToken.None);

        var org = new Organization($"Org-{Guid.NewGuid()}");
        org.AddUser(owner, UserRole.Owner);
        org.AddUser(member, UserRole.Member);

        await orgs.AddAsync(org, CancellationToken.None);
        await uow.CommitAsync(CancellationToken.None);

        return org.Id;
    }
}
