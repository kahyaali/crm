using Crm.API.Attributes;
using Crm.Application.DTOs.User;
using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Crm.Infrastructure.Helpers;
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
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly ILogService _logService;

        public UsersController(AppDbContext context, IPermissionService permissionService, ILogService logService)
        {
            _context = context;
            _permissionService = permissionService;
            _logService = logService;
        }

        [HttpGet]
        [HasPermission("user.view")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Role)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        Role = u.Role != null ? u.Role.Name : null,
                        u.CreatedAt
                    })
                    .ToListAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "User",
                    AdditionalInfo = $"Kullanıcı listesi görüntülendi. Toplam: {users.Count}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });
                return Ok(users);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/users",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPut("{id}/role")]
        [HasPermission("user.role.assign")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound();

                // SystemAdmin'in rolü değiştirilemez
                if (user.Role?.Name == "SystemAdmin")
                    return BadRequest(new { message = "SystemAdmin rolü değiştirilemez" });

                var newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
                if (newRole == null)
                    return BadRequest(new { message = "Geçersiz rol" });

                var oldRoleName = user.Role?.Name ?? "Yok";
                var userFullName = $"{user.FirstName} {user.LastName}";

                user.RoleId = newRole.Id;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ASSIGN_ROLE",
                    EntityType = "User",
                    EntityId = user.Id,
                    AdditionalInfo = $"Kullanıcının rolü değiştirildi: {userFullName}, {oldRoleName} -> {newRole.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Rol güncellendi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/users/{id}/role",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }

        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized();

                var userId = int.Parse(userIdClaim);
                var permissions = await _permissionService.GetUserPermissionsAsync(userId);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "User",
                    AdditionalInfo = $"Kullanıcı kendi yetkilerini görüntüledi. Yetki sayısı: {permissions.Count}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/users/permissions",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }
    }
}


