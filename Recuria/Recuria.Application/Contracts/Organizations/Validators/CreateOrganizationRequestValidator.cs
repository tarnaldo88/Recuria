using FluentValidation;
using Recuria.Application.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Recuria.Application.Contracts.Organizations.Validators
{
    public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
    {
        public CreateOrganizationRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Organization name is required.")
                .MinimumLength(2)
                .MaximumLength(120);

            //RuleFor(x => x.OwnerEmail)
            //    .NotEmpty()
            //    .EmailAddress();

            //RuleFor(x => x.OwnerFirstName)
            //    .NotEmpty()
            //    .MaximumLength(60);

            //RuleFor(x => x.OwnerLastName)
            //    .NotEmpty()
            //    .MaximumLength(60);
        }
    }
}
