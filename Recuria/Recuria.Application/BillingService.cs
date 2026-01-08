using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class BillingService : IBillingService
    {
        private static readonly int GracePeriodDays = 7;

        public Invoice RunBillingCycle(Subscription subscription, DateTime now)
        {
            if (subscription.Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Billing can only run on active subscriptions.");

            if (now < subscription.PeriodEnd)
                throw new InvalidOperationException("Billing period has not ended.");

            var amount = GetAmountForPlan(subscription.Plan);

            var invoice = new Invoice(subscription.Id, amount);

            subscription.AdvancePeriod(now);

            return invoice;
        }

        public void HandleOverdueSubscription(Subscription subscription, DateTime now)
        {
            if(subscription.Status != SubscriptionStatus.PastDue)
            {
                return;
            }

            var overDueDays = (now - subscription.PeriodEnd).Days;

            if(overDueDays > GracePeriodDays)
            {
               subscription.CancelForNonPayment();
            }

        }
        private decimal GetAmountForPlan(PlanType plan)
        {
            return plan switch
            {
                PlanType.Free => 0m,
                PlanType.Pro => 29m,
                PlanType.Enterprise => 99m,
                _ => throw new ArgumentOutOfRangeException(nameof(plan))
            };
        }
    }
}
