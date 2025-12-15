using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public interface IBillingService
    {
        Invoice RunBillingCycle(
             Subscription subscription,
             DateTime now
         );

        void HandleOverdueSubscription(
            Subscription subscription,
            DateTime now
        );
    }
}
