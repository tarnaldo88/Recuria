using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public interface ISubscriptionService
    {
        Subscription CreateTrial(Organization org);
        
        void UpgradePlan(Subscription subscription, PlanType newPlan);
        
        void CancelSubscription(Subscription subscription);
        
        Invoice GenerateInvoice(Subscription subscription, decimal amount);
    }
}
