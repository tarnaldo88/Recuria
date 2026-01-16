using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Recuria.Application.Validation.Common
{
    public static class SharedRules
    {
        public static IRuleBuilderOptions<T, string> ValidName<T>(
            this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty()
                .MinimumLength(2)
                .MaximumLength(80);
        }

        public static IRuleBuilderOptions<T, string> ValidEmail<T>(
            this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(120);
        }
    }
}
