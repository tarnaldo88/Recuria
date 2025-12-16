using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Recuria.Domain;
//using Recuria.Services;
using Recuria.Application;

namespace Recuria.Tests
{
    public class SubscriptionServiceTests
    {
        private readonly SubscriptionService _service;
        private readonly Organization _org;

        public SubscriptionServiceTests()
        {
            _service = new SubscriptionService();
            _org = new Organization("Test Org");
        }

        [Fact]
        public void CreateTrial_Should_AddTrialSubscriptionToOrganization()
        {
            var subscription = _service.CreateTrial(_org);

            subscription.Should().NotBeNull();
            subscription.Plan.Should().Be(PlanType.Free);
            subscription.Status.Should().Be(SubscriptionStatus.Trial);
            _org.Subscriptions.Should().Contain(subscription);
        }

        [Fact]
        public void CreateTrial_Should_Throw_WhenActiveSubsriptionExits()
        {
            var activeSub = new Subscription(_org, PlanType.Pro, DateTime.UtcNow, DateTime.UtcNow);
            activeSub.Activate();
            _org.AssignSubscription(activeSub);

            Action act = () => _service.CreateTrial(_org);

            act.Should().Throw<InvalidOperationException>().WithMessage("Organization already has an active subscription.");
        }

        [Fact]
        public void UpgradePlan_Should_ChangePlan_WhenActive()
        {
            var sub = _service.CreateTrial(_org);
            sub.Activate();

            _service.UpgradePlan(sub, PlanType.Enterprise);

            sub.Plan.Should().Be(PlanType.Enterprise);
        }

        [Fact]
        public void UpgradePlan_SHould_Throw_WhenCanceled()
        {
            var sub = _service.CreateTrial(_org);
            sub.Activate();
            sub.Cancel();

            Action act = () => _service.UpgradePlan(sub, PlanType.Enterprise);

            act.Should().Throw<InvalidOperationException>().WithMessage("Cannot upgrade a canceled or expired subscription.");
        }
    }
}
