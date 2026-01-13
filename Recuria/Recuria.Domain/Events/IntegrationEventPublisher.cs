using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events
{
    public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
    {
        public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
