using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Subscription
{
    public sealed record SubscriptionDto(Guid Id,
        PlanType PlanCode,
        SubscriptionStatus Status,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        bool IsTrial,
        bool IsPastDue
    );
}
