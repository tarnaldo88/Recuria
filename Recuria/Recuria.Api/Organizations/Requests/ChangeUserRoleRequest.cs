using Recuria.Domain;

namespace Recuria.Api.Organizations.Requests
{
    public sealed class ChangeUserRoleRequest
    {
        public UserRole Role { get; init; }
    }
}
