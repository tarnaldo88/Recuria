using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    internal interface ISubscriptionLifecycleOrchestrator
    {
        void Process(Subscription subscription, DateTime now);
    }
}
