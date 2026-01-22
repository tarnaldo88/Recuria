using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Recuria.Api;
using Recuria.Application.Interface;
using Recuria.Domain.Events.Organization;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Tests.IntegrationTests.TestDoubles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Recuria.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing OrganizationCreated handlers
                services.RemoveAll<IDomainEventHandler<OrganizationCreatedDomainEvent>>();
                services.RemoveAll<IHostedService>();

                // Remove ONLY the outbox hosted service (do not remove all hosted services)
                services.RemoveAll<OutboxProcessorHostedService>();
                services.RemoveAll<OutboxProcessor>();

                // Add failing one
                services.AddScoped<
                    IDomainEventHandler<OrganizationCreatedDomainEvent>,
                    FailingOrganizationCreatedHandler>();
            });
        }
    }
}
