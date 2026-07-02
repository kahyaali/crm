using Crm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
        Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryTime);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
