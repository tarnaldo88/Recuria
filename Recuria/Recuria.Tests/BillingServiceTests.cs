using FluentAssertions;
//using Recuria.Services;
using Recuria.Application;
using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests
{
    internal class BillingServiceTests
    {
        private readonly SubscriptionService _service;
        private readonly Organization _org;

        public BillingServiceTests()
        {
            _service = new SubscriptionService();
            _org = new Organization("Test Org");
        }
        private Subscription CreateActiveSubscription(
            PlanType plan = PlanType.Pro,
            DateTime? periodStart = null,
            DateTime? periodEnd = null)
        {
            var start = periodStart ?? DateTime.UtcNow.AddMonths(-1);
            var end = periodEnd ?? DateTime.UtcNow;

            var sub = new Subscription(_org, plan, SubscriptionStatus.Active,start, end);
            _org.AssignSubscription(sub);

            return sub;
        }

        /*TESTS TO WRITE NEXT */
        //Billing creates invoice and advances period

        //Billing throws before period end

        //PastDue subscription cancels after grace period

        //PastDue subscription remains after grace not exceeded

        //Billing refuses inactive subscriptions

        //[Fact]
        //public void 

    }
}
