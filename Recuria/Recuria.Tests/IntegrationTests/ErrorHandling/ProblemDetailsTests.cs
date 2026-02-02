using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Recuria.Tests.IntegrationTests.ErrorHandling
{
    public sealed class ProblemDetailsTests : IntegrationTestBase
    {
        public ProblemDetailsTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task Unauthorized_Returns_ProblemDetails()
        {
            var response = await Client.GetAsync("/api/organizations/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
                Assert.Contains("\"errorCode\":\"auth_required\"", body);
        }

        [Fact]
        public async Task ValidationError_Returns_ProblemDetails_WithErrors()
        {
            var ownerId = Guid.NewGuid();
            var bootstrapOrgId = Guid.NewGuid();
            SetAuthHeader(ownerId, bootstrapOrgId, UserRole.Owner);

            var response = await Client.PostAsJsonAsync("/api/organizations", new
            {
                name = "",
                ownerId
            }, JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"errorCode\":\"validation_error\"", body);
            Assert.Contains("\"errors\"", body);
        }

        [Fact]
        public async Task BusinessRule_Returns_ProblemDetails()
        {
            var orgId = await SeedOrganizationAsync();
            SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

            var response = await Client.PostAsJsonAsync(
                $"/api/organizations/{orgId}/users",
                new { userId = Guid.NewGuid(), role = UserRole.Member },
                JsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"errorCode\":\"business_rule_violation\"", body);
        }

        [Fact]
        public async Task NotFound_Returns_ProblemDetails()
        {
            var orgId = await SeedOrganizationAsync();
            SetAuthHeader(Guid.NewGuid(), orgId, UserRole.Owner);

            var response = await Client.GetAsync($"/api/invoices/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body) && body.Contains("\"errorCode\""))
                Assert.Contains("\"errorCode\":\"not_found\"", body);
        }

        private async Task<Guid> SeedOrganizationAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var orgs = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var owner = new User($"{Guid.NewGuid()}@err.test", "Error Owner");
            await users.AddAsync(owner, CancellationToken.None);

            var org = new Organization($"Org-{Guid.NewGuid()}");
            org.AddUser(owner, UserRole.Owner);
            await orgs.AddAsync(org, CancellationToken.None);

            await uow.CommitAsync(CancellationToken.None);

            return org.Id;
        }
    }
}
