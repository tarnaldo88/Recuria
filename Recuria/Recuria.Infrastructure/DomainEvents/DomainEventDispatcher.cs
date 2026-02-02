using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.DomainEvents
{
    internal sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _provider;

        public DomainEventDispatcher(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
        {
            foreach (var @event in events)
            {
                var handlerType = typeof(IDomainEventHandler<>)
                    .MakeGenericType(@event.GetType());

                var handlers = _provider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    if (handler is null)
                        continue;

                    var method = handlerType.GetMethod("HandleAsync");
                    if (method is null)
                        throw new InvalidOperationException($"Handler {handlerType.Name} is missing HandleAsync.");

                    await ((Task)method.Invoke(handler, new object[] { @event, ct })!)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
