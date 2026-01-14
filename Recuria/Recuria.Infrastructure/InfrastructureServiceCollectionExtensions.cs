using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scrutor;
using Recuria.Infrastructure.Persistence.Queries;
using Recuria.Application.Interface;

namespace Recuria.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<RecuriaDbContext>(...);

            services.Scan(scan => scan
                .FromAssemblyOf<SubscriptionQueries>()
                .AddClasses(c => c.AssignableTo(typeof(ISubscriptionQueries)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            return services;
        }
    }
}
