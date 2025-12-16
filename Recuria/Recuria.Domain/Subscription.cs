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
        public Organization Organization { get; private set; }

        public PlanType Plan {  get; set; }
        public SubscriptionStatus Status { get; private set; }
        public DateTime PeriodStart { get; private set; }
        public DateTime PeriodEnd { get; private set; }

        public Subscription(
            Organization organization,
            PlanType plan,
            DateTime periodStart,
            DateTime periodEnd)
        {
            Id = Guid.NewGuid();
            Organization = organization;
            OrganizationId = organization.Id;
            Plan = plan;
            Status = SubscriptionStatus.Active;
            PeriodStart = periodStart;
            PeriodEnd = periodEnd;
        }

        protected Subscription() { } //EF Core

        //public void Activate(DateTime now)
        //{
        //    Status = SubscriptionStatus.Active;
        //    PeriodStart = now;
        //    PeriodEnd = now.AddMonths(1);
        //}

        public void MarkPaid()
        {
            Status = SubscriptionStatus.Active;
        }

        public void Cancel() { Status = SubscriptionStatus.Canceled; }

        public void MarkPastDue() {Status = SubscriptionStatus.PastDue; }

        public void AdvancePeriod(DateTime now)
        {
            PeriodStart = now;
            PeriodEnd = now.AddMonths(1);
        }
    }
}
