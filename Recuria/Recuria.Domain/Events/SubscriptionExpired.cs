using Recuria.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Events
{
    public sealed record SubscriptionExpired(Guid SubscriptionId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public DateTime OccuredOn => throw new NotImplementedException();
    }
}
