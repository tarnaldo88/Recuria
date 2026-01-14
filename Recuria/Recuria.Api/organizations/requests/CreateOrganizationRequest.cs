namespace Recuria.Api.organizations.requests
{
    public class CreateOrganizationRequest
    {
        public string Name { get; init; } = null!;
        public string OwnerName { get; init; } = null!;
        public string OwnerEmail { get; init; } = null!;
    }
}
