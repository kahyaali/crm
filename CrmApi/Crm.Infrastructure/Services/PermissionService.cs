using Microsoft.EntityFrameworkCore;
using Crm.Domain.Entities;
using Crm.Infrastructure.Data;

namespace Crm.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            // Önce kullanıcıyı bul
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;
            if (user.Role == null) return false;

            // SystemAdmin her şeyi yapabilir
            if (user.Role.Name == "SystemAdmin") return true;
            if (user.Role.Name == "Admin") return true;

            // Diğer roller için permission kontrolü
            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp => rp.RoleId == user.RoleId && rp.Permission.Name == permissionName);

            return hasPermission;
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Role == null) return new List<string>();

            return user.Role.RolePermissions
                .Select(rp => rp.Permission.Name)
                .ToList();
        }
    }
}