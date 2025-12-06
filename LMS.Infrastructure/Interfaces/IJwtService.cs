using LMS.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infrastructure.Interfaces
{

    public interface IJwtTokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);

        RefreshToken GenerateRefreshToken(string userId, string jwtId);

        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
