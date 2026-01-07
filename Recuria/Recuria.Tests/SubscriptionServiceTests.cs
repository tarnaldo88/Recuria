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
            var trial = _service.CreateTrial(_org);
            _service.ActivateSubscription(trial);

            Action act = () => _service.CreateTrial(_org);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Organization already has an active subscription.");
        }

        [Fact]
        public void UpgradePlan_Should_ChangePlan_When_Active()
        {
            var sub = _service.CreateTrial(_org);
            _service.ActivateSubscription(sub);

            _service.UpgradePlan(sub, PlanType.Enterprise);

            sub.Plan.Should().Be(PlanType.Enterprise);
        }

        [Fact]
        public void UpgradePlan_Should_Throw_When_Canceled()
        {
            var sub = _service.CreateTrial(_org);
            _service.ActivateSubscription(sub);
            _service.CancelSubscription(sub);

            Action act = () => _service.UpgradePlan(sub, PlanType.Enterprise);

            act.Should().Throw<InvalidOperationException>().WithMessage("Cannot upgrade a canceled or expired subscription.");
        }

        [Fact]
        public void CancelSubscription_Should_SetStatusToCanceled()
        {
            var sub = _service.CreateTrial(_org);
            DateTime now = DateTime.UtcNow;
            sub.Activate(now);

            _service.CancelSubscription(sub);
            sub.Status.Should().Be(SubscriptionStatus.Canceled);
        }

        [Fact]
        public void GenerateInvoice_Should_Throw_ForInactiveSubscription()
        {
            var sub = _service.CreateTrial(_org);

            Action act = () => _service.GenerateInvoice(sub, 100);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot generate invoice for inactive subscription.");
        }

        [Fact]
        public void GenerateInvoice_Should_CreateInvoice_ForActiveSubscription()
        {
            var sub = _service.CreateTrial(_org);
            sub.Activate();

            var invoice = _service.GenerateInvoice(sub, 100);

            invoice.Should().NotBeNull();
            invoice.Amount.Should().Be(100);
            invoice.SubscriptionId.Should().Be(sub.Id);
        }
    }
}
