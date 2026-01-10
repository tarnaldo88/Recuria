using Microsoft.Extensions.DependencyInjection;
using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _provider;

        public DomainEventDispatcher(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            var handlers = _provider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                await((dynamic)handler).HandleAsync((dynamic)domainEvent, cancellationToken);
            }
        }
    }
}
