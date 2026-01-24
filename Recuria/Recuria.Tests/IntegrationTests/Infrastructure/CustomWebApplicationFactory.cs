using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Recuria.Api;
using Recuria.Application.Interface;
using Recuria.Application.Subscriptions;
using Recuria.Domain.Events.Organization;
using Recuria.Domain.Events.Subscription;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Recuria.Tests.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Recuria.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<RecuriaDbContext>>();
                services.AddDbContext<RecuriaDbContext>(options =>
                    options.UseInMemoryDatabase($"RecuriaTests-{Guid.NewGuid()}"));
                services.RemoveAll<IHostedService>();

                // Remove ONLY the outbox hosted service (do not remove all hosted services)
                services.RemoveAll<OutboxProcessorHostedService>();
                services.RemoveAll<OutboxProcessor>();

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<RecuriaDbContext>();
                db.Database.EnsureCreated();

                // Handlers are already registered via Scrutor scan in Program.cs
                // Explicitly ensure they're available for the dispatcher
                services.AddScoped<IDomainEventHandler<SubscriptionActivatedDomainEvent>, SubscriptionActivatedHandler>();
                services.AddScoped<IDomainEventHandler<SubscriptionExpiredDomainEvent>, SubscriptionExpiredHandler>();
            });
        }
    }
}
