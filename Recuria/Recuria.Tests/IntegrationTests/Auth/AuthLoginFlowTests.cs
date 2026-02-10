using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;

namespace Recuria.Tests.IntegrationTests.Auth;

public sealed class AuthLoginFlowTests : IntegrationTestBase
{
    public AuthLoginFlowTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_Logout_Should_Revoke_Previous_AccessToken()
    {
        var organizationId = await SeedOwnerWithPasswordAsync("owner@recuria.local", "StrongPass!123");

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            organizationId,
            email = "owner@recuria.local",
            password = "StrongPass!123"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var whoBeforeLogout = await Client.GetAsync("/api/auth/whoami");
        whoBeforeLogout.StatusCode.Should().Be(HttpStatusCode.OK);

        var logoutResponse = await Client.PostAsync("/api/auth/logout", content: null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var whoAfterLogout = await Client.GetAsync("/api/auth/whoami");
        whoAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_For_InvalidPassword()
    {
        var organizationId = await SeedOwnerWithPasswordAsync("owner2@recuria.local", "StrongPass!123");

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            organizationId,
            email = "owner2@recuria.local",
            password = "wrong-password"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> SeedOwnerWithPasswordAsync(string email, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var organizations = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owner = new User(email, "Owner");
        owner.SetPassword(password);
        await users.AddAsync(owner, CancellationToken.None);

        var organization = new Organization("Auth Org");
        organization.AddUser(owner, UserRole.Owner);
        await organizations.AddAsync(organization, CancellationToken.None);

        await uow.CommitAsync(CancellationToken.None);
        return organization.Id;
    }

    private sealed record AuthResponse(string AccessToken);
}
