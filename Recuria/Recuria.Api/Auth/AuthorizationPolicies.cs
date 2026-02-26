namespace Recuria.Api.Auth;

public static class AuthorizationPolicies
{
    public const string OwnerOnly = "OwnerOnly";
    public const string AdminOrOwner = "AdminOrOwner";
    public const string MemberOrAbove = "MemberOrAbove";

    public const string OrganizationsRead = "OrganizationsRead";
    public const string OrganizationsManageUsers = "OrganizationsManageUsers";

    public const string InvoicesRead = "InvoicesRead";
    public const string InvoicesWrite = "InvoicesWrite";

    public const string SubscriptionsRead = "SubscriptionsRead";
    public const string SubscriptionsManage = "SubscriptionsManage";

    public const string PaymentsCheckout = "PaymentsCheckout";
    public const string OpsManage = "OpsManage";
}
