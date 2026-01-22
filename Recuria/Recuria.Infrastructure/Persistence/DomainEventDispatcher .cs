using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events;
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
            foreach (var evt in domainEvent)
            {
                // IMPORTANT: use the EVENT instance type, not domainEvents.GetType()
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());

                // Resolve IEnumerable<IDomainEventHandler<TEvent>>
                var enumerableHandlerType = typeof(IEnumerable<>).MakeGenericType(handlerType);
                var handlers = (IEnumerable<object>)_serviceProvider.GetRequiredService(enumerableHandlerType);

                foreach (var handler in handlers)
                {
                    // Call HandleAsync(TEvent evt, CancellationToken ct)
                    var method = handlerType.GetMethod("HandleAsync");
                    if (method is null)
                        throw new InvalidOperationException($"Handler {handlerType.Name} is missing HandleAsync.");

                    var task = (Task)method.Invoke(handler, new object[] { evt, ct })!;
                    await task.ConfigureAwait(false);
                }
            }
        }
    }
}
