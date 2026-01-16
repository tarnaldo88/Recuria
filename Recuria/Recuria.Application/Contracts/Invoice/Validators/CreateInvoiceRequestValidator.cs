using FluentValidation;
using Recuria.Application.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Invoice.Validators
{
    public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
    {
        public CreateInvoiceRequestValidator()
        {
            RuleFor(x => x.OrganizationId)
                .NotEmpty();

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .LessThan(100_000);

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(200);
        }
    } 
}
