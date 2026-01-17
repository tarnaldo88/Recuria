using FluentAssertions;
//using Recuria.Services;
using Recuria.Application;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Validation;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Recuria.Tests
{
    public class SubscriptionServiceTests
    {
        private readonly SubscriptionService _service;
        private readonly Organization _org;
        private readonly ISubscriptionRepository _subscriptions;
        private readonly IOrganizationRepository _organizations;
        private readonly ISubscriptionQueries _queries;
        private readonly ValidationBehavior _validator;
        private readonly UnitOfWork _unitOfWork;

        public SubscriptionServiceTests()
        {
            _service = new SubscriptionService(_subscriptions, _organizations, _queries, _validator, _unitOfWork);
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
            _service.ActivateSubscription(sub);

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
            _service.ActivateSubscription(sub);

            var invoice = _service.GenerateInvoice(sub, 100);

            invoice.Should().NotBeNull();
            invoice.Amount.Should().Be(100);
            invoice.SubscriptionId.Should().Be(sub.Id);
        }

        [Fact]
        public void AdvancePeriod_Before_PeriodEnd_Should_Throw()
        {
            var sub = _service.CreateTrial(_org);
            _service.ActivateSubscription(sub);

            Action act = () => sub.AdvancePeriod(DateTime.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("Cannot advance period before the current period ends.");
        }

        [Fact]
        public void AdvancePeriod_When_NotActive_Should_Throw()
        {
            var sub = _service.CreateTrial(_org);
            _service.CancelSubscription(sub);

            Action act = () => sub.AdvancePeriod(DateTime.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("Only active subscriptions can advance billing period.");
        }

        [Fact]
        public void ExpireIfOverdue_Should_ExpireSubscription_WhenPastPeriodEnd()
        {
            // Arrange
            var org = new Organization("Test Org");

            var periodStart = DateTime.UtcNow.AddMonths(-2);
            var periodEnd = DateTime.UtcNow.AddMonths(-1);

            var subscription = new Subscription(
                organization: org,
                plan: PlanType.Pro,
                SubscriptionStatus.Active,
                periodStart: periodStart,
                periodEnd: periodEnd
            );

            subscription.MarkPaid(); // ensure Active
            org.AssignSubscription(subscription);

            var now = DateTime.UtcNow;

            // Act
            subscription.ExpireIfOverdue(now);

            // Assert
            subscription.Status.Should().Be(SubscriptionStatus.Expired);
        }

        [Fact]
        public void ExpireIfOverdue_Should_NotExpire_WhenWithinPeriod()
        {            
            var subscription = new Subscription(
                organization: _org,
                plan: PlanType.Pro,
                SubscriptionStatus.Active,
                periodStart: DateTime.UtcNow.AddDays(-5),
                periodEnd: DateTime.UtcNow.AddDays(5)
            );

            subscription.MarkPaid();
            _org.AssignSubscription(subscription);

            // Act
            subscription.ExpireIfOverdue(DateTime.UtcNow);

            // Assert
            subscription.Status.Should().Be(SubscriptionStatus.Active);
        }

        [Fact]
        public void ExpireIfOverdue_Should_NotChangeCanceledSubscription()
        {
            var subscription = new Subscription(
                _org,
                PlanType.Pro,
                SubscriptionStatus.Active,
                DateTime.UtcNow.AddMonths(-2),
                DateTime.UtcNow.AddMonths(-1)
            );

            subscription.Cancel();

            subscription.ExpireIfOverdue(DateTime.UtcNow);

            subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        }
    }
}
