namespace Recuria.Client
{
    public partial interface IRecuriaApiClient
    {
        System.Threading.Tasks.Task<AuthResponse> RegisterAsync(RegisterRequest? body = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }

    public partial class RegisterRequest
    {
        [Newtonsoft.Json.JsonProperty("organizationName", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 2)]
        public string OrganizationName { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("ownerName", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        [System.ComponentModel.DataAnnotations.StringLength(120, MinimumLength = 2)]
        public string OwnerName { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("email", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        [System.ComponentModel.DataAnnotations.StringLength(256)]
        public string Email { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("password", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(256, MinimumLength = 8)]
        public string Password { get; set; } = default!;
    }
}
