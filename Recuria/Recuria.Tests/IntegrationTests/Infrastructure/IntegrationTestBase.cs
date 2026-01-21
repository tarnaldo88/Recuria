using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Xunit;

namespace Recuria.Tests.IntegrationTests.Infrastructure
{
    public abstract class IntegrationTestBase
    : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient Client;

        protected IntegrationTestBase(CustomWebApplicationFactory factory)
        {
            Client = factory.CreateClient();
        }
    }
}
