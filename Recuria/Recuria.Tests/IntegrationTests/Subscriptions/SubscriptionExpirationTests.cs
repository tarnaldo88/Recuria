using FluentAssertion;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Repositories;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace Recuria.Tests.IntegrationTests.Subscriptions
{
    public class SubscriptionExpirationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly ISubscriptionQueries _queries;
        private readonly IOrganizationRepository _organizations;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;

        public SubscriptionExpirationTests(CustomWebApplicationFactory factory)
        {
            var services = factory.Services;

            _subscriptions = services.GetRequiredService<ISubscriptionRepository>();
            _queries = services.GetRequiredService<ISubscriptionQueries>();
            _organizations = services.GetRequiredService<IOrganizationRepository>();
            _users = services.GetRequiredService<IUserRepository>();
            _uow = services.GetRequiredService<IUnitOfWork>();
        }

        [Fact]
        public async Task ActiveSubscription_Should_Expire_When_PeriodEnded()
        {
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: DateTime.UtcNow.AddDays(-30),
                periodEnd: DateTime.UtcNow.AddDays(-1));

            subscription.Expire(DateTime.UtcNow);
            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            var reloaded = await _subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);

            reloaded.Should().NotBeNull();
            reloaded!.Status.Should().Be(SubscriptionStatus.Expired);
        }

        //Noticing trend of having to make active subscriptions for tests. Making Helper method.
        //Helper method is going to create a persisted org and persisted active subscription.
        private async Task<(Organization Org, Subscription Subscription)> CreateActiveSubscriptionAsync(DateTime periodStart, DateTime periodEnd)
        {
            var owner = new User($"{Guid.NewGuid()}@expiretest.com", "TestName");
            await _users.AddAsync(owner, CancellationToken.None);

            var org = new Organization($"Org-{Guid.NewGuid()}");
            org.AddUser(owner, UserRole.Owner);
            await _organizations.AddAsync(org, CancellationToken.None);

            var subscription = new Subscription(
                org,
                PlanType.Pro,
                SubscriptionStatus.Active,
                periodStart,
                periodEnd);

            await _subscriptions.AddAsync(subscription, CancellationToken.None);
            await _uow.CommitAsync();

            return (org, subscription);
        }
    }
}
