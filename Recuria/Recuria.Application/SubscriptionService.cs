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
            // Ensure only one active subscription at a time
            var current = org.GetCurrentSubscription();
            if (current != null && current.Status == SubscriptionStatus.Active)
            {
                throw new InvalidOperationException("Organization already has an active subscription.");
            }

            var now = DateTime.UtcNow;
            var subscription = new Subscription(
                organization: org,
                plan: PlanType.Free,
                SubscriptionStatus.Trial,
                periodStart: now,
                periodEnd: now.AddDays(14)
            );

            org.AssignSubscription(subscription);
            return subscription;
        }

        public void UpgradePlan(Subscription subscription, PlanType newPlan)
        {
            subscription.UpgradePlan(newPlan);
        }

        public void CancelSubscription(Subscription subscription)
        {
            subscription.Cancel();
        }

        public Invoice GenerateInvoice(Subscription subscription, decimal amount)
        {
            if (subscription.Status != SubscriptionStatus.Active)
                throw new InvalidOperationException("Invoices can only be generated for active subscriptions.");

            return new Invoice(subscription.Id, amount);
        }

        public Subscription GenerateTrial(Organization organization)
        {
            if(organization.GetCurrentSubscription() != null)
            {
                throw new InvalidOperationException("Organization already has an active subscription.");
            }

            var now = DateTime.UtcNow;
            var subscription = Subscription.CreateTrial(organization, now); 

            organization.AssignSubscription(subscription);

            return subscription;
        }
    }
}
