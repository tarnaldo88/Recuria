using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class SubscriptionLifecycleOrchestrator : ISubscriptionLifecycleOrchestrator
    {
        private readonly IBillingService _billingService;

        public SubscriptionLifecycleOrchestrator(IBillingService billingService)
        {
            _billingService = billingService;
        }

        public void Process(Subscription subscription, DateTime now)
        {
            switch (subscription.Status)
            { 
                case SubscriptionStatus.Trial:
                    HandleTrial(subscription, now);
                    break;

                case SubscriptionStatus.Active:
                    HandleActive(subscription, now);
                    break;

                case SubscriptionStatus.PastDue: 
                    HandlePastDue(subscription, now);
                    break;

                case SubscriptionStatus.Canceled:
                case SubscriptionStatus.Expired:
                    break;

                default:
                    throw new InvalidOperationException($"Unhandled subscription status: {subscription.Status}");
            }

        }

        private void HandleTrial(Subscription subscription, DateTime now)
        {
            if( now >= subscription.PeriodEnd)
            {
                subscription.Expire(now);
            }
        }

        private void HandleActive(Subscription subscription, DateTime now)
        {
            if (now < subscription.PeriodEnd)
                return;

            try
            {
                _billingService.RunBillingCycle(subscription,now);
                subscription.AdvancePeriod(now);
            }
            catch (Exception ex)
            {
                subscription.MarkPastDue();
            }
        }

        private void HandlePastDue(Subscription subscription, DateTime now)
        {

        }
    }
}
