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

            var organizationId =
                await createOrgResponse.Content.ReadFromJsonAsync<Guid>();

            // Add user
            var addUser = new AddUserRequest
            {
                UserId = userId,
                Role = UserRole.Member
            };

            var addUserResponse =
                await Client.PostAsJsonAsync(
                    $"/api/organizations/{organizationId}/users",
                    addUser);

            Assert.Equal(HttpStatusCode.NoContent, addUserResponse.StatusCode);

            // Change role
            var changeRole = new ChangeUserRoleRequest
            {
                NewRole = UserRole.Admin
            };

            var changeRoleResponse =
                await Client.PutAsJsonAsync(
                    $"/api/organizations/{organizationId}/users/{userId}/role",
                    changeRole);

            Assert.Equal(HttpStatusCode.NoContent, changeRoleResponse.StatusCode);

            // Remove user
            var removeUserResponse =
                await Client.DeleteAsync(
                    $"/api/organizations/{organizationId}/users/{userId}");

            Assert.Equal(HttpStatusCode.NoContent, removeUserResponse.StatusCode);
        }

        private async Task SeedUser(Guid userId)
        {
            await Client.PostAsJsonAsync("/api/users", new
            {
                Id = userId,
                Email = $"{userId}@test.com"
            });
        }
    }
}
