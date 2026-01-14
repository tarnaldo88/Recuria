using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Subscriptions
{
    public sealed record SubscriptionDto(Guid Id,
    string Plan,
    string Status,
    DateTime PeriodStart,
    DateTime PeriodEnd);
}
