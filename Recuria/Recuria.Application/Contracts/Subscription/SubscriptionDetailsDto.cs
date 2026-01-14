using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Subscription
{
    public sealed record SubscriptionDetailsDto(
        SubscriptionDto Subscription,
        SubscriptionActionAvailabilityDto Actions
    );
}
