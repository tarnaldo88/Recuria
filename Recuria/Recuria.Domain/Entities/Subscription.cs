using Recuria.Domain.Abstractions;
using Recuria.Domain.Enums;
using Recuria.Domain.Events;
using Recuria.Domain.Events.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain.Entities
{    
    public class Subscription : Entity
    {
        public Guid Id { get; init; }
        public Guid OrganizationId { get; private set; }
        public Organization Organization { get; private set; } = null!;

        public PlanType Plan {  get; private set; }
        public SubscriptionStatus Status { get; private set; }

        public DateTime PeriodStart { get; private set; }
        public DateTime PeriodEnd { get; private set; }
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
        private readonly List<BillingAttempt> _billingAttempts = new List<BillingAttempt>();
        public IReadOnlyCollection<BillingAttempt> BillingAttempts => _billingAttempts.AsReadOnly();
        protected Subscription() { } //EF Core

        public Subscription(
            Organization organization,
            PlanType plan,
            SubscriptionStatus status,
            DateTime periodStart,
            DateTime periodEnd)
        {
            Id = Guid.NewGuid();
            Organization = organization;
            OrganizationId = organization.Id;
            Plan = plan;
            Status = status;
            PeriodStart = periodStart;
            PeriodEnd = periodEnd;
        }

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
            RaiseDomainEvent(new BillingSucceeded(Id));
        }

        public void Activate()
        {
            if (Status != SubscriptionStatus.Trial)
                throw new InvalidOperationException("Only trial subscriptions can be activated.");

            Status = SubscriptionStatus.Active;
            PeriodEnd.AddMonths(1);
            PeriodStart = DateTime.Now;
            RaiseDomainEvent(new SubscriptionActivatedDomainEvent(Id, OrganizationId));
        }

        public void Activate(DateTime now)
        {
            if (Status != SubscriptionStatus.Trial && Status != SubscriptionStatus.PastDue)
                throw new InvalidOperationException("Only trial or past-due subscriptions can be activated.");

            Status = SubscriptionStatus.Active;
            PeriodStart = now;
            PeriodEnd = now.AddMonths(1);
            RaiseDomainEvent(new SubscriptionActivatedDomainEvent(Id, OrganizationId));
        }

        public void Cancel()
        {
            if (Status == SubscriptionStatus.Canceled)
                return; // idempotent

            if (Status != SubscriptionStatus.Active && Status != SubscriptionStatus.PastDue && Status != SubscriptionStatus.Trial)
                throw new InvalidOperationException("Only trial, active, or past-due subscriptions can be canceled.");

            Status = SubscriptionStatus.Canceled;
            RaiseDomainEvent(new SubscriptionCanceled(Id));
        }

        public void Expire(DateTime now)
        {
            if (Status != SubscriptionStatus.Trial && Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Only trial or active subscriptions can expire.");

            if (now < PeriodEnd)
                throw new InvalidOperationException("Subscription period has not ended.");

            Status = SubscriptionStatus.Expired;
           
            RaiseDomainEvent(new SubscriptionExpired(Id));
            RaiseDomainEvent(new SubscriptionExpiredDomainEvent(Id, OrganizationId));
        }

        public void UpgradePlan(PlanType newPlan)
        {
            if (Status == SubscriptionStatus.Canceled || Status == SubscriptionStatus.Expired)
                throw new InvalidOperationException("Cannot upgrade a canceled or expired subscription.");

            Plan = newPlan;
        }

        public void MarkPastDue() 
        {
            if (Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Only active subscriptions can become past due.");

            Status = SubscriptionStatus.PastDue; 
            RaiseDomainEvent(new SubscriptionPastDue(Id));
        }

        public void AdvancePeriod(DateTime now)
        {
            if (Status != SubscriptionStatus.Active)
            {
                throw new InvalidOperationException("Only active subscriptions can advance billing period.");
            }

            if(now < PeriodEnd)
            {
                throw new InvalidOperationException("Cannot advance period before the current period ends.");
            }

            PeriodStart = PeriodEnd;
            PeriodEnd = PeriodEnd.AddMonths(1);
        }

        public bool IsExpired(DateTime now) => now > PeriodEnd;

        public void ExpireIfOverdue(DateTime now)
        {
            if (Status != SubscriptionStatus.Active)
                return;

            if (IsExpired(now))
            {
                Status = SubscriptionStatus.Expired;
                RaiseDomainEvent(new SubscriptionExpired(Id));
            }   
        }

        public void CancelForNonPayment()
        {
            if (Status != SubscriptionStatus.PastDue)
                throw new InvalidOperationException("Only past-due subscriptions can be canceled.");

            Status = SubscriptionStatus.Canceled;
            RaiseDomainEvent(new SubscriptionCanceled(Id));
        }

        public void RecordBillingAttempt(BillingAttempt billingAttempt)
        {
            if(billingAttempt.SubscriptionId != Id)
            {
                throw new InvalidOperationException("Billing attempt does not belong to this subscription.");
            }

            _billingAttempts.Add(billingAttempt);
        }

    }
}
