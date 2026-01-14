using Recuria.Application.Contracts.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Subscriptions
{
    public sealed class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionQueries _queries;

        public SubscriptionController(ISubscriptionQueries queries)
        {
            _queries = queries;
        }

        [HttpGet("current")]
        public async Task<ActionResult<SubscriptionDto>> GetCurrent()
        {
            var orgId = /* resolve from auth later */;
            var subscription = await _queries.GetCurrentAsync(orgId);

            if (subscription is null)
                return NotFound();

            return Ok(subscription);
        }
    }
}
