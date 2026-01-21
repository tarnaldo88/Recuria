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
