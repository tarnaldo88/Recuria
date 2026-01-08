using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Recuria.Application;
using Recuria.Domain;
using Xunit;

namespace Recuria.Tests
{
    public class SubscriptionLifecycleOrchestratorTests
    {
        private readonly Mock<IBillingService> _billingService;
        private readonly SubscriptionLifecycleOrchestrator _orchestrator;
        private readonly Organization _org;

        public SubscriptionLifecycleOrchestratorTests()
        {
            _billingService = new Mock<IBillingService>();
            _orchestrator = new SubscriptionLifecycleOrchestrator(_billingService.Object);
            _org = new Organization("Test Org");
        }
    }
}
