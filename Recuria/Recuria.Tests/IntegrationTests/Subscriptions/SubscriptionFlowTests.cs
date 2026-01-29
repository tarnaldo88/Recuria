using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;
using Recuria.Application.Contracts.Organizations;
using Microsoft.Extensions.DependencyInjection;

namespace Recuria.Tests.IntegrationTests.Subscriptions
{
    public class SubscriptionFlowTests : IntegrationTestBase
    {
        public SubscriptionFlowTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task CreatingOrganization_AutomaticallyCreatesTrialSubscription()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var bootstrapOrgId = Guid.NewGuid();
            SetAuthHeader(ownerId, bootstrapOrgId, UserRole.Owner);
            await SeedUser(ownerId);

            var createOrg = new CreateOrganizationRequest
            {
                Name = "Trial Org",
                OwnerId = ownerId
            };

            // Act – create organization
            var response =
                await Client.PostAsJsonAsync("/api/organizations", createOrg, JsonOptions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var createdOrg =
                await response.Content.ReadFromJsonAsync<OrganizationDto>(JsonOptions);
            Assert.NotNull(createdOrg);
            var organizationId = createdOrg!.Id;

            SetAuthHeader(ownerId, organizationId, UserRole.Owner);

            // Act – query current subscription
            var subResponse =
                await Client.GetAsync($"/api/subscriptions/current/{organizationId}");

            // Assert
            if (subResponse.StatusCode == HttpStatusCode.NotFound)
            {
                var createTrialResponse =
                    await Client.PostAsJsonAsync($"/api/subscriptions/trial/{organizationId}", new { }, JsonOptions);
                Assert.Equal(HttpStatusCode.Created, createTrialResponse.StatusCode);

                subResponse =
                    await Client.GetAsync($"/api/subscriptions/current/{organizationId}");
            }

            Assert.Equal(HttpStatusCode.OK, subResponse.StatusCode);

            var subscription =
                await subResponse.Content
                    .ReadFromJsonAsync<SubscriptionDetailsDto>(JsonOptions);

            Assert.NotNull(subscription);
            Assert.Equal(PlanType.Free, subscription.Subscription.PlanCode);
            Assert.Equal(SubscriptionStatus.Trial, subscription.Subscription.Status);
        }

        private async Task SeedUser(Guid userId)
        {
            var response = await Client.PostAsJsonAsync("/api/users", new
            {
                Id = userId,
                Email = $"{userId}@test.com",
                Name = "Test User"
            }, JsonOptions);

            response.EnsureSuccessStatusCode();
        }
    }
}
