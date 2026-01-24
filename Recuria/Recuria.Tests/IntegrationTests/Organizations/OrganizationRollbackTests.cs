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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Recuria.Domain.Events.Organization;
using Recuria.Tests.IntegrationTests.TestDoubles;

namespace Recuria.Tests.IntegrationTests.Organizations
{
    public class OrganizationRollbackTests : IClassFixture<OrganizationRollbackTests.FailingOrganizationCreatedFactory>, IDisposable
    {
        private readonly IOrganizationService _service;
        private readonly IOrganizationQueries _orgQueries;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;
        private readonly IServiceScope _scope;

        public OrganizationRollbackTests(FailingOrganizationCreatedFactory factory)
        {
            _scope = factory.Services.CreateScope();
            _service = _scope.ServiceProvider.GetRequiredService<IOrganizationService>();
            _orgQueries = _scope.ServiceProvider.GetRequiredService<IOrganizationQueries>();
            _users = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
            _uow = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        }

        [Fact]
        public async Task CreateOrganization_Should_Rollback_When_DomainEventHandler_Fails()
        {
            // Arrange
            var owner = new User("owner@test.com", "ownerName");
            await _users.AddAsync(owner, CancellationToken.None);
            await _uow.CommitAsync(CancellationToken.None);

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

        public sealed class FailingOrganizationCreatedFactory : CustomWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDomainEventHandler<OrganizationCreatedDomainEvent>>();
                    services.AddScoped<
                        IDomainEventHandler<OrganizationCreatedDomainEvent>,
                        FailingOrganizationCreatedHandler>();
                });
            }
        }
    }
}
