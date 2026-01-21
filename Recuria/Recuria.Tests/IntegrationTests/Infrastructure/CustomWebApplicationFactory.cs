using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Recuria.Application.Interface;
using Recuria.Domain.Events.Organization;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.TestDoubles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                //// Remove real DbContext
                //var descriptor = services.SingleOrDefault(
                //    d => d.ServiceType == typeof(DbContextOptions<RecuriaDbContext>));

                //if (descriptor != null)
                //    services.Remove(descriptor);

                //// Add in-memory DB
                //services.AddDbContext<RecuriaDbContext>(options =>
                //{
                //    options.UseInMemoryDatabase("Recuria_TestDb");
                //});

                // Remove existing OrganizationCreated handlers
                services.RemoveAll<IDomainEventHandler<OrganizationCreatedDomainEvent>>();

                // Add failing one
                services.AddScoped<
                    IDomainEventHandler<OrganizationCreatedDomainEvent>,
                    FailingOrganizationCreatedHandler>();
            });
        }
    }
}
