
using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public TokenService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public string GenerateAccessToken(User user)
        {
            // 🔥 List<Claim> kullan - array değil!
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
            };

            // 🔥 PERSONEL ID EKLE - List'e Add yapılabilir
            try
            {
                var personel = _context.Personels.FirstOrDefault(p => p.UserId == user.Id);
                if (personel != null)
                {
                    claims.Add(new Claim("PersonelId", personel.Id.ToString()));
                }
                else
                {
                    // Personel kaydı yoksa, UserId'yi PersonelId olarak kullan
                    claims.Add(new Claim("PersonelId", user.Id.ToString()));
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda UserId'yi kullan
                Console.WriteLine($"PersonelId alınamadı: {ex.Message}");
                claims.Add(new Claim("PersonelId", user.Id.ToString()));
            }

            var jwtSecret = _configuration["JWT:Secret"] ?? throw new Exception("JWT Secret not configured");
            var jwtIssuer = _configuration["JWT:ValidIssuer"] ?? throw new Exception("JWT Issuer not configured");
            var jwtAudience = _configuration["JWT:ValidAudience"] ?? throw new Exception("JWT Audience not configured");
            var tokenValidity = Convert.ToDouble(_configuration["JWT:TokenValidityInMinutes"] ?? "30");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,  // 🔥 List<Claim> direkt kullanılabilir
                expires: DateTime.UtcNow.AddMinutes(tokenValidity),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            return user.RefreshToken == refreshToken &&
                   user.RefreshTokenExpiryTime > DateTime.UtcNow;
        }

        public async Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiryTime)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = expiryTime;
                await _context.SaveChangesAsync();
            }
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSecret = _configuration["JWT:Secret"] ?? throw new Exception("JWT Secret not configured");
            var jwtIssuer = _configuration["JWT:ValidIssuer"] ?? throw new Exception("JWT Issuer not configured");
            var jwtAudience = _configuration["JWT:ValidAudience"] ?? throw new Exception("JWT Audience not configured");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}