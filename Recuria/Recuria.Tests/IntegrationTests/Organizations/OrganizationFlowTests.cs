using Recuria.Application.Requests;
using Recuria.Domain;
//using Recuria.Domain.Enums;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Recuria.Domain.Enums;
using Recuria.Application.Contracts.Organizations;
using Microsoft.Extensions.DependencyInjection;

namespace Recuria.Tests.IntegrationTests.Organizations
{
    public class OrganizationFlowTests : IntegrationTestBase
    {
        public OrganizationFlowTests(CustomWebApplicationFactory factory)
        : base(factory) { }

        [Fact]
        public async Task CreateOrganization_AddUser_ChangeRole_RemoveUser_Works()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var bootstrapOrgId = Guid.NewGuid();

            // Bootstrap auth for create user/org (org id can be arbitrary here)
            SetAuthHeader(ownerId, bootstrapOrgId, UserRole.Owner);

            await SeedUser(ownerId);
            await SeedUser(userId);

            // Create organization
            var createOrg = new CreateOrganizationRequest
            {
                Name = "Test Org",
                OwnerId = ownerId
            };

            var createOrgResponse =
                await Client.PostAsJsonAsync("/api/organizations", createOrg);

            Assert.Equal(HttpStatusCode.Created, createOrgResponse.StatusCode);

            var createdOrg =
                await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>(JsonOptions);
            Assert.NotNull(createdOrg);
            var organizationId = createdOrg!.Id;

            // Update auth to the created organization for org-scoped endpoints
            SetAuthHeader(ownerId, organizationId, UserRole.Owner);

            // Add user
            var addUser = new AddUserRequest
            {
                UserId = userId,
                Role = UserRole.Member,
                Email = $"{userId}@test.com",
                Name = "Member User"
            };

            var addUserResponse =
                await Client.PostAsJsonAsync(
                    $"/api/organizations/{organizationId}/users",
                    addUser,
                    JsonOptions);

            Assert.Equal(HttpStatusCode.NoContent, addUserResponse.StatusCode);

            // Change role
            var changeRole = new ChangeUserRoleRequest
            {
                NewRole = UserRole.Admin
            };

            var changeRoleResponse =
                await Client.PutAsJsonAsync(
                    $"/api/organizations/{organizationId}/users/{userId}/role",
                    changeRole,
                    JsonOptions);

            Assert.Equal(HttpStatusCode.NoContent, changeRoleResponse.StatusCode);

            // Remove user
            var removeUserResponse =
                await Client.DeleteAsync(
                    $"/api/organizations/{organizationId}/users/{userId}");

            Assert.Equal(HttpStatusCode.NoContent, removeUserResponse.StatusCode);
        }

        private async Task SeedUser(Guid userId)
        {
            using var scope = Factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<Recuria.Application.Interface.Abstractions.IUserRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<Recuria.Application.Interface.Abstractions.IUnitOfWork>();

            var user = new Recuria.Domain.User($"{userId}@test.com", "Test User") { Id = userId };
            await users.AddAsync(user, CancellationToken.None);
            await uow.CommitAsync(CancellationToken.None);
        }
    }
}
