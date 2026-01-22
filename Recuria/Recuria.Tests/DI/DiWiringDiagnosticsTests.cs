using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Interface.Idempotency;
using Recuria.Domain.Abstractions;
using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace Recuria.Tests.DI
{
    public sealed class DiWiringDiagnosticsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public DiWiringDiagnosticsTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void DI_Should_Resolve_Expected_Concrete_Types()
        {
            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var uow = sp.GetRequiredService<IUnitOfWork>();
            var dispatcher = sp.GetRequiredService<IDomainEventDispatcher>();
            var store = sp.GetRequiredService<IProcessedEventStore>();

            // These three lines tell you immediately if you are NOT using the implementations you think you are.
            uow.GetType().FullName.Should().Be("Recuria.Infrastructure.Persistence.UnitOfWork");
            dispatcher.GetType().FullName.Should().Be("Recuria.Infrastructure.Persistence.DomainEventDispatcher");
            store.GetType().FullName.Should().Be("Recuria.Infrastructure.Idempotency.EfProcessedEventStore");
        }

        [Fact]
        public void DI_Should_Have_At_Least_One_SubscriptionActivated_Handler()
        {
            using var scope = _factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // If this is empty, your dispatcher can't find handlers.
            var handlers = sp.GetServices<IDomainEventHandler<Recuria.Domain.Events.Subscription.SubscriptionActivatedDomainEvent>>();
            handlers.Should().NotBeNull();
            handlers.Should().NotBeEmpty();
        }
    }
}
