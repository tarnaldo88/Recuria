using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Invoices;
using Recuria.Application.Organizations;
using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Domain.Events.Organization;
using Recuria.Domain.Events.Subscription;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Recuria.Tests.Unit.Domain
{
    public class OrganizationSubscriptionInvoiceTests
    {
        private readonly ServiceProvider _provider;

        public static RecuriaDbContext BuildInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<RecuriaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new RecuriaDbContext(options);
        }        

        public OrganizationSubscriptionInvoiceTests()
        {
            var services = new ServiceCollection();

            // In-memory DB
            services.AddDbContext<RecuriaDbContext>(options =>
                options.UseInMemoryDatabase("RecuriaTestDb"));

            // Repositories & UnitOfWork
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Services
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<IInvoiceService, InvoiceService>();

            // Domain events
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            // Handlers
            services.AddScoped<IDomainEventHandler<OrganizationCreatedDomainEvent>, CreateTrialOnOrganizationCreated>();
            services.AddScoped<IDomainEventHandler<SubscriptionActivatedDomainEvent>, CreateInvoiceOnSubscriptionActivated>();

            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task OrganizationCreated_ShouldCreateTrialSubscription_AndGenerateInvoice()
        {
            // Arrange
            var db = _provider.GetRequiredService<RecuriaDbContext>();
            var orgService = _provider.GetRequiredService<IOrganizationService>();
            var userRepo = _provider.GetRequiredService<IUserRepository>();

            // Create owner user
            var owner = new User("owner@test.com", "Owner");
            await userRepo.AddAsync(owner, CancellationToken.None);
            await _provider.GetRequiredService<IUnitOfWork>().CommitAsync();

            // Act: create organization
            var orgId = await orgService.CreateOrganizationAsync(new CreateOrganizationRequest
            {
                Name = "Test Org"
            }, CancellationToken.None);

            // Assert organization exists
            var org = await db.Organizations.Include(o => o.Subscriptions).FirstOrDefaultAsync(o => o.Id == orgId);
            org.Should().NotBeNull();

            // Subscription created automatically
            org.Subscriptions.Should().HaveCount(1);
            var subscription = org.Subscriptions.First();
            subscription.Status.Should().Be(SubscriptionStatus.Trial);

            // Activate subscription
            var subscriptionService = _provider.GetRequiredService<ISubscriptionService>();
            CancellationToken ct = default(CancellationToken);
            subscriptionService.ActivateAsync(subscription.Id, ct);

            // Commit UnitOfWork to trigger domain events
            await _provider.GetRequiredService<IUnitOfWork>().CommitAsync();

            // Invoice created
            var invoices = await db.Invoices.Where(i => i.SubscriptionId == subscription.Id).ToListAsync();
            invoices.Should().HaveCount(1);
        }
    }
}
