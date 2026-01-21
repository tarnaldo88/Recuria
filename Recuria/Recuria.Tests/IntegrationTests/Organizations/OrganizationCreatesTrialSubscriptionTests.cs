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

namespace Recuria.Tests.IntegrationTests.Organizations
{
    public class OrganizationCreatesTrialSubscriptionTests
    : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly IOrganizationService _organizationService;
        private readonly ISubscriptionQueries _subscriptionQueries;
        private readonly IUserRepository _users;

        public OrganizationCreatesTrialSubscriptionTests(
            CustomWebApplicationFactory factory)
        {
            _organizationService =
                factory.Services.GetRequiredService<IOrganizationService>();

            _subscriptionQueries =
                factory.Services.GetRequiredService<ISubscriptionQueries>();

            _users =
                factory.Services.GetRequiredService<IUserRepository>();
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
            subscription!.Status.Should().Be(SubscriptionStatus.Active);
            subscription.Plan.Should().Be(PlanType.Trial);
        }
    }
}
