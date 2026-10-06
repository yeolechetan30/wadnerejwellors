using System.Security.Claims;
using WadnereJwellors.Domain.Entities;

namespace WadnereJwellors.Business.Services
{
    public interface IJwtTokenService
    {
        (string AccessToken, DateTime Expiration) GenerateAccessToken(UserRegistration user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
