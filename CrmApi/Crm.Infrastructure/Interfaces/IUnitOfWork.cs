using System;
using System.Linq;
using System.Threading.Tasks;
using Crm.Domain.Entities;

namespace Crm.Infrastructure.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        // Generic Repository'ler
        IGenericRepository<User> Users { get; }
        IGenericRepository<Personel> Personels { get; }
        IGenericRepository<Customer> Customers { get; }
        IGenericRepository<Lead> Leads { get; }
        IGenericRepository<Opportunity> Opportunities { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Order> Orders { get; }
        IGenericRepository<OrderItem> OrderItems { get; }
        IGenericRepository<Invoice> Invoices { get; }
        IGenericRepository<Ticket> Tickets { get; }
        IGenericRepository<TicketComment> TicketComments { get; }
        IGenericRepository<DomainTask> Tasks { get; }
        IGenericRepository<Department> Departments { get; }
        IGenericRepository<Position> Positions { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<Permission> Permissions { get; }

        // Direct Query Methods
        IQueryable<T> Query<T>() where T : BaseEntity;
        Task<T?> GetByIdAsync<T>(int id) where T : BaseEntity;
        Task<T> AddAsync<T>(T entity) where T : BaseEntity;

        Task AddRangeAsync<T>(IEnumerable<T> entities) where T : BaseEntity;

        void Update<T>(T entity) where T : BaseEntity;
        void Delete<T>(T entity) where T : BaseEntity;
        void SoftDelete<T>(T entity) where T : BaseEntity;
        Task<bool> AnyAsync<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate) where T : BaseEntity;

        void DeleteRange<T>(IEnumerable<T> entities) where T : BaseEntity;

        // Transaction Methods
        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}