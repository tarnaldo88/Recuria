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

namespace Recuria.Tests.IntegrationTests.Subscriptions
{
    public class SubscriptionActivationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly ISubscriptionQueries _queries;
        private readonly IOrganizationRepository _organizations;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;

        public SubscriptionActivationTests(CustomWebApplicationFactory factory)
        {
            var services = factory.Services;

            _subscriptions = services.GetRequiredService<ISubscriptionRepository>();
            _queries = services.GetRequiredService<ISubscriptionQueries>();
            _organizations = services.GetRequiredService<IOrganizationRepository>();
            _users = services.GetRequiredService<IUserRepository>();
            _uow = services.GetRequiredService<IUnitOfWork>();
        }

        [Fact]
        public async Task Activate_Should_SetActive_And_SetNewBillingPeriod_When_Trial()
        {
            var (org, subscription) = await CreateSubscriptionAsync(
                status: SubscriptionStatus.Trial,
                periodStart: DateTime.UtcNow.AddDays(-10),
                periodEnd: DateTime.UtcNow.AddDays(+4));

            var now = DateTime.UtcNow;

            subscription.Activate(now);
            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            var reloaded = await _subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);
            reloaded.Should().NotBeNull();

            reloaded!.Status.Should().Be(SubscriptionStatus.Active);
            reloaded.PeriodStart.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(2));
            reloaded.PeriodEnd.Should().BeCloseTo(now.AddMonths(1), precision: TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task Activate_Should_Succeed_When_StatusIsPastDue()
        {
            var (org, subscription) = await CreateSubscriptionAsync(
            status: SubscriptionStatus.PastDue,
            periodStart: DateTime.UtcNow.AddMonths(-1),
            periodEnd: DateTime.UtcNow.AddDays(-1));

            var now = DateTime.UtcNow;

            subscription.Activate(now);
            _subscriptions.Update(subscription);
            await _uow.CommitAsync();
                        
            var reloaded = await _subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);
            reloaded!.Status.Should().Be(SubscriptionStatus.Active);
            reloaded.PeriodStart.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(2));
            reloaded.PeriodEnd.Should().BeCloseTo(now.AddMonths(1), precision: TimeSpan.FromSeconds(2));
        }

        //Creating helper method to make a persisted org and sub
        private async Task<(Organization Org, Subscription Subscription)> CreateSubscriptionAsync(SubscriptionStatus status, DateTime periodStart, DateTime periodEnd)
        {
            var owner = new User($"{Guid.NewGuid()}@activationtest.com", "TestName");
            await _users.AddAsync(owner, CancellationToken.None);

            var org = new Organization($"Org-{Guid.NewGuid()}");
            org.AddUser(owner, UserRole.Owner);
            await _organizations.AddAsync(org, CancellationToken.None);

            var subscription = new Subscription(
                org,
                PlanType.Pro,
                status,
                periodStart,
                periodEnd);

            await _subscriptions.AddAsync(subscription, CancellationToken.None);
            await _uow.CommitAsync();

            return (org, subscription);
        }
    }
}
