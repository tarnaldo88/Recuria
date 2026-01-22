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

        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct)
        {
            foreach (var evt in domainEvents)
            {
                var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
                var enumerableHandlerType = typeof(IEnumerable<>).MakeGenericType(handlerInterface);

                // GetServices returns empty if none are registered (no exception)
                var handlers = _serviceProvider.GetServices(enumerableHandlerType);

                foreach (var handler in handlers)
                {
                    var method = handlerInterface.GetMethod("HandleAsync");
                    if (method is null)
                        throw new InvalidOperationException($"Handler {handlerInterface.Name} is missing HandleAsync.");

                    var task = (Task)method.Invoke(handler, new object[] { evt, ct })!;
                    await task.ConfigureAwait(false);
                }
            }
        }
    }
}
