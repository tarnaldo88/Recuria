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
            factory = factory;

            _scope = factory.Services.CreateScope();
            var sp = _scope.ServiceProvider;

            _subscriptions = sp.GetRequiredService<ISubscriptionRepository>();
            _queries = sp.GetRequiredService<ISubscriptionQueries>();
            _organizations = sp.GetRequiredService<IOrganizationRepository>();
            _users = sp.GetRequiredService<IUserRepository>();
            _uow = sp.GetRequiredService<IUnitOfWork>();
            _processedEvents = sp.GetRequiredService<IProcessedEventStore>();
        }

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

            var reloaded = await _subscriptions.GetByIdAsync(subscription.Id, CancellationToken.None);

            reloaded!.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        public async Task Expire_Should_DispatchDomainEvent_And_MarkProcessedEventStore_When_PeriodEnded()
        {
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: DateTime.UtcNow.AddDays(-30),
                periodEnd: DateTime.UtcNow.AddDays(-1));

            var now = DateTime.UtcNow;
            subscription.Expire(now);

            // Capture the domain event BEFORE commit (commit clears DomainEvents)
            var expiredEvt = subscription.DomainEvents.OfType<SubscriptionExpiredDomainEvent>().Single();

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            // Assert: handler ran and marked processed
            var handlerName = nameof(SubscriptionExpiredHandler);

            var exists = await _processedEvents.ExistsAsync(
                expiredEvt.EventId,
                handlerName,
                CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Expire_Should_DispatchDomainEvent_And_MarkProcessedEventStore_When_Now_Equals_PeriodEnd()
        {
            var boundaryEnd = DateTime.UtcNow;
            var (org, subscription) = await CreateActiveSubscriptionAsync(periodStart: boundaryEnd.AddMonths(-1), periodEnd: boundaryEnd);

            subscription.Expire(boundaryEnd);

            var expiredEvt = subscription.DomainEvents
                .OfType<SubscriptionExpiredDomainEvent>()
                .Single();

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            // Assert
            var handlerName = nameof(SubscriptionExpiredHandler);

            var exists = await _processedEvents.ExistsAsync(
                expiredEvt.EventId,
                handlerName,
                CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Fact]
        public async Task Expire_Should_NotRaiseDomainEvent_When_PeriodNotEnded()
        {
            var (org, subscription) = await CreateActiveSubscriptionAsync(
                periodStart: DateTime.UtcNow.AddDays(-1),
                periodEnd: DateTime.UtcNow.AddDays(+10));

            var now = DateTime.UtcNow;
                     
            Action act = () => subscription.Expire(now);

            // Assert
            act.Should().Throw<InvalidOperationException>();

            subscription.DomainEvents
                .OfType<SubscriptionExpiredDomainEvent>()
                .Should().BeEmpty();
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
