using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Subscriptions;

public sealed class SubscriptionActionsTests : IntegrationTestBase
{
    public SubscriptionActionsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Trial_Subscription_Should_Allow_Cancel()
    {
        var (orgId, ownerId) = await SeedOrganizationWithOwnerAsync();
        SetAuthHeader(ownerId, orgId, UserRole.Owner);

        var createTrialResponse = await Client.PostAsJsonAsync($"/api/subscriptions/trial/{orgId}", new { }, JsonOptions);
        createTrialResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var currentResponse = await Client.GetAsync($"/api/subscriptions/current/{orgId}");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await currentResponse.Content.ReadFromJsonAsync<SubscriptionDetailsDto>(JsonOptions);
        details.Should().NotBeNull();
        details!.Subscription.Status.Should().Be(SubscriptionStatus.Trial);
        details.Actions.CanCancel.Should().BeTrue();

        var cancelResponse = await Client.PostAsync(
            $"/api/subscriptions/{details.Subscription.Id}/cancel",
            JsonContent.Create(new { }));

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<(Guid OrganizationId, Guid OwnerId)> SeedOrganizationWithOwnerAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User($"{Guid.NewGuid()}@sub.test", "Subscription Owner");
        await users.AddAsync(owner, CancellationToken.None);

        var org = new Organization($"Org-{Guid.NewGuid()}");
        org.AddUser(owner, UserRole.Owner);
        await orgs.AddAsync(org, CancellationToken.None);

        await uow.CommitAsync(CancellationToken.None);

        return (org.Id, owner.Id);
    }
}
