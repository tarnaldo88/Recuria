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
    }
}
