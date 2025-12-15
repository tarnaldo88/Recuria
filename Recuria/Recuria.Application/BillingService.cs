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

        Invoice RunBillingCycle(Subscription subscription, DateTime now)
        {
            var inv = new Invoice(subscription.Id, subscription.Plan_.);
        }

        void HandleOverdueSubscription(Subscription subscription, DateTime now)
        {

        }
    }
}
