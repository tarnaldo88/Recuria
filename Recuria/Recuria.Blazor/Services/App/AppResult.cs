namespace Recuria.Blazor.Services.App
{
    public sealed record AppResult(bool Success, string? Error = null)
    {
        public static AppResult Ok() => new(true);
        public static AppResult Fail(string error) => new(false, error);
    }

    public sealed record AppResult<T>(bool Success, T? Data = default, string? Error = null)
    {
        public static AppResult<T> Ok(T data) => new(true, data, null);
        public static AppResult<T> Fail(string error) => new(false, default, error);
    }
}
