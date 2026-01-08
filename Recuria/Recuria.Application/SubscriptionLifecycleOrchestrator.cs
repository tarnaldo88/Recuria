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
            }

        }

        private void HandleTrial(Subscription subscription, DateTime now)
        {

        }

        private void HandleActive(Subscription subscription, DateTime now)
        {

        }

        private void HandlePastDue(Subscription subscription, DateTime now)
        {

        }
    }
}
