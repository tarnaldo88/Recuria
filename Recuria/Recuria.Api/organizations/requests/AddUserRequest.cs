using Recuria.Domain;

namespace Recuria.Api.organizations.requests
{
    public class AddUserRequest
    {
        public string Name { get; init; } = null!;
        public string Email { get; init; } = null!;
        public UserRole Role { get; init; }
    }
}
