using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;

namespace Recuria.Application.Requests
{
    public class UpgradeSubscriptionRequest
    {
        public PlanType NewPlan { get; init; }
    }
}
