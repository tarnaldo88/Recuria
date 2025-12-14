using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public enum SubscriptionStatus
    {
        Trialing,
        Active,
        PastDue,
        Canceled
    }


    public class Subscription
    {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; private set; }
        public Organization Organization_ { get; private set; }

        public PlanType Plan_ {  get; private set; }
        public SubscriptionStatus Status { get; private set; }
        public DateTime PeriodStart { get; private set; }
        public DateTime PeriodEnd { get; private set; }

        public Subscription(Guid orgId, PlanType plan)
        {
            OrganizationId = orgId;
            Plan_ = plan;
            Status = SubscriptionStatus.Trialing;
            PeriodStart = DateTime.UtcNow;
            PeriodEnd = DateTime.UtcNow.AddDays(14);
        }

        public void Activate(DateTime now)
        {
            Status = SubscriptionStatus.Active;
            PeriodStart = now;
            PeriodEnd = now.AddMonths(1);
        }

        public void Cancel() { Status = SubscriptionStatus.Canceled; }

        public void MarkPastDue() {Status = SubscriptionStatus.PastDue; }
    }
}
