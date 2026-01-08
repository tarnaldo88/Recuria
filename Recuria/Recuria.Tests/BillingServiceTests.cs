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
    public class BillingServiceTests
    {
        private readonly BillingService _service;
        private readonly Organization _org;

        public BillingServiceTests()
        {
            _service = new BillingService();
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

        [Fact]
        public void RunBillingCycle_Should_CreateInvoice_And_AdvancePeriod()
        {
            var now = DateTime.UtcNow;
            var subscription = CreateActiveSubscription(periodStart: now.AddMonths(-1), periodEnd: now);

            var invoice = _service.RunBillingCycle(subscription, now);

            invoice.Should().NotBeNull();
            invoice.SubscriptionId.Should().Be(subscription.Id);
            invoice.Amount.Should().Be(29m);

            subscription.PeriodStart.Should().Be(now);
            subscription.PeriodEnd.Should().Be(now.AddMonths(1));
        }

        [Fact]
        public void RunBillingCycle_Should_Throw_When_PeriodNotEnded()
        {
            var now = DateTime.UtcNow;
            var subscription = CreateActiveSubscription(
                periodEnd: now.AddDays(5)
            );

            Action act = () => _service.RunBillingCycle(subscription, now);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Billing period has not ended.");
        }
    }
}
