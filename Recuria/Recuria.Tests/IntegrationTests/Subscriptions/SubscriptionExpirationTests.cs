using FluentAssertion;
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
            var owner = new User("expire@test.com", "TestName");
            await _users.AddAsync(owner, CancellationToken.None);

            var org = new Organization("Expired Org");
            org.AddUser(owner, UserRole.Owner);

            await _organizations.AddAsync(org, CancellationToken.None);
        }
    }
}
