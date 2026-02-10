using FluentAssertions;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace Recuria.Tests.IntegrationTests.Ops;

public sealed class OutboxAuthorizationTests : IntegrationTestBase
{
    public OutboxAuthorizationTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Theory]
    [InlineData(UserRole.Owner, HttpStatusCode.OK)]
    [InlineData(UserRole.Admin, HttpStatusCode.OK)] // expects OutboxController policy = AdminOrOwner
    [InlineData(UserRole.Member, HttpStatusCode.Forbidden)]
    public async Task DeadLettered_Endpoint_Should_Enforce_Role_Policy(UserRole role, HttpStatusCode expected)
    {
        var orgId = Guid.NewGuid();
        SetAuthHeader(Guid.NewGuid(), orgId, role);

        var response = await Client.GetAsync("/api/outbox/dead-lettered?take=10");
        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.Owner, HttpStatusCode.NotFound)]
    [InlineData(UserRole.Admin, HttpStatusCode.NotFound)] // authorized, but fake id doesn't exist
    [InlineData(UserRole.Member, HttpStatusCode.Forbidden)]
    public async Task Retry_Endpoint_Should_Enforce_Role_Policy(UserRole role, HttpStatusCode expected)
    {
        var orgId = Guid.NewGuid();
        SetAuthHeader(Guid.NewGuid(), orgId, role);

        var fakeId = Guid.NewGuid();
        var response = await Client.PostAsJsonAsync($"/api/outbox/{fakeId}/retry", new { });

        response.StatusCode.Should().Be(expected);
    }
}
