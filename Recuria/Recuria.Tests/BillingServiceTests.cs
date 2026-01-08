using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Recuria.Domain;
//using Recuria.Services;
using Recuria.Application;

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
    }
}
