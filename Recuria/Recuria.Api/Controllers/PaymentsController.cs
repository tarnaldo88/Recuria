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

        public sealed class CreateCheckoutRequest
        {
            public string PriceId { get; init; } = string.Empty;
            public int Quantity { get; init; } = 1;
            public Guid OrganizationId {  get; init; } = Guid.Empty;
        }

    }
}
