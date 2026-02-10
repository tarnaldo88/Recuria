using Recuria.Domain;

namespace Recuria.Api.Auth
{
    public interface ITokenService
    {
        string CreateAccessToken(User user);
    }
}
