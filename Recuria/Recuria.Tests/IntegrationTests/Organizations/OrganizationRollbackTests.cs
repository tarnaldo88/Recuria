using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.IntegrationTests.Organizations
{
    public class OrganizationRollbackTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly IOrganizationService _service;
        private readonly IOrganizationQueries _orgQueries;
        private readonly IUserRepository _users;
        private readonly IServiceScope _scope;

        public OrganizationRollbackTests(CustomWebApplicationFactory factory)
        {
            _scope = factory.Services.CreateScope();
            _service = _scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            _orgQueries = _scope.ServiceProvider.GetRequiredService<IOrganizationQueries>();
            _users = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        }

        [Fact]
        public async Task CreateOrganization_Should_Rollback_When_DomainEventHandler_Fails()
        {
            // Arrange
            var owner = new User("owner@test.com", "ownerName");
            await _users.AddAsync(owner, CancellationToken.None);

            var request = new CreateOrganizationRequest
            {
                Name = "Rollback Org",
                OwnerId = owner.Id
            };

            Guid createdOrgId = Guid.Empty;

            // Act
            Func<Task> act = async () =>
            {
                createdOrgId =
                    await _service.CreateOrganizationAsync(request, CancellationToken.None);
            };

            // Assert – handler failure propagates
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Simulated failure");

            // Assert – organization was NOT persisted
            var orgDto = await _orgQueries.GetByIdAsync(createdOrgId, CancellationToken.None);
            orgDto.Should().BeNull();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
