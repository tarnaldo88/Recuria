using FluentValidation;
using Recuria.Application.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Subscription.Validators
{
    public class UpgradeSubscriptionRequestValidator : AbstractValidator<UpgradeSubscriptionRequest>
    {
        public UpgradeSubscriptionRequestValidator()
        {
            RuleFor(x => x.NewPlan)
                .IsInEnum();
        }
    }
}
