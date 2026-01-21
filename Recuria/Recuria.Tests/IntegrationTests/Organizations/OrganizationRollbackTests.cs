using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Requests;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System.Net.Http.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.IntegrationTests.Organizations
{
    public class OrganizationRollbackTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly IOrganizationService _service;
        private readonly IOrganizationQueries _queries;

        public OrganizationRollbackTests(CustomWebApplicationFactory factory)
        {
            _service = factory.Services.GetRequiredService<IOrganizationService>();
            _queries = factory.Services.GetRequiredService<IOrganizationQueries>();
        }

        [Fact]
        public async Task CreateOrganization_Should_Rollback_When_DomainEventHandler_Fails()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            await SeedUser(ownerId);

            var request = new CreateOrganizationRequest
            {
                Name = "Rollback Org",
                OwnerId = ownerId
            };

            // Act
            Func<Task> act = async () =>
                await _service.CreateOrganizationAsync(request, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Simulated failure");

            // Confirm rollback
            var org = await _queries.FindByNameAsync("Rollback Org", CancellationToken.None);

            org.Should().BeNull();
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
