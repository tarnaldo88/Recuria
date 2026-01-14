using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Subscription
{
    public sealed record SubscriptionActionAvailabilityDto(
        bool CanActivate,
        bool CanCancel,
        bool CanUpgrade
    );

}
