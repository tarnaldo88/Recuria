using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;

namespace Recuria.Application.Requests
{
    /// <summary>
    /// Request to upgrade a subscription plan.
    /// </summary>
    public class UpgradeSubscriptionRequest
    {
        /// <summary>
        /// New plan code.
        /// </summary>
        public PlanType NewPlan { get; init; }
    }
}
