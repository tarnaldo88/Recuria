using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Recuria.Tests.IntegrationTests.Auth
{
    public sealed class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuthorizationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetOrganization_Forbid_When_OrgMismatch()
        {
            var orgA = await SeedOrganizationAsync();
            var orgB = await SeedOrganizationAsync();

            using var client = CreateClientWithToken(orgA, UserRole.Owner);
            var response = await client.GetAsync($"/api/organizations/{orgB}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetOrganization_Allows_When_OrgMatches()
        {
            var orgId = await SeedOrganizationAsync();

            using var client = CreateClientWithToken(orgId, UserRole.Member);
            var response = await client.GetAsync($"/api/organizations/{orgId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetCurrentSubscription_Forbid_When_OrgMismatch()
        {
            var orgA = await SeedOrganizationWithSubscriptionAsync();
            var orgB = await SeedOrganizationAsync();

            using var client = CreateClientWithToken(orgB, UserRole.Member);
            var response = await client.GetAsync($"/api/subscriptions/current/{orgA}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetInvoiceDetails_Forbid_When_OrgMismatch()
        {
            var (orgA, invoiceId) = await SeedOrganizationWithInvoiceAsync();
            var orgB = await SeedOrganizationAsync();

            using var client = CreateClientWithToken(orgB, UserRole.Member);
            var response = await client.GetAsync($"/api/invoices/{invoiceId}");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private HttpClient CreateClientWithToken(Guid organizationId, UserRole role)
        {
            var client = _factory.CreateClient();
            var token = CreateJwt(Guid.NewGuid(), organizationId, role);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static string CreateJwt(Guid userId, Guid organizationId, UserRole role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("org_id", organizationId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(CustomWebApplicationFactory.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: CustomWebApplicationFactory.JwtIssuer,
                audience: CustomWebApplicationFactory.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<Guid> SeedOrganizationAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var owner = new User($"{Guid.NewGuid()}@auth.test", "Auth Owner");
            await users.AddAsync(owner, CancellationToken.None);

            var org = new Organization($"Org-{Guid.NewGuid()}");
            org.AddUser(owner, UserRole.Owner);
            await orgs.AddAsync(org, CancellationToken.None);

            await uow.CommitAsync(CancellationToken.None);

            return org.Id;
        }

        private async Task<Guid> SeedOrganizationWithSubscriptionAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
            var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var owner = new User($"{Guid.NewGuid()}@auth.test", "Auth Owner");
            await users.AddAsync(owner, CancellationToken.None);

            var org = new Organization($"Org-{Guid.NewGuid()}");
            org.AddUser(owner, UserRole.Owner);
            await orgs.AddAsync(org, CancellationToken.None);

            var now = DateTime.UtcNow;
            var subscription = new Subscription(
                org,
                PlanType.Pro,
                SubscriptionStatus.Active,
                now.AddDays(-5),
                now.AddDays(25));

            await subs.AddAsync(subscription, CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);

            return org.Id;
        }

        private async Task<(Guid OrgId, Guid InvoiceId)> SeedOrganizationWithInvoiceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
            var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var db = scope.ServiceProvider.GetRequiredService<RecuriaDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var owner = new User($"{Guid.NewGuid()}@auth.test", "Auth Owner");
            await users.AddAsync(owner, CancellationToken.None);

            var org = new Organization($"Org-{Guid.NewGuid()}");
            org.AddUser(owner, UserRole.Owner);
            await orgs.AddAsync(org, CancellationToken.None);

            var now = DateTime.UtcNow;
            var subscription = new Subscription(
                org,
                PlanType.Pro,
                SubscriptionStatus.Active,
                now.AddDays(-5),
                now.AddDays(25));

            await subs.AddAsync(subscription, CancellationToken.None);

            var invoice = new Invoice(subscription.Id, 10m)
            {
                Id = Guid.NewGuid()
            };
            db.Invoices.Add(invoice);

            await uow.CommitAsync(CancellationToken.None);

            return (org.Id, invoice.Id);
        }
    }
}
