using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Infrastructure.Persistence;
using Recuria.Infrastructure.Persistence.Queries;
using Scrutor;
using Scrutor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<RecuriaDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            );

            services.Scan(scan => scan
                .FromAssemblyOf<SubscriptionQueries>()
                .AddClasses(c => c.AssignableTo(typeof(ISubscriptionQueries)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }
    }
}
