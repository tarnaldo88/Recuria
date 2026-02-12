using MudBlazor;
using System.Text.Json;

namespace Recuria.Blazor.Services.App
{
    public sealed class ApiCallRunner
    {
        private readonly ISnackbar _snackbar;

        public ApiCallRunner(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }

        public async Task<AppResult<T>> RunAsync<T>(
            Func<Task<T>> call,
            string? successMessage = null,
            string? errorPrefix = null,
            bool notifySuccess = false,
            bool notifyError = true)
        {
            try
            {
                var data = await call();
                if (notifySuccess && !string.IsNullOrWhiteSpace(successMessage))
                    _snackbar.Add(successMessage, Severity.Success);

                return AppResult<T>.Ok(data);
            }
            catch (Exception ex)
            {
                var message = BuildErrorMessage(ex, errorPrefix);
                if (notifyError)
                    _snackbar.Add(message, Severity.Error);
                return AppResult<T>.Fail(message);
            }
        }

        public async Task<AppResult> RunAsync(
            Func<Task> call,
            string? successMessage = null,
            string? errorPrefix = null,
            bool notifySuccess = false,
            bool notifyError = true)
        {
            try
            {
                await call();
                if (notifySuccess && !string.IsNullOrWhiteSpace(successMessage))
                    _snackbar.Add(successMessage, Severity.Success);

                return AppResult.Ok();
            }
            catch (Exception ex)
            {
                var message = BuildErrorMessage(ex, errorPrefix);
                if (notifyError)
                    _snackbar.Add(message, Severity.Error);
                return AppResult.Fail(message);
            }
        }

        public AppResult Ok(string? successMessage = null, bool notifySuccess = false)
        {
            if (notifySuccess && !string.IsNullOrWhiteSpace(successMessage))
                _snackbar.Add(successMessage, Severity.Success);
            return AppResult.Ok();
        }

        public AppResult<T> Ok<T>(T data, string? successMessage = null, bool notifySuccess = false)
        {
            if (notifySuccess && !string.IsNullOrWhiteSpace(successMessage))
                _snackbar.Add(successMessage, Severity.Success);
            return AppResult<T>.Ok(data);
        }

        public AppResult Fail(Exception ex, string? errorPrefix = null, bool notifyError = true)
        {
            var message = BuildErrorMessage(ex, errorPrefix);
            if (notifyError)
                _snackbar.Add(message, Severity.Error);
            return AppResult.Fail(message);
        }

        public AppResult<T> Fail<T>(Exception ex, string? errorPrefix = null, bool notifyError = true)
        {
            var message = BuildErrorMessage(ex, errorPrefix);
            if (notifyError)
                _snackbar.Add(message, Severity.Error);
            return AppResult<T>.Fail(message);
        }

        private static string BuildErrorMessage(Exception ex, string? errorPrefix)
        {
            var core = ex switch
            {
                Recuria.Client.ApiException apiEx => ExtractApiMessage(apiEx),
                _ => ex.Message
            };

            if (string.IsNullOrWhiteSpace(errorPrefix))
                return core;

            return $"{errorPrefix}: {core}";
        }

        private static string ExtractApiMessage(Recuria.Client.ApiException ex)
        {
            if(ex.StatusCode == 401) return "Unauthorized. Please sign in again.";            
            if (ex.StatusCode == 403) return "Forbidden. You do not have permission for this action. Contact an Admin/Owner if access is required.";
            if (ex.StatusCode == 404) return "Resource not found.";
            if (ex.StatusCode == 409) return "Conflict detected. Refresh and try again.";
            if (ex.StatusCode >= 500) return "Server error. Please try again.";

            if (!string.IsNullOrWhiteSpace(ex.Response))
            {
                try
                {
                    using var doc = JsonDocument.Parse(ex.Response);
                    if (doc.RootElement.TryGetProperty("title", out var title))
                        return title.GetString() ?? ex.Message;
                    if (doc.RootElement.TryGetProperty("detail", out var detail))
                        return detail.GetString() ?? ex.Message;
                }
                catch
                {
                }
            }

            return ex.Message;
        }
    }
}
