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
            await SeedUser(ownerId);

            var createOrg = new CreateOrganizationRequest
            {
                Name = "Trial Org",
                OwnerId = ownerId
            };

            // Act – create organization
            var response =
                await Client.PostAsJsonAsync("/api/organizations", createOrg);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var organizationId =
                await response.Content.ReadFromJsonAsync<Guid>();

            // Act – query current subscription
            var subResponse =
                await Client.GetAsync($"/api/subscriptions/current/{organizationId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, subResponse.StatusCode);

            var subscription =
                await subResponse.Content
                    .ReadFromJsonAsync<SubscriptionDetailsDto>();

            Assert.NotNull(subscription);
            Assert.Equal(PlanType.Trial, subscription.PlanCode);
            Assert.Equal(SubscriptionStatus.Active, subscription.Status);
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
