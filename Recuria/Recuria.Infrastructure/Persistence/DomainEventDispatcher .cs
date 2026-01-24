using Microsoft.Extensions.DependencyInjection;
using Recuria.Application.Interface;
using Recuria.Domain.Abstractions;
using Recuria.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence
{
    public sealed class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DomainEventDispatcher(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            foreach (var evt in domainEvents)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
                var enumerableHandlerType = typeof(IEnumerable<>).MakeGenericType(handlerType);

                var handlers = (IEnumerable<object>)sp.GetRequiredService(enumerableHandlerType);

                foreach (var handler in handlers)
                {
                    var method = handlerType.GetMethod("HandleAsync")
                                 ?? throw new InvalidOperationException($"Handler {handlerType.Name} is missing HandleAsync.");

                    try
                    {
                        await ((Task)method.Invoke(handler, new object[] { evt, ct })!).ConfigureAwait(false);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }
                }
            }
        }
    }
}
