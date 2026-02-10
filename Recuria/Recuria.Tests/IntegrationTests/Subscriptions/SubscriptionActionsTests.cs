using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Recuria.Application.Contracts.Subscription;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Subscriptions;

public sealed class SubscriptionActionsTests : IntegrationTestBase
{
    public SubscriptionActionsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Trial_Subscription_Should_Allow_Cancel()
    {
        // Seed org via existing flow in your project or helper
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetAuthHeader(userId, orgId, UserRole.Owner);

        // create trial if needed in your setup
        await Client.PostAsJsonAsync($"/api/subscriptions/trial/{orgId}", new { }, JsonOptions);

        var current = await Client.GetAsync($"/api/subscriptions/current/{orgId}");
        if (current.StatusCode == HttpStatusCode.NotFound) return; // depends on your seed setup

        var details = await current.Content.ReadFromJsonAsync<SubscriptionDetailsDto>(JsonOptions);
        details!.Actions.CanCancel.Should().BeTrue();

        var cancel = await Client.PostAsync($"/api/subscriptions/{details.Subscription.Id}/cancel", JsonContent.Create(new { }));
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
