using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts
{
    public sealed record SubscriptionDto(Guid Id,
        string PlanCode,
        string Status,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        bool IsTrial,
        bool IsPastDue
    );
}
