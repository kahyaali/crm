using Crm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Mevcut DbSet'ler
        public DbSet<User> Users { get; set; }
        public DbSet<Personel> Personels { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<MailSetting> MailSettings { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        // Yeni Ticari Entity'ler
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteItem> QuoteItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<DomainTask> Tasks { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MeetingAttendee> MeetingAttendees { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<CompanySetting> CompanySettings { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ExchangeRateSetting> ExchangeRateSettings { get; set; }

       
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== BRAND İLİŞKİLERİ ==========
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== RBAC ==========
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // ========== PERSONEL İLİŞKİLERİ ==========
            modelBuilder.Entity<Personel>()
                .HasOne(p => p.Department)
                .WithMany(d => d.Personels)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Personel>()
                .HasOne(p => p.Position)
                .WithMany(pos => pos.Personels)
                .HasForeignKey(p => p.PositionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Personel>()
                .HasOne(p => p.Manager)
                .WithMany(p => p.Subordinates)
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Personel)
                .WithOne(p => p.User)
                .HasForeignKey<Personel>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== CUSTOMER İLİŞKİLERİ ==========
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.AssignedToPersonel)
                .WithMany(p => p.AssignedCustomers)
                .HasForeignKey(c => c.AssignedToPersonelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.CreatedByPersonel)
                .WithMany()
                .HasForeignKey(c => c.CreatedByPersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== LEAD İLİŞKİLERİ ==========
            modelBuilder.Entity<Lead>()
                .HasOne(l => l.AssignedToPersonel)
                .WithMany(p => p.AssignedLeads)
                .HasForeignKey(l => l.AssignedToPersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.ConvertedToCustomer)
                .WithMany()
                .HasForeignKey(l => l.ConvertedToCustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Campaign)
                .WithMany()
                .HasForeignKey(l => l.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== OPPORTUNITY İLİŞKİLERİ ==========
            modelBuilder.Entity<Opportunity>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Opportunity>()
                .HasOne(o => o.AssignedToPersonel)
                .WithMany()
                .HasForeignKey(o => o.AssignedToPersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Opportunity>()
                 .HasOne(o => o.CreatedByPersonel)
                 .WithMany()
                 .HasForeignKey(o => o.CreatedByPersonelId)
                 .OnDelete(DeleteBehavior.Restrict);

            // ========== QUOTE İLİŞKİLERİ ==========
            modelBuilder.Entity<Quote>()
                .HasOne(q => q.Customer)
                .WithMany()
                .HasForeignKey(q => q.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Quote>()
                .HasOne(q => q.Opportunity)
                .WithMany(o => o.Quotes)
                .HasForeignKey(q => q.OpportunityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuoteItem>()
                .HasOne(qi => qi.Quote)
                .WithMany(q => q.Items)
                .HasForeignKey(qi => qi.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuoteItem>()
                .HasOne(qi => qi.Product)
                .WithMany()
                .HasForeignKey(qi => qi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== ORDER İLİŞKİLERİ ==========
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Quote)
                .WithMany()
                .HasForeignKey(o => o.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== INVOICE & PAYMENT ==========
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Invoices)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ReceivedByPersonel)
                .WithMany()
                .HasForeignKey(p => p.ReceivedByPersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            // InvoiceItem ilişkileri
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Product)
                .WithMany()
                .HasForeignKey(ii => ii.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== PRODUCT & CATEGORY ==========
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.ParentCategory)
                .WithMany(pc => pc.SubCategories)
                .HasForeignKey(pc => pc.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== DOMAINTASK KONFİGÜRASYONLARI ==========
            modelBuilder.Entity<DomainTask>()
                .HasOne(t => t.AssignedToPersonel)
                .WithMany(p => p.AssignedTasks)
                .HasForeignKey(t => t.AssignedToPersonelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DomainTask>()
                .HasOne(t => t.CreatedByPersonel)
                .WithMany()
                .HasForeignKey(t => t.CreatedByPersonelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DomainTask>()
                .HasOne(t => t.RelatedToCustomer)
                .WithMany()
                .HasForeignKey(t => t.RelatedToCustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DomainTask>()
                .HasOne(t => t.RelatedToLead)
                .WithMany()
                .HasForeignKey(t => t.RelatedToLeadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DomainTask>()
                .HasOne(t => t.RelatedToOpportunity)
                .WithMany()
                .HasForeignKey(t => t.RelatedToOpportunityId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== MEETING İLİŞKİLERİ ==========
            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.Customer)
                .WithMany()
                .HasForeignKey(m => m.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.Lead)
                .WithMany()
                .HasForeignKey(m => m.LeadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MeetingAttendee>()
                .HasOne(ma => ma.Meeting)
                .WithMany(m => m.Attendees)
                .HasForeignKey(ma => ma.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MeetingAttendee>()
                .HasOne(ma => ma.Personel)
                .WithMany()
                .HasForeignKey(ma => ma.PersonelId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== TICKET İLİŞKİLERİ ==========
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedToPersonel)
                .WithMany(p => p.AssignedTickets)
                .HasForeignKey(t => t.AssignedToPersonelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedByPersonel)
                .WithMany()
                .HasForeignKey(t => t.CreatedByPersonelId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // ========== TICKET COMMENT ==========
            modelBuilder.Entity<TicketComment>()
                .HasOne(tc => tc.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(tc => tc.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketComment>()
                .HasOne(tc => tc.Personel)
                .WithMany()
                .HasForeignKey(tc => tc.PersonelId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== NOTIFICATION ==========
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Personel)
                .WithMany()
                .HasForeignKey(n => n.PersonelId)
                .OnDelete(DeleteBehavior.SetNull);


            // ========== CONTRACT ==========
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            //  CreatedByPersonel
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.CreatedByPersonel)
                .WithMany()
                .HasForeignKey(c => c.CreatedByPersonelId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Quote
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Quote)
                .WithMany()
                .HasForeignKey(c => c.QuoteId)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== CAMPAIGN ==========
            modelBuilder.Entity<Campaign>()
                .HasOne(c => c.CreatedByPersonel)
                .WithMany()
                .HasForeignKey(c => c.CreatedByPersonelId)
                .OnDelete(DeleteBehavior.Restrict);


            // ========== ACTION & ERROR LOGS ==========
            modelBuilder.Entity<ActionLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActionLog>()
                .HasOne(a => a.Personel)
                .WithMany()
                .HasForeignKey(a => a.PersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ErrorLog>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== RBAC İLİŞKİLERİ ==========
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========== INDEX'LER ==========
            modelBuilder.Entity<Brand>().HasIndex(b => b.Name).IsUnique();
            modelBuilder.Entity<Brand>().HasIndex(b => b.IsActive);
            modelBuilder.Entity<ActionLog>().HasIndex(a => new { a.ActionType, a.EntityType, a.CreatedAt });
            modelBuilder.Entity<ErrorLog>().HasIndex(e => new { e.ErrorLevel, e.CreatedAt });
            modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
            modelBuilder.Entity<Customer>().HasIndex(c => c.TaxNumber);
            modelBuilder.Entity<Customer>().HasIndex(c => c.AccountNumber).IsUnique();
            modelBuilder.Entity<Personel>().HasIndex(p => p.Email).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<Personel>().HasIndex(p => p.PersonnelNumber).IsUnique().HasFilter("[PersonnelNumber] IS NOT NULL AND [IsDeleted] = 0");
            modelBuilder.Entity<Personel>().HasIndex(p => p.RegistrationNumber).IsUnique().HasFilter("[RegistrationNumber] IS NOT NULL AND [IsDeleted] = 0");
            modelBuilder.Entity<Lead>().HasIndex(l => l.Status);
            modelBuilder.Entity<Lead>().HasIndex(l => l.AssignedToPersonelId);
            modelBuilder.Entity<Opportunity>().HasIndex(o => o.Stage);
            modelBuilder.Entity<Opportunity>().HasIndex(o => o.ExpectedCloseDate);
            modelBuilder.Entity<Quote>().HasIndex(q => q.QuoteNumber).IsUnique();
            modelBuilder.Entity<Quote>().HasIndex(q => q.Status);
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.Status);
            modelBuilder.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
            modelBuilder.Entity<Invoice>().HasIndex(i => i.Status);
            modelBuilder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.Barcode);
            modelBuilder.Entity<Ticket>().HasIndex(t => t.TicketNumber).IsUnique();
            modelBuilder.Entity<Ticket>().HasIndex(t => t.Status);
            modelBuilder.Entity<Contract>().HasIndex(c => c.ContractNumber).IsUnique();
            modelBuilder.Entity<Campaign>().HasIndex(c => c.Status);
            modelBuilder.Entity<Notification>().HasIndex(n => new { n.PersonelId, n.IsRead, n.CreatedAt });
            modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.Name).IsUnique();

            // ========== SOFT DELETE FILTERS ==========
            modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Personel>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Customer>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ActionLog>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ErrorLog>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Brand>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Lead>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Opportunity>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Quote>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<QuoteItem>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Order>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<OrderItem>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Invoice>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Payment>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ProductCategory>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<DomainTask>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Meeting>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<MeetingAttendee>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Ticket>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<TicketComment>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Notification>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Contract>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Campaign>().HasQueryFilter(x => !x.IsDeleted);
        }
    }
}