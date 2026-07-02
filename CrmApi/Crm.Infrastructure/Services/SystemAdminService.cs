using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public class SystemAdminService:ISystemAdminService
    {
        private readonly AppDbContext _context;

        public SystemAdminService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> IsSystemAdmin(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user != null &&
           user.Role?.Name == "SystemAdmin" &&
           user.Email == "systemadmin@crm.com";
        }

        public async Task<bool> CanDeleteUser(int userId)
        {
            // Sistem Admin asla silinemez
            return !await IsSystemAdmin(userId);
        }

        public async Task<bool> CanUpdateUser(int userId, User updatedUser)
        {
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser == null) return false;

            // Sistem Admin ise, sadece şifre değişebilir
            if (await IsSystemAdmin(userId))
            {
                // Email, FirstName, LastName, Role değişemez
                updatedUser.Email = existingUser.Email;
                updatedUser.FirstName = existingUser.FirstName;
                updatedUser.LastName = existingUser.LastName;
                updatedUser.RoleId = existingUser.RoleId;
                return true;
            }

            return true;
        }
    }
}
