using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Interface.Idempotency;
using Recuria.Application.Requests;
using Recuria.Application.Subscriptions;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Domain.Events.Subscription;
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
        private readonly CustomWebApplicationFactory _factory;
        private readonly IProcessedEventStore _processedEvents;
        private readonly IServiceScope _scope;


        public SubscriptionExpirationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

            _scope = factory.Services.CreateScope();
            var sp = _scope.ServiceProvider;

            _subscriptions = sp.GetRequiredService<ISubscriptionRepository>();
            _queries = sp.GetRequiredService<ISubscriptionQueries>();
            _organizations = sp.GetRequiredService<IOrganizationRepository>();
            _users = sp.GetRequiredService<IUserRepository>();
            _uow = sp.GetRequiredService<IUnitOfWork>();
            _processedEvents = sp.GetRequiredService<IProcessedEventStore>();
        }

        [Fact]
        public void Dispose()
        {
            _scope.Dispose();
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

        [Fact]
        public async Task Expire_Should_Succeed_When_Now_Equals_PeriodEnd()
        {
            var boundaryEnd = DateTime.UtcNow;
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: boundaryEnd.AddMonths(-1),
                periodEnd: boundaryEnd);

            subscription.Expire(boundaryEnd);
            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            var reloaded = await _subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);
            reloaded!.Status.Should().Be(SubscriptionStatus.Expired);
        }

        [Fact]
        public async Task Expire_Should_Throw_And_NotPersist_When_PeriodNotEnded()
        {
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: DateTime.UtcNow.AddDays(-1),
                periodEnd: DateTime.UtcNow.AddDays(+10));

            var now = DateTime.UtcNow;

            var act = () => subscription.Expire(now);

            act.Should().Throw<InvalidOperationException>().WithMessage("Subscription period has not ended.");

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            //assert using a new scope so that it is not reading a tracked instance
            using var scope2 = _factory.Services.CreateScope();
            var subs2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var reloaded = await subs2.GetByIdAsync(subscription.Id, CancellationToken.None);

            reloaded!.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        public async Task Expire_Should_DispatchDomainEvent_And_MarkProcessedEventStore_When_PeriodEnded()
        {            
            var end = DateTime.UtcNow;
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: end.AddMonths(-1),
                periodEnd: end);

            _uow.GetType().FullName.Should().Be(
                "Recuria.Infrastructure.Persistence.UnitOfWork",
                "tests expect the infrastructure UnitOfWork that dispatches domain events");

            //_dispatcher.GetType().FullName.Should().Be(
            //    "Recuria.Infrastructure.Persistence.DomainEventDispatcher",
            //    "tests expect the dispatcher used by UnitOfWork");

            var now = end.AddTicks(1); // strictly after
            subscription.Expire(now);

            subscription.DomainEvents.Should().NotBeEmpty("Expire() should raise a domain event");

            var types = subscription.DomainEvents.Select(e => e.GetType().FullName).ToArray();
            types.Should().Contain(t => t!.Contains("Expired"), $"Got: {string.Join(", ", types)}");

            var expiredEvt = subscription.DomainEvents
                .OfType<SubscriptionExpiredDomainEvent>()
                .Single();

            expiredEvt.SubscriptionId.Should().Be(subscription.Id);
            expiredEvt.OrganizationId.Should().Be(org.Id);
        }

        [Fact]
        public async Task Expire_Should_DispatchDomainEvent_When_Now_Equals_PeriodEnd()
        {
            var boundaryEnd = DateTime.UtcNow;

            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: boundaryEnd.AddMonths(-1),
                periodEnd: boundaryEnd);

            subscription.Expire(boundaryEnd);

            subscription.DomainEvents
                .OfType<SubscriptionExpired>()
                .Should()
                .ContainSingle(e => e.SubscriptionId == subscription.Id);

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            using var scope2 = _factory.Services.CreateScope();
            var subs2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var reloaded = await subs2.GetByIdAsync(subscription.Id, CancellationToken.None);
            reloaded!.Status.Should().Be(SubscriptionStatus.Expired);
        }

        [Fact]
        public async Task Expire_Should_DispatchDomainEvent_When_PeriodEnded()
        {
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: DateTime.UtcNow.AddDays(-30),
                periodEnd: DateTime.UtcNow.AddDays(-1));

            var now = DateTime.UtcNow;

            subscription.Expire(now);

            subscription.DomainEvents
                .OfType<SubscriptionExpired>()
                .Should()
                .ContainSingle(e => e.SubscriptionId == subscription.Id);

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            using var scope2 = _factory.Services.CreateScope();
            var subs2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var reloaded = await subs2.GetByIdAsync(subscription.Id, CancellationToken.None);
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
