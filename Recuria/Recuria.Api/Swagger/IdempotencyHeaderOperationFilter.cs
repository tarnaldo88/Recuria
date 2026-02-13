using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recuria.Api.Swagger
{
    public sealed class IdempotencyHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var method = context.MethodInfo;
            var isInvoiceCreate =
                string.Equals(method.Name, "Create", StringComparison.Ordinal) &&
                string.Equals(method.DeclaringType?.Name, "InvoiceController", StringComparison.Ordinal);

            if (!isInvoiceCreate)
                return;

            operation.Parameters ??= new List<OpenApiParameter>();

            if (operation.Parameters.Any(p =>
                string.Equals(p.Name, "Idempotency-Key", StringComparison.OrdinalIgnoreCase) &&
                p.In == ParameterLocation.Header))
            {
                return;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Unique key for safe request replay. Same key + same payload reuses existing invoice.",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    MinLength = 8,
                    MaxLength = 120
                }
            });
        }
    }
}
