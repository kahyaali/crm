using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public class DataFilterService : IDataFilterService
    {
        // personeller kendilerine bağlı personel ve müşterileri görebilmesi için 
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;

        public DataFilterService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<int> GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        public async Task<Personel?> GetCurrentPersonel()
        {
            var userId = await GetCurrentUserId();
            return await _context.Personels
                 .Include(p => p.Department)
                 .Include(p => p.Position)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<bool> IsAdmin()
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim == "SystemAdmin" || roleClaim == "Admin";
        }

        public async Task<IQueryable<Personel>> FilterPersonelsByRole(IQueryable<Personel> query)
        {
            if (await IsAdmin())
                return query;

            var currentPersonel = await GetCurrentPersonel();
            if (currentPersonel == null)
                return query.Where(p => false);

            // Manager: Kendisi + kendine bağlı personeller
            var subordinateIds = await GetAllSubordinateIds(currentPersonel.Id);
            subordinateIds.Add(currentPersonel.Id);

            return query.Where(p => subordinateIds.Contains(p.Id));
        }

        public async Task<IQueryable<Customer>> FilterCustomersByRole(IQueryable<Customer> query)
        {
            if (await IsAdmin())
                return query;

            var currentPersonel = await GetCurrentPersonel();
            if (currentPersonel == null)
                return query.Where(c => false);

            // Personel: Sadece kendisine atanmış müşteriler
            return query.Where(c => c.AssignedToPersonelId == currentPersonel.Id);
        }

        public async Task<IQueryable<Lead>> FilterLeadsByRole(IQueryable<Lead> query)
        {
            if (await IsAdmin())
                return query;

            var currentPersonel = await GetCurrentPersonel();
            if (currentPersonel == null)
                return query.Where(l => false);

            // Personel: Sadece kendisine atanmış lead'ler
            return query.Where(l => l.AssignedToPersonelId == currentPersonel.Id);
        }

        private async Task<List<int>> GetAllSubordinateIds(int managerId)
        {
            var ids = new List<int>();
            var directSubordinates = await _context.Personels
                .Where(p => p.ManagerId == managerId && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

            ids.AddRange(directSubordinates);

            foreach (var subId in directSubordinates)
            {
                ids.AddRange(await GetAllSubordinateIds(subId));
            }

            return ids;
        }
    }
}


//using Crm.Domain.Entities;
//using Crm.Infrastructure.Data;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;

//namespace Crm.Infrastructure.Services
//{
//    public class DataFilterService : IDataFilterService
//    {
//        private readonly IHttpContextAccessor _httpContextAccessor;
//        private readonly AppDbContext _context;

//        public DataFilterService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
//        {
//            _httpContextAccessor = httpContextAccessor;
//            _context = context;
//        }

//        public async Task<int> GetCurrentUserId()
//        {
//            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
//        }

//        public async Task<Personel?> GetCurrentPersonel()
//        {
//            var userId = await GetCurrentUserId();
//            return await _context.Personels
//                 .Include(p => p.Department)
//                 .Include(p => p.Position)
//                .FirstOrDefaultAsync(p => p.UserId == userId);
//        }

//        // 🔴 YETKİ KONTROLÜ (sabit rol adı yok)
//        public async Task<bool> HasPermission(string permissionName)
//        {
//            var userId = await GetCurrentUserId();
//            if (userId == 0) return false;

//            return await _context.Users
//                .Where(u => u.Id == userId)
//                .SelectMany(u => u.Role.RolePermissions)
//                .AnyAsync(rp => rp.Permission.Name == permissionName);
//        }

//        // 🔴 TÜM PERSONELERİ GÖRME YETKİSİ
//        public async Task<bool> CanViewAllPersonels()
//        {
//            return await HasPermission("personel.view.all");
//        }

//        // 🔴 TÜM MÜŞTERİLERİ GÖRME YETKİSİ
//        public async Task<bool> CanViewAllCustomers()
//        {
//            return await HasPermission("customer.view.all");
//        }

//        // 🔴 TÜM LEAD'LERİ GÖRME YETKİSİ
//        public async Task<bool> CanViewAllLeads()
//        {
//            return await HasPermission("lead.view.all");
//        }

//        public async Task<IQueryable<Personel>> FilterPersonelsByRole(IQueryable<Personel> query)
//        {
//            // Tüm personelleri görme yetkisi varsa
//            if (await CanViewAllPersonels())
//                return query;

//            var currentPersonel = await GetCurrentPersonel();
//            if (currentPersonel == null)
//                return query.Where(p => false);

//            // Sadece kendine bağlı personeller
//            var subordinateIds = await GetAllSubordinateIds(currentPersonel.Id);
//            subordinateIds.Add(currentPersonel.Id);

//            return query.Where(p => subordinateIds.Contains(p.Id));
//        }

//        public async Task<IQueryable<Customer>> FilterCustomersByRole(IQueryable<Customer> query)
//        {
//            // Tüm müşterileri görme yetkisi varsa
//            if (await CanViewAllCustomers())
//                return query;

//            var currentPersonel = await GetCurrentPersonel();
//            if (currentPersonel == null)
//                return query.Where(c => false);

//            // Sadece kendine atanmış müşteriler
//            return query.Where(c => c.AssignedToPersonelId == currentPersonel.Id);
//        }

//        public async Task<IQueryable<Lead>> FilterLeadsByRole(IQueryable<Lead> query)
//        {
//            // Tüm lead'leri görme yetkisi varsa
//            if (await CanViewAllLeads())
//                return query;

//            var currentPersonel = await GetCurrentPersonel();
//            if (currentPersonel == null)
//                return query.Where(l => false);

//            // Sadece kendine atanmış lead'ler
//            return query.Where(l => l.AssignedToPersonelId == currentPersonel.Id);
//        }

//        private async Task<List<int>> GetAllSubordinateIds(int managerId)
//        {
//            var ids = new List<int>();
//            var directSubordinates = await _context.Personels
//                .Where(p => p.ManagerId == managerId && !p.IsDeleted)
//                .Select(p => p.Id)
//                .ToListAsync();

//            ids.AddRange(directSubordinates);

//            foreach (var subId in directSubordinates)
//            {
//                ids.AddRange(await GetAllSubordinateIds(subId));
//            }

//            return ids;
//        }
//    }
//}
