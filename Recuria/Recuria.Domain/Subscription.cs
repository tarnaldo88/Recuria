using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public enum SubscriptionStatus
    {
        Trial,
        Active,
        PastDue,
        Canceled,
        Expired
    }


    public class Subscription
    {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; private set; }
        public Organization Organization { get; private set; }

        public PlanType Plan {  get; private set; }
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
            Status = SubscriptionStatus.Trial;
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

        public static Subscription CreateTrial(Organization organization, DateTime now)
        {
            return new Subscription(
                organization,
                PlanType.Free,
                SubscriptionStatus.Trial,
                now,
                now.AddDays(14)
            );
        }

        public void MarkPaid()
        {
            if(Status == SubscriptionStatus.Expired || Status == SubscriptionStatus.Canceled)
            {
                throw new InvalidOperationException("Subscriptions cannot be expired or cancelled to be mark paid.");
            }
            Status = SubscriptionStatus.Active;
        }

        public void Activate()
        {
            if (Status != SubscriptionStatus.Trial)
                throw new InvalidOperationException("Only trial subscriptions can be activated.");

            Status = SubscriptionStatus.Active;
        }

        public void Cancel()
        {
            if (Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Only active subscriptions can be canceled.");

            Status = SubscriptionStatus.Canceled;
        }

        public void Expire()
        {
            if (Status != SubscriptionStatus.Active || Status != SubscriptionStatus.Trial)
                throw new InvalidOperationException("Only active subscriptions can expire.");

            Status = SubscriptionStatus.Expired;
        }

        public void UpgradePlan(PlanType newPlan)
        {
            if (Status == SubscriptionStatus.Canceled || Status == SubscriptionStatus.Expired)
                throw new InvalidOperationException("Cannot upgrade a canceled or expired subscription.");

            Plan = newPlan;
        }

        public void MarkPastDue() {Status = SubscriptionStatus.PastDue; }

        public void AdvancePeriod(DateTime now)
        {
            PeriodStart = now;
            PeriodEnd = now.AddMonths(1);
        }
    }
}
