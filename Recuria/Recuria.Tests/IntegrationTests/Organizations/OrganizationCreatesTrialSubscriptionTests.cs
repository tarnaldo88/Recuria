using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Domain.Enums;

namespace Recuria.Tests.IntegrationTests.Organizations
{
    public class OrganizationCreatesTrialSubscriptionTests
    : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly IOrganizationService _organizationService;
        private readonly ISubscriptionQueries _subscriptionQueries;
        private readonly IUserRepository _users;
        private readonly IServiceScope _scope;

        public OrganizationCreatesTrialSubscriptionTests(
            CustomWebApplicationFactory factory)
        {
            _scope = factory.Services.CreateScope();
            _organizationService =
                _scope.ServiceProvider.GetRequiredService<IOrganizationService>();

            _subscriptionQueries =
                _scope.ServiceProvider.GetRequiredService<ISubscriptionQueries>();

            _users =
                _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        }

        [Fact]
        public async Task Creating_Organization_Should_Create_Trial_Subscription()
        {
            // Arrange
            var owner = new User("owner@trialtest.com", "ownerName");
            await _users.AddAsync(owner, CancellationToken.None);

            var request = new CreateOrganizationRequest
            {
                Name = "Trial Org",
                OwnerId = owner.Id
            };

            // Act
            var organizationId =
                await _organizationService.CreateOrganizationAsync(
                    request,
                    CancellationToken.None);

            // Assert
            var subscription =
                await _subscriptionQueries.GetCurrentAsync(
                    organizationId,
                    CancellationToken.None);

            subscription.Should().NotBeNull();
            subscription!.Subscription.Status.Should().Be(SubscriptionStatus.Trial);
            subscription.Subscription.PlanCode.Should().Be(PlanType.Free);
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
