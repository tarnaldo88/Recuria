using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Abstractions
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(IIntegrationEvent @event, CancellationToken ct);
    }
}
