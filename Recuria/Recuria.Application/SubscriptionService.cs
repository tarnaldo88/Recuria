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
    }
}
