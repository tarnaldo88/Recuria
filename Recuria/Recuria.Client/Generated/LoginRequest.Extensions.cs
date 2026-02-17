namespace Recuria.Client
{
    public partial class LoginRequest
    {
        [Newtonsoft.Json.JsonProperty("organizationName", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string? OrganizationName { get; set; } = default!;
    }
}
