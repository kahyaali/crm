// Controllers/AuthController.cs

using Crm.Application.DTOs.Auth;
using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(AppDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        //========= Register ============
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Bu email zaten kayıtlı" });

            PasswordHelper.CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (defaultRole == null)
            {
                defaultRole = new Role { Name = "User", Description = "Standart Kullanıcı" };
                await _context.Roles.AddAsync(defaultRole);
                await _context.SaveChangesAsync();
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = Convert.ToBase64String(passwordHash),
                PasswordSalt = Convert.ToBase64String(passwordSalt),
                RoleId = defaultRole.Id,
                CreatedAt = DateTime.UtcNow,
                Role = defaultRole 
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            return Ok(new AuthResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role.Name,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        //======== Login ===============
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthLoginRequest request)
        {
            // Kullanıcıyı rolüyle birlikte çekiyoruz ki TokenService doğru claim üretebilsin
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return Unauthorized(new { message = "Email veya şifre hatalı" });

            if (!PasswordHelper.VerifyPasswordHash(request.Password,
                Convert.FromBase64String(user.PasswordHash),
                Convert.FromBase64String(user.PasswordSalt)))
                return Unauthorized(new { message = "Email veya şifre hatalı" });

        
            if (user.Email == "systemadmin@crm.com" && user.Role?.Name != "SystemAdmin")
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SystemAdmin");
                if (adminRole != null)
                {
                    user.RoleId = adminRole.Id;
                    user.Role = adminRole;
                    await _context.SaveChangesAsync();
                }
            }

            var roleName = user.Role?.Name ?? "User";

     
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            return Ok(new AuthResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = roleName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        //================== Refresh =================
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequest request)
        {
            try
            {
                // Önce eski access token'dan kullanıcı bilgilerini al
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                // Kullanıcıyı rolüyle birlikte bul
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Unauthorized(new { message = "Kullanıcı bulunamadı" });

                // Refresh token'ı doğrula
                if (!await _tokenService.ValidateRefreshTokenAsync(userId, request.RefreshToken))
                    return Unauthorized(new { message = "Refresh token geçersiz veya süresi dolmuş" });

                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));

                return Ok(new
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = $"Token yenileme hatası: {ex.Message}" });
            }
        }

        //================= LogOut ==============
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] int userId)
        {
            await _tokenService.SaveRefreshTokenAsync(userId, null, DateTime.UtcNow);
            return Ok(new { message = "Çıkış yapıldı" });
        }

        //================ FORGOT PASSWORD ===================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] AuthForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Kullanıcı yoksa da aynı mesaj (güvenlik)
                return Ok(new { message = "Eğer bu email kayıtlıysa, şifre sıfırlama linki gönderildi." });
            }

            var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
             .Replace('+', '-')
             .Replace('/', '_')
             .Replace("=", "");
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
            var result = await emailService.SendPasswordResetEmailAsync(user.Email, resetToken);

            // Mail gönderildi mi kontrol et!
            if (result)
            {
                return Ok(new { message = "Şifre sıfırlama linki e-posta adresinize gönderildi." });
            }
            else
            {
                // ❌ Mail gönderilemedi - hata mesajı döndür
                return BadRequest(new { message = "Mail ayarları yapılandırılmamış. Lütfen önce Mail Ayarları sayfasından SMTP ayarlarınızı yapın." });
            }
        }

        //=============== RESET PASSWORD ==================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AuthResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null ||
                user.PasswordResetToken != request.Token ||
                user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Geçersiz veya süresi dolmuş token." });
            }

            PasswordHelper.CreatePasswordHash(request.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = Convert.ToBase64String(passwordHash);
            user.PasswordSalt = Convert.ToBase64String(passwordSalt);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Şifreniz başarıyla değiştirildi." });
        }

        // =============== CHANGE PASSWORD (Admin veya Kendi Şifresi) ==================
   

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUser = await _context.Users.FindAsync(currentUserId);

                if (currentUser == null)
                    return Unauthorized(new { message = "Kullanıcı bulunamadı" });

                int targetUserId;

                // Admin başkasının şifresini değiştiriyor mu?
                if (request.UserId.HasValue && request.UserId.Value > 0)
                {
                    var currentUserRole = await _context.Roles.FindAsync(currentUser.RoleId);
                    if (currentUserRole?.Name != "SystemAdmin" && currentUserRole?.Name != "Admin")
                    {
                        return BadRequest(new { message = "Başka kullanıcının şifresini değiştirme yetkiniz yok" });
                    }
                    targetUserId = request.UserId.Value;
                }
                else
                {
                    // Kendi şifresini değiştiriyor - mevcut şifre kontrolü yap
                    targetUserId = currentUserId;

                    if (string.IsNullOrEmpty(request.CurrentPassword))
                        return BadRequest(new { message = "Mevcut şifrenizi girmelisiniz" });

                    if (!PasswordHelper.VerifyPasswordHash(request.CurrentPassword,
                        Convert.FromBase64String(currentUser.PasswordHash),
                        Convert.FromBase64String(currentUser.PasswordSalt)))
                        return BadRequest(new { message = "Mevcut şifreniz hatalı" });
                }

                var targetUser = await _context.Users.FindAsync(targetUserId);
                if (targetUser == null)
                    return NotFound(new { message = "Hedef kullanıcı bulunamadı" });

                // Şifre validasyonu
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır" });
                }

                // Yeni şifreyi hashle
                PasswordHelper.CreatePasswordHash(request.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);

                targetUser.PasswordHash = Convert.ToBase64String(passwordHash);
                targetUser.PasswordSalt = Convert.ToBase64String(passwordSalt);
                targetUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Şifre başarıyla değiştirildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Hata: {ex.Message}" });
            }
        }

  
        // Helper metod
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}