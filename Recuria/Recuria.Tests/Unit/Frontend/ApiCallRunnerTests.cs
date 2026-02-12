using FluentAssertions;
using Moq;
using MudBlazor;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class ApiCallRunnerTests
    {
        private readonly Mock<ISnackbar> _snackbar = new();

        [Fact]
        public async Task RunAsync_Generic_Should_Return_Data_On_Success()
        {
            var runner = new ApiCallRunner(_snackbar.Object);

            var result = await runner.RunAsync(() => Task.FromResult(42));

            result.Success.Should().BeTrue();
            result.Data.Should().Be(42);
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task RunAsync_Generic_Should_Map_401_To_Friendly_Message()
        {
            var runner = new ApiCallRunner(_snackbar.Object);
            var ex = CreateApiException(401, "{\"title\":\"Unauthorized\"}");

            var result = await runner.RunAsync<int>(
                () => Task.FromException<int>(ex),
                errorPrefix: "Request failed");

            result.Success.Should().BeFalse();
            result.Error.Should().Be("Request failed: Unauthorized. Please sign in again.");
        }

        [Fact]
        public async Task RunAsync_Generic_Should_Map_500_To_Generic_Server_Message()
        {
            var runner = new ApiCallRunner(_snackbar.Object);
            var ex = CreateApiException(500, "{\"title\":\"Service unavailable\"}");

            var result = await runner.RunAsync<int>(
                () => Task.FromException<int>(ex),
                errorPrefix: "Load failed");

            result.Success.Should().BeFalse();
            result.Error.Should().Be("Load failed: Server error. Please try again.");
        }

        [Theory]
        [InlineData(403, "Forbidden. You do not have permission for this action. Contact an Admin/Owner if access is required.")]
        [InlineData(404, "Resource not found.")]
        [InlineData(409, "Conflict detected. Refresh and try again.")]
        public async Task RunAsync_Generic_Should_Map_Common_Status_Codes(int statusCode, string expected)
        {
            var runner = new ApiCallRunner(_snackbar.Object);
            var ex = CreateApiException(statusCode, "{\"title\":\"Ignored by mapper\"}");

            var result = await runner.RunAsync<int>(
                () => Task.FromException<int>(ex),
                errorPrefix: "Request failed");

            result.Success.Should().BeFalse();
            result.Error.Should().Be($"Request failed: {expected}");
        }

        private static Recuria.Client.ApiException CreateApiException(int statusCode, string response)
        {
            return new Recuria.Client.ApiException(
                message: "API Error",
                statusCode: statusCode,
                response: response,
                headers: new Dictionary<string, IEnumerable<string>>(),
                innerException: null);
        }
    }
}
