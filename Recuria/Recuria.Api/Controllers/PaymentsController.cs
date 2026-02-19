using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Recuria.Api.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Recuria.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly SessionService _sessions;
        private readonly StripeOptions _stripe;

        public PaymentsController(SessionService sessions, StripeOptions stripe)
        {
            _sessions = sessions;
            _stripe = stripe;
        }


    }
}
