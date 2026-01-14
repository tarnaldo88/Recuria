using Recuria.Application.Contracts.Subscription;
using Recuria.Application.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<SubscriptionDetailsDto>> GetCurrent(CancellationToken ct)
        {
            var orgId = new Guid(); /* resolve from auth later */

            var subscription = await _queries.GetCurrentAsync(orgId, ct);

            if (subscription is null)
                return NotFound();

            return Ok(subscription);
        }
    }
}
