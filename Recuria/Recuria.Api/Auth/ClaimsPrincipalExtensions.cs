using System.Security.Claims;

namespace Recuria.Api.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool TryGetOrganizationId(this ClaimsPrincipal principal, out Guid organizationId)
        {
            var raw = principal.FindFirstValue("org_id");
            return Guid.TryParse(raw, out organizationId);
        }

        public static bool IsInOrganization(this ClaimsPrincipal principal, Guid organizationId) =>
            principal.TryGetOrganizationId(out var claimOrgId) && claimOrgId == organizationId;
    }
}
