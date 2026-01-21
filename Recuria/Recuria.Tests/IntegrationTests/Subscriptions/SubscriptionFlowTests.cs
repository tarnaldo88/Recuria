using Recuria.Tests.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Tests.IntegrationTests.Subscriptions
{
    public class SubscriptionFlowTests : IntegrationTestBase
    {
        public SubscriptionFlowTests(CustomWebApplicationFactory factory) : base(factory) { }

        private async Task SeedUser(Guid userId)
        {
            await Client.PostAsJsonAsync("/api/users", new
            {
                Id = userId,
                Email = $"{userId}@test.com"
            });
        }
    }
}
