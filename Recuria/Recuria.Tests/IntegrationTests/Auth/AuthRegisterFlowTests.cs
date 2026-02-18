using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Auth;

public sealed class AuthRegisterFlowTests : IntegrationTestBase
{
    public AuthRegisterFlowTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_Then_Login_ByOrganizationName_Should_Succeed()
    {
        var organizationName = $"Org-{Guid.NewGuid():N}";
        var email = $"owner-{Guid.NewGuid():N}@recuria.local";
        var password = "StrongPass!123";
        var ownerName = "Owner User";

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organizationName,
            ownerName,
            email,
            password
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var registerJson = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        var registerRoot = registerJson.RootElement;

        registerRoot.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        registerRoot.GetProperty("organizationId").GetGuid().Should().NotBe(Guid.Empty);
        registerRoot.GetProperty("email").GetString().Should().Be(email);

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            organizationName,
            email,
            password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var loginRoot = loginJson.RootElement;

        loginRoot.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        loginRoot.GetProperty("organizationId").GetGuid()
            .Should().Be(registerRoot.GetProperty("organizationId").GetGuid());
        loginRoot.GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Register_With_Duplicate_OrganizationName_Should_Return_Conflict()
    {
        var organizationName = $"DupOrg-{Guid.NewGuid():N}";
        var password = "StrongPass!123";

        var first = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organizationName,
            ownerName = "First Owner",
            email = $"first-{Guid.NewGuid():N}@recuria.local",
            password
        });

        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organizationName,
            ownerName = "Second Owner",
            email = $"second-{Guid.NewGuid():N}@recuria.local",
            password
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
