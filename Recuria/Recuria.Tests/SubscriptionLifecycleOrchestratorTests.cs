using FluentAssertions;
using Moq;
using Recuria.Application;
using Recuria.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Recuria.Tests
{
    public class SubscriptionLifecycleOrchestratorTests
    {
        private readonly Mock<IBillingService> _billingService;
        private readonly Mock<IBillingRetryPolicy> _retryPolicy;
        private readonly SubscriptionLifecycleOrchestrator _orchestrator;
        private readonly Organization _org;

        public SubscriptionLifecycleOrchestratorTests()
        {
            _billingService = new Mock<IBillingService>();
            _retryPolicy = new Mock<IBillingRetryPolicy>();

            _orchestrator = new SubscriptionLifecycleOrchestrator(
                _billingService.Object,
                _retryPolicy.Object
            );

            _org = new Organization("Test Org");
        }

        [Fact]
        public void Process_Should_ExpireTrial_WhenPeriodEnds()
        {
            var subscription = new Subscription(
                _org,
                PlanType.Free,
                SubscriptionStatus.Trial,
                DateTime.UtcNow.AddDays(-14),
                DateTime.UtcNow.AddDays(-1)
            );

            _orchestrator.Process(subscription, DateTime.UtcNow);

            subscription.Status.Should().Be(SubscriptionStatus.Expired);
        }

        [Fact]
        public void Process_Should_BillAndAdvancePeriod_WhenActiveAndPeriodEnded()
        {
            var start = DateTime.UtcNow.AddMonths(-1);
            var end = DateTime.UtcNow.AddDays(-1);

            var subscription = new Subscription(_org, PlanType.Pro, SubscriptionStatus.Active, start, end);

            _billingService.Setup(b => b.RunBillingCycle(subscription, It.IsAny<DateTime>()))
                .Returns(new Invoice(subscription.Id, 29m));

            var now = DateTime.UtcNow;

            _orchestrator.Process(subscription, now);

            subscription.Status.Should().Be(SubscriptionStatus.Active);
            subscription.PeriodStart.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            subscription.PeriodEnd.Should().BeAfter(now);
        }

        [Fact]
        public void Process_Should_MarkPastDue_WhenBillingFails()
        {
            var subscription = new Subscription(_org, PlanType.Pro, SubscriptionStatus.Active, DateTime.UtcNow.AddMonths(-1),
                DateTime.UtcNow.AddDays(-1));

            _billingService.Setup(b => b.RunBillingCycle(subscription, It.IsAny<DateTime>()))
                .Throws<InvalidOperationException>();

            _orchestrator.Process(subscription, DateTime.UtcNow);

            subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        }

        [Fact]
        public void Process_Should_CancelSubscription_WhenPastDueBeyondGracePeriod()
        {
            var subscription = new Subscription(
                   _org,
                   PlanType.Pro,
                   SubscriptionStatus.Active,
                   DateTime.UtcNow.AddMonths(-2),
                   DateTime.UtcNow.AddDays(-10)
            );

            subscription.MarkPastDue();

            _billingService
                .Setup(b => b.HandleOverdueSubscription(subscription, It.IsAny<DateTime>()))
                .Callback(() => subscription.Cancel());

            _orchestrator.Process(subscription, DateTime.UtcNow);

            subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        }

        [Fact]
        public void Process_Should_DoNothing_ForCanceledSubscription()
        {
            var subscription = new Subscription(
                _org,
                PlanType.Pro,
                SubscriptionStatus.Active,
                DateTime.UtcNow.AddMonths(-2),
                DateTime.UtcNow.AddMonths(-1)
            );

            subscription.Cancel();

            _orchestrator.Process(subscription, DateTime.UtcNow);

            subscription.Status.Should().Be(SubscriptionStatus.Canceled);
            _billingService.VerifyNoOtherCalls();
        }
    }
}
