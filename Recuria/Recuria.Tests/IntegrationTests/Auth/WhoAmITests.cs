using System.Net;
using System.Text.Json;
using FluentAssertions;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Auth;

public sealed class WhoAmITests : IntegrationTestBase
{
    public WhoAmITests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task WhoAmI_Should_Return_Subject_Org_And_Role()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        SetAuthHeader(userId, orgId, UserRole.Admin);

        var response = await Client.GetAsync("/api/auth/whoami");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        root.GetProperty("subject").GetString().Should().Be(userId.ToString());
        root.GetProperty("organizationId").GetString().Should().Be(orgId.ToString());
        root.GetProperty("role").GetString().Should().Be(UserRole.Admin.ToString());
        root.GetProperty("claims").ValueKind.Should().Be(JsonValueKind.Array);
    }
}
