using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Validation
{
    public class ValidationBehavior
    {
        private readonly IEnumerable<IValidator> _validators;

        public ValidationBehavior(IEnumerable<IValidator> validators)
        {
            _validators = validators;
        }

        public async Task ValidateAsync<T>(T instance)
        {
            var context = new ValidationContext<T>(instance);

            var failures = _validators
                .OfType<IValidator<T>>()
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }
    }
}
