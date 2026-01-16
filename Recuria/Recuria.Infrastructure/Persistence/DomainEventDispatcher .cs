using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
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
        private readonly IServiceProvider _serviceProvider;

        public DomainEventDispatcher(IServiceProvider provider)
        {
            _serviceProvider = provider;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvent, CancellationToken ct)
        {
            var eventType = domainEvent.GetType();

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));

                if (method is null)
                    continue;

                var task = (Task)method.Invoke(handler, new object[] { domainEvent, ct })!;
                await task;
            }
        }
    }
}
