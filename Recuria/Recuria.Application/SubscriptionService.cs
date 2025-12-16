using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Recuria.Application
{
    public class SubscriptionService : ISubscriptionService
    {
        public Subscription CreateTrial(Organization org)
        {
            var subscription = new Subscription(org.Id, PlanType.Free);
            subscription.Activate(DateTime.UtcNow);
            org.AssignSubscription(subscription);
            return subscription;
        }

        public void UpgradePlan(Subscription subscription, PlanType newPlan)
        {
            if(subscription.Status == SubscriptionStatus.Canceled)
            {
                throw new InvalidOperationException("Cannot upgrade a canceled subscription");
            }

            subscription.Plan = newPlan;
        }

        public void CancelSubscription(Subscription subscription)
        {
            subscription.Cancel();
        }

        public Invoice GenerateInvoice(Subscription subscription, decimal amount)
        {
            return new Invoice(subscription.Id, amount);
        }
    }
}
