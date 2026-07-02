using Crm.API.Attributes;
using Crm.Application.DTOs.Role;
using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [HasPermission("role.manage")]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogService _logService;

        public RolesController(AppDbContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [HttpGet]
        [HasPermission("role.view")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new RoleResponseDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Role",
                    AdditionalInfo = $"Roller listelendi. Toplam: {roles.Count}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });
                return Ok(roles);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/roles",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpGet("{id}/permissions")]
        public async Task<IActionResult> GetRolePermissions(int id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                    return NotFound(new { message = "Rol bulunamadı" });

                var permissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == id)
                    .Select(rp => new RolePermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Module = rp.Permission.Module,
                        Action = rp.Permission.Action
                    })
                    .ToListAsync();

                var roleName = role.Name;
                var permissionsCount = permissions.Count;

                // ACTION LOG - role değişkeni artık güvenli
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Role",
                    EntityId = id,
                    AdditionalInfo = $"Rolün yetkileri görüntülendi: {role.Name}, Yetki sayısı: {permissions.Count}",
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
                    RequestPath = $"/api/roles/{id}/permissions",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost]
        [HasPermission("role.create")]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Rol adı zorunludur" });

                if (await _context.Roles.AnyAsync(r => r.Name == request.Name))
                    return BadRequest(new { message = "Bu rol adı zaten mevcut" });

                var role = new Role
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Roles.AddAsync(role);
                await _context.SaveChangesAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Role",
                    EntityId = role.Id,
                    AdditionalInfo = $"Yeni rol oluşturuldu: {role.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new RoleResponseDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    CreatedAt = role.CreatedAt
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/roles",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [HasPermission("role.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                    return NotFound(new { message = "Rol bulunamadı" });

                if (role.Name == "SystemAdmin")
                    return BadRequest(new { message = "SystemAdmin rolü değiştirilemez" });

                var oldName = role.Name;

                if (await _context.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
                    return BadRequest(new { message = "Bu rol adı zaten mevcut" });

                role.Name = request.Name.Trim();
                role.Description = request.Description?.Trim();
                role.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Role",
                    EntityId = role.Id,
                    AdditionalInfo = $"Rol güncellendi: {oldName} -> {role.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new RoleResponseDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    CreatedAt = role.CreatedAt
                });
            }

            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/roles/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost("{id}/permissions")]
        [HasPermission("role.assignpermission")]
        public async Task<IActionResult> AssignPermissions(int id, [FromBody] AssignPermissionsRequest request)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                    return NotFound(new { message = "Rol bulunamadı" });

                if (role.Name == "SystemAdmin")
                    return BadRequest(new { message = "SystemAdmin rolünün yetkileri değiştirilemez" });

                // Mevcut yetkileri temizle
                var existingPermissions = await _context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
                _context.RolePermissions.RemoveRange(existingPermissions);

                // Yeni yetkileri ekle
                foreach (var permissionId in request.PermissionIds)
                {
                    await _context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ASSIGN_PERMISSIONS",
                    EntityType = "Role",
                    EntityId = id,
                    AdditionalInfo = $"Rolün yetkileri güncellendi: {role.Name}, Yetki sayısı: {request.PermissionIds.Count}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Yetkiler güncellendi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/roles/{id}/permissions",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }

        }

        [HttpDelete("{id}")]
        [HasPermission("role.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                    return NotFound(new { message = "Rol bulunamadı" });

                if (role.Name == "SystemAdmin")
                    return BadRequest(new { message = "SystemAdmin rolü silinemez" });

                if (await _context.Users.AnyAsync(u => u.RoleId == id))
                    return BadRequest(new { message = "Bu role ait kullanıcılar var. Önce kullanıcıların rolünü değiştirmelisiniz." });

                var roleName = role.Name;

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Role",
                    EntityId = id,
                    AdditionalInfo = $"Rol silindi: {roleName}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });


                return Ok(new { message = "Rol silindi" });
            }

            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/roles/{id}",
                    RequestMethod = "DELETE",
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

