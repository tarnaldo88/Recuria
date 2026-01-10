using Recuria.Domain.Abstractions;
using Recuria.Domain.Events.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Billing
{
    public class SubscriptionExpiredHandler : IDomainEventHandler<SubscriptionExpired>
    {
        public Task HandleAsync(SubscriptionExpired domainEvent, CancellationToken ct)
        {
            // Send email
            // Emit integration event
            // Audit log
            return Task.CompletedTask;
        }
    }
}
