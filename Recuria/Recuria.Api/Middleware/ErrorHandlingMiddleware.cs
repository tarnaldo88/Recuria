using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Recuria.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                var details = CreateProblemDetails(
                    context,
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    "validation_error");

                details.Extensions["errors"] = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                await Write(context, details);
            }
            catch (InvalidOperationException ex)
            {
                var details = CreateProblemDetails(
                    context,
                    StatusCodes.Status400BadRequest,
                    ex.Message,
                    "business_rule_violation");
                await Write(context, details);
            }
            catch (Exception)
            {
                var details = CreateProblemDetails(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred.",
                    "server_error");
                await Write(context, details);
            }
        }

        private static ProblemDetails CreateProblemDetails(
            HttpContext ctx,
            int status,
            string title,
            string errorCode)
        {
            var details = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://httpstatuses.com/{status}",
                Instance = ctx.Request.Path
            };

            details.Extensions["traceId"] = ctx.TraceIdentifier;
            details.Extensions["errorCode"] = errorCode;
            return details;
        }

        private static Task Write(HttpContext ctx, ProblemDetails payload)
        {
            ctx.Response.StatusCode = payload.Status ?? StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/problem+json";
            return ctx.Response.WriteAsJsonAsync(payload);
        }
    }
}
