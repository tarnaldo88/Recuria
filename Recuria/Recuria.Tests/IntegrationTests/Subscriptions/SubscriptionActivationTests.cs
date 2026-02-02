using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Interface.Idempotency;
using Recuria.Application.Requests;
using Recuria.Application.Subscriptions;
using Recuria.Domain;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Domain.Events.Subscription;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Repositories;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Recuria.Tests.IntegrationTests.Subscriptions
{
    public class SubscriptionActivationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly ISubscriptionRepository _subscriptions;
        private readonly ISubscriptionQueries _queries;
        private readonly IOrganizationRepository _organizations;
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;
        private readonly CustomWebApplicationFactory _factory;
        private readonly IProcessedEventStore _processedEvents;
        private readonly IServiceScope _scope;

        private readonly IDomainEventDispatcher _dispatcher;


        public SubscriptionActivationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

            _scope = factory.Services.CreateScope();

            _subscriptions = _scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            _queries = _scope.ServiceProvider.GetRequiredService<ISubscriptionQueries>();
            _organizations = _scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
            _users = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
            _uow = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _processedEvents = _scope.ServiceProvider.GetRequiredService<IProcessedEventStore>();
            _dispatcher = _scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        }

        [Fact]
        public void Dispose() => _scope.Dispose();

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

            // Re-query using a separate scope to avoid reading a tracked instance.
            using var scope2 = _factory.Services.CreateScope();
            var subs2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var reloaded = await subs2.GetByIdAsync(subscription.Id, CancellationToken.None);
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

            using var scope2 = _factory.Services.CreateScope();
            var subs2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var reloaded = await subs2.GetByIdAsync(subscription.Id, CancellationToken.None);
            reloaded.Should().NotBeNull();

            reloaded!.Status.Should().Be(SubscriptionStatus.Active);
            reloaded.PeriodStart.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(2));
            reloaded.PeriodEnd.Should().BeCloseTo(now.AddMonths(1), precision: TimeSpan.FromSeconds(2));
        }

        [Fact]
        public async Task Activate_Should_Throw_When_StatusIsAlreadyActive_And_NotPersist()
        {
            var (org, subscription) = await CreateSubscriptionAsync(
                status: SubscriptionStatus.Active,
                periodStart: DateTime.UtcNow.AddDays(-5),
                periodEnd: DateTime.UtcNow.AddDays(+25));

            var originalStart = subscription.PeriodStart;
            var originalEnd = subscription.PeriodEnd;

            var now = DateTime.UtcNow;
            Action act = () => subscription.Activate(now);

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Only trial or past-due subscriptions can be activated.");

            // Persist unchanged state (should remain unchanged).
            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            // Assert using a new scope so that it is not reading a tracked instance
            using var scope2 = _factory.Services.CreateScope();
            var subs2 = scope2.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            var reloaded = await subs2.GetByIdAsync(subscription.Id, CancellationToken.None);
            reloaded.Should().NotBeNull();

            reloaded!.Status.Should().Be(SubscriptionStatus.Active);
            reloaded.PeriodStart.Should().Be(originalStart);
            reloaded.PeriodEnd.Should().Be(originalEnd);
        }

        [Fact]
        public async Task Activate_Should_DispatchSubscriptionActivatedEvent_And_MarkProcessedEventStore()
        {
            var (org, subscription) = await CreateSubscriptionAsync(
               status: SubscriptionStatus.Trial,
               periodStart: DateTime.UtcNow.AddDays(-10),
               periodEnd: DateTime.UtcNow.AddDays(+4));

            var now = DateTime.UtcNow;

            // Act: raise event on aggregate
            subscription.Activate(now);

            var activatedEvt = subscription.DomainEvents
                .OfType<SubscriptionActivatedDomainEvent>()
                .Single();

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            // IMPORTANT: explicitly dispatch the event (do NOT rely on Commit to do it)
            using (var dispatchScope = _factory.Services.CreateScope())
            {
                var dispatcher = dispatchScope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
                await dispatcher.DispatchAsync(new IDomainEvent[] { activatedEvt }, CancellationToken.None);
            }

            // Assert: processed marker exists (use a fresh scope)
            using (var verifyScope = _factory.Services.CreateScope())
            {
                var processed = verifyScope.ServiceProvider.GetRequiredService<IProcessedEventStore>();
                var exists = await processed.ExistsAsync(
                    activatedEvt.EventId,
                    nameof(SubscriptionActivatedHandler),
                    CancellationToken.None);

                exists.Should().BeTrue("handler should mark the event as processed after dispatch");
            }
        }

        [Fact]
        public async Task Activate_Should_DispatchDomainEvent_And_MarkProcessedEventStore()
        {
            var (org, subscription) = await CreateSubscriptionAsync(
                  status: SubscriptionStatus.Trial,
                  periodStart: DateTime.UtcNow.AddDays(-10),
                  periodEnd: DateTime.UtcNow.AddDays(+4));

            var now = DateTime.UtcNow;

            subscription.Activate(now);

            var activatedEvt = subscription.DomainEvents
                .OfType<SubscriptionActivatedDomainEvent>()
                .Single();

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            // Act: call ONLY SubscriptionActivatedHandler (but allow duplicates in DI)
            using (var handlerScope = _factory.Services.CreateScope())
            {
                var handlers = handlerScope.ServiceProvider
                    .GetServices<IDomainEventHandler<SubscriptionActivatedDomainEvent>>()
                    .OfType<SubscriptionActivatedHandler>()
                    .ToList();

                handlers.Should().NotBeEmpty("SubscriptionActivatedHandler must be registered in DI.");
                // Pick one deterministically
                var handler = handlers[0];

                await handler.HandleAsync(activatedEvt, CancellationToken.None);
            }

            // Assert
            using (var verifyScope = _factory.Services.CreateScope())
            {
                var processed = verifyScope.ServiceProvider.GetRequiredService<IProcessedEventStore>();

                var exists = await processed.ExistsAsync(
                    activatedEvt.EventId,
                    nameof(SubscriptionActivatedHandler),
                    CancellationToken.None);

                exists.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Activate_Should_BeIdempotent_HandlerShouldNotDuplicateProcessedMarker()
        {
            var (org, subscription) = await CreateSubscriptionAsync(
        status: SubscriptionStatus.Trial,
        periodStart: DateTime.UtcNow.AddDays(-10),
        periodEnd: DateTime.UtcNow.AddDays(+4));

            var now = DateTime.UtcNow;

            subscription.Activate(now);

            var activatedEvt = subscription.DomainEvents
                .OfType<SubscriptionActivatedDomainEvent>()
                .Single();

            _subscriptions.Update(subscription);
            await _uow.CommitAsync();

            // Act: call ONLY SubscriptionActivatedHandler twice
            using (var handlerScope = _factory.Services.CreateScope())
            {
                var handlers = handlerScope.ServiceProvider
                    .GetServices<IDomainEventHandler<SubscriptionActivatedDomainEvent>>()
                    .OfType<SubscriptionActivatedHandler>()
                    .ToList();

                handlers.Should().NotBeEmpty("SubscriptionActivatedHandler must be registered in DI.");
                var handler = handlers[0];

                await handler.HandleAsync(activatedEvt, CancellationToken.None);
                await handler.HandleAsync(activatedEvt, CancellationToken.None);
            }

            // Assert: exactly one processed marker row for this handler+event
            using (var verifyScope = _factory.Services.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<RecuriaDbContext>();

                var count = await db.ProcessedEvents.CountAsync(
                    x => x.EventId == activatedEvt.EventId &&
                         x.Handler == nameof(SubscriptionActivatedHandler));

                count.Should().Be(1);
            }
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
