using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Infrastructure.Persistence;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Recuria.Tests.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove real DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<RecuriaDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory DB
                services.AddDbContext<RecuriaDbContext>(options =>
                {
                    options.UseInMemoryDatabase("Recuria_TestDb");
                });
            });
        }
    }
}
