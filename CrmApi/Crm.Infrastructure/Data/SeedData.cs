

using Crm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Crm.Infrastructure.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            // ========== DEPARTMANLAR ==========
            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new Department { Name = "Muhasebe", Description = "Finans ve muhasebe işlemleri", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "Satış", Description = "Satış ve müşteri kazanımı", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "Destek", Description = "Müşteri destek hizmetleri", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "Yönetim", Description = "Şirket yönetimi", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "İK", Description = "İnsan kaynakları", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "IT", Description = "Bilgi teknolojileri", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "Pazarlama", Description = "Pazarlama ve reklam", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "Ar-Ge", Description = "Araştırma geliştirme", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Department { Name = "Lojistik", Description = "Lojistik ve tedarik", IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            // ========== POSITIONS ==========
            if (!await context.Positions.AnyAsync())
            {
                var positions = new List<Position>
                {
                    new Position { Name = "Müdür", Description = "Departman yöneticisi" },
                    new Position { Name = "Müdür Yardımcısı", Description = "Müdüre yardımcı olur" },
                    new Position { Name = "Uzman", Description = "Alanında uzman personel" },
                    new Position { Name = "Kıdemli Uzman", Description = "Deneyimli uzman" },
                    new Position { Name = "Uzman Yardımcısı", Description = "Uzman adayı" },
                    new Position { Name = "Stajyer", Description = "Stajyer personel" },
                    new Position { Name = "Asistan", Description = "Departman asistanı" },
                    new Position { Name = "Şef", Description = "Birim şefi" },
                    new Position { Name = "Operatör", Description = "Operasyon personeli" },
                    new Position { Name = "Teknisyen", Description = "Teknik personel" },
                    new Position { Name = "Danışman", Description = "Danışman personel" }
                };

                await context.Positions.AddRangeAsync(positions);
                await context.SaveChangesAsync();
            }

            // ========== PERMISSIONS ==========
            if (!await context.Permissions.AnyAsync())
            {
                var permissions = new List<Permission>
                {
                    new Permission { Name = "customer.view", Module = "Customers", Action = "View" },
                    new Permission { Name = "customer.create", Module = "Customers", Action = "Create" },
                    new Permission { Name = "customer.edit", Module = "Customers", Action = "Edit" },
                    new Permission { Name = "customer.delete", Module = "Customers", Action = "Delete" },
                    new Permission { Name = "product.view", Module = "Products", Action = "View" },
                    new Permission { Name = "product.create", Module = "Products", Action = "Create" },
                    new Permission { Name = "product.edit", Module = "Products", Action = "Edit" },
                    new Permission { Name = "product.delete", Module = "Products", Action = "Delete" },
                    new Permission { Name = "personel.view", Module = "Personels", Action = "GetAll" },
                    new Permission { Name = "personel.create", Module = "Personels", Action = "Create" },
                    new Permission { Name = "personel.edit", Module = "Personels", Action = "Edit" },
                    new Permission { Name = "personel.delete", Module = "Personels", Action = "Delete" },
                    new Permission { Name = "personel.createuser", Module = "Personels", Action = "CreateUser" },
                    new Permission { Name = "order.view", Module = "Orders", Action = "View" },
                    new Permission { Name = "order.create", Module = "Orders", Action = "Create" },
                    new Permission { Name = "order.edit", Module = "Orders", Action = "Edit" },
                    new Permission { Name = "order.delete", Module = "Orders", Action = "Delete" },
                    new Permission { Name = "ticket.view", Module = "Tickets", Action = "View" },
                    new Permission { Name = "ticket.create", Module = "Tickets", Action = "Create" },
                    new Permission { Name = "ticket.edit", Module = "Tickets", Action = "Edit" },
                    new Permission { Name = "ticket.delete", Module = "Tickets", Action = "Delete" },
                    new Permission { Name = "lead.view", Module = "Leads", Action = "View" },
                    new Permission { Name = "lead.create", Module = "Leads", Action = "Create" },
                    new Permission { Name = "lead.edit", Module = "Leads", Action = "Edit" },
                    new Permission { Name = "lead.delete", Module = "Leads", Action = "Delete" },
                    new Permission { Name = "report.view", Module = "Reports", Action = "View" },
                    new Permission { Name = "report.export", Module = "Reports", Action = "Export" },
                    new Permission { Name = "mail.settings.view", Module = "MailSettings", Action = "View" },
                    new Permission { Name = "mail.settings.edit", Module = "MailSettings", Action = "Edit" },
                    new Permission { Name = "user.view", Module = "Users", Action = "View" },
                    new Permission { Name = "user.create", Module = "Users", Action = "Create" },
                    new Permission { Name = "user.edit", Module = "Users", Action = "Edit" },
                    new Permission { Name = "user.delete", Module = "Users", Action = "Delete" },
                    new Permission { Name = "user.role.assign", Module = "Users", Action = "AssignRole" },
                    new Permission { Name = "department.view", Module = "Departments", Action = "View" },
                    new Permission { Name = "department.create", Module = "Departments", Action = "Create" },
                    new Permission { Name = "department.edit", Module = "Departments", Action = "Edit" },
                    new Permission { Name = "department.delete", Module = "Departments", Action = "Delete" },
                    new Permission { Name = "positions.view", Module = "Positions", Action = "View" },
                    new Permission { Name = "positions.create", Module = "Positions", Action = "Create" },
                    new Permission { Name = "positions.edit", Module = "Positions", Action = "Edit" },
                    new Permission { Name = "positions.delete", Module = "Positions", Action = "Delete" },
                    new Permission { Name = "role.manage", Module = "Roles", Action = "Manage" },
                    new Permission { Name = "role.view", Module = "Roles", Action = "View" },
                    new Permission { Name = "role.create", Module = "Roles", Action = "Create" },
                    new Permission { Name = "role.edit", Module = "Roles", Action = "Edit" },
                    new Permission { Name = "role.delete", Module = "Roles", Action = "Delete" },
                    new Permission { Name = "role.assignpermission", Module = "Roles", Action = "AssignPermission" },
                    new Permission { Name = "settings.manage", Module = "Settings", Action = "Manage" },
                };

                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();
            }

            // ========== ROLES ==========
            if (!await context.Roles.AnyAsync())
            {
                var permissions = await context.Permissions.ToListAsync();

                var roles = new List<Role>
                {
                    new Role { Name = "SystemAdmin", Description = "Sistem Yöneticisi - Tüm yetkilere sahiptir" },
                    new Role { Name = "Admin", Description = "Genel Yönetici - Tüm modüllerde yetkilidir" },
                    new Role { Name = "SatisMuduru", Description = "Satış Müdürü - Müşteri, Lead, Teklif yetkileri vardır" },
                    new Role { Name = "SatisTemsilcisi", Description = "Satış Temsilcisi - Müşteri ve Lead ekleyebilir, görüntüleyebilir" },
                    new Role { Name = "DestekUzmani", Description = "Destek Uzmanı - Ticket yönetebilir" },
                    new Role { Name = "Muhasebe", Description = "Muhasebeci - Sipariş ve Fatura yönetebilir" },
                    new Role { Name = "Viewer", Description = "İzleyici - Sadece görüntüleme yetkisi vardır" }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();

                var systemAdminRole = await context.Roles.FirstAsync(r => r.Name == "SystemAdmin");
                var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
                var salesManagerRole = await context.Roles.FirstAsync(r => r.Name == "SatisMuduru");
                var salesRepRole = await context.Roles.FirstAsync(r => r.Name == "SatisTemsilcisi");
                var supportRole = await context.Roles.FirstAsync(r => r.Name == "DestekUzmani");
                var accountantRole = await context.Roles.FirstAsync(r => r.Name == "Muhasebe");
                var viewerRole = await context.Roles.FirstAsync(r => r.Name == "Viewer");

                // SystemAdmin: Tüm yetkiler
                foreach (var permission in permissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = systemAdminRole.Id, PermissionId = permission.Id });
                }

                // Admin: Tüm yetkiler
                foreach (var permission in permissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = adminRole.Id, PermissionId = permission.Id });
                }

                // SatisMuduru
                var salesManagerPermissions = permissions.Where(p =>
                    p.Name.StartsWith("customer.") ||
                    p.Name.StartsWith("lead.") ||
                    p.Name == "product.view" ||
                    p.Name == "department.view" ||
                    p.Name == "order.view");

                foreach (var permission in salesManagerPermissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = salesManagerRole.Id, PermissionId = permission.Id });
                }

                // SatisTemsilcisi
                var salesRepPermissions = permissions.Where(p =>
                    p.Name == "customer.view" || p.Name == "customer.create" ||
                    p.Name == "lead.view" || p.Name == "lead.create");

                foreach (var permission in salesRepPermissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = salesRepRole.Id, PermissionId = permission.Id });
                }

                // DestekUzmani
                var supportPermissions = permissions.Where(p => p.Name.StartsWith("ticket."));
                foreach (var permission in supportPermissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = supportRole.Id, PermissionId = permission.Id });
                }

                // Muhasebe
                var accountantPermissions = permissions.Where(p =>
                    p.Name.StartsWith("order.") || p.Name.StartsWith("report."));

                foreach (var permission in accountantPermissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = accountantRole.Id, PermissionId = permission.Id });
                }

                // Viewer - Sadece görüntüleme yetkileri
                var viewerPermissions = permissions.Where(p => p.Name.EndsWith(".view"));
                foreach (var permission in viewerPermissions)
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    { RoleId = viewerRole.Id, PermissionId = permission.Id });
                }

                await context.SaveChangesAsync();
            }

            // ========== SYSTEM ADMIN USER ==========
            if (!await context.Users.AnyAsync(u => u.Email == "systemadmin@crm.com"))
            {
                CreatePasswordHash("SistemAdmin123.", out byte[] passwordHash, out byte[] passwordSalt);

                var systemAdminRole = await context.Roles.FirstAsync(r => r.Name == "SystemAdmin");

                var systemAdmin = new User
                {
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "systemadmin@crm.com",
                    PasswordHash = Convert.ToBase64String(passwordHash),
                    PasswordSalt = Convert.ToBase64String(passwordSalt),
                    RoleId = systemAdminRole.Id,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(systemAdmin);
                await context.SaveChangesAsync();

                //  PERSONEL KAYDI  OLUŞTUR!
                var createdUser = await context.Users.FirstAsync(u => u.Email == "systemadmin@crm.com");

                var systemAdminPersonel = new Personel
                {
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "systemadmin@crm.com",
                    Phone = "555-0000000",
                    UserId = createdUser.Id,       
                    IsActive = true,
                    Currency = "TRY",
                    CreatedAt = DateTime.UtcNow
                };

                await context.Personels.AddAsync(systemAdminPersonel);
                await context.SaveChangesAsync();

                Console.WriteLine("✅ System Admin ve Personel kaydı oluşturuldu!");
            }
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}