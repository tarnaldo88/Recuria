using FluentValidation;

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
                await Write(context, 400, new
                {
                    type = "validation",
                    errors = ex.Errors.Select(e => e.ErrorMessage)
                });
            }
            catch (InvalidOperationException ex)
            {
                await Write(context, 400, new
                {
                    type = "business",
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                await Write(context, 500, new
                {
                    type = "server",
                    message = "An unexpected error occurred."
                });
            }
        }

        private static Task Write(
            HttpContext ctx,
            int status,
            object payload)
        {
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsJsonAsync(payload);
        }
    }

}
