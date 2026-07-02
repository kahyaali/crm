using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        // Generic Repository'ler
        private IGenericRepository<User>? _users;
        private IGenericRepository<Personel>? _personels;
        private IGenericRepository<Customer>? _customers;
        private IGenericRepository<Lead>? _leads;
        private IGenericRepository<Opportunity>? _opportunities;
        private IGenericRepository<Product>? _products;
        private IGenericRepository<Order>? _orders;
        private IGenericRepository<OrderItem>? _orderItems;
        private IGenericRepository<Invoice>? _invoices;
        private IGenericRepository<Ticket>? _tickets;
        private IGenericRepository<TicketComment>? _ticketComments;
        private IGenericRepository<DomainTask>? _tasks;
        private IGenericRepository<Department>? _departments;
        private IGenericRepository<Position>? _positions;
        private IGenericRepository<Role>? _roles;
        private IGenericRepository<Permission>? _permissions;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        // Repository Property'ler
        public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
        public IGenericRepository<Personel> Personels => _personels ??= new GenericRepository<Personel>(_context);
        public IGenericRepository<Customer> Customers => _customers ??= new GenericRepository<Customer>(_context);
        public IGenericRepository<Lead> Leads => _leads ??= new GenericRepository<Lead>(_context);
        public IGenericRepository<Opportunity> Opportunities => _opportunities ??= new GenericRepository<Opportunity>(_context);
        public IGenericRepository<Product> Products => _products ??= new GenericRepository<Product>(_context);
        public IGenericRepository<Order> Orders => _orders ??= new GenericRepository<Order>(_context);
        public IGenericRepository<OrderItem> OrderItems => _orderItems ??= new GenericRepository<OrderItem>(_context);
        public IGenericRepository<Invoice> Invoices => _invoices ??= new GenericRepository<Invoice>(_context);
        public IGenericRepository<Ticket> Tickets => _tickets ??= new GenericRepository<Ticket>(_context);
        public IGenericRepository<TicketComment> TicketComments => _ticketComments ??= new GenericRepository<TicketComment>(_context);
        public IGenericRepository<DomainTask> Tasks => _tasks ??= new GenericRepository<DomainTask>(_context);
        public IGenericRepository<Department> Departments => _departments ??= new GenericRepository<Department>(_context);
        public IGenericRepository<Position> Positions => _positions ??= new GenericRepository<Position>(_context);
        public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);
        public IGenericRepository<Permission> Permissions => _permissions ??= new GenericRepository<Permission>(_context);

        // Direct Query Methods
        public IQueryable<T> Query<T>() where T : BaseEntity
        {
            return _context.Set<T>().AsQueryable();
        }

        public async Task<T?> GetByIdAsync<T>(int id) where T : BaseEntity
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<T> AddAsync<T>(T entity) where T : BaseEntity
        {
            entity.CreatedAt = DateTime.UtcNow;
            await _context.Set<T>().AddAsync(entity);
            return entity;
        }

        public async Task AddRangeAsync<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            var entityList = entities.ToList();
            if (!entityList.Any()) return;

            foreach (var entity in entityList)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _context.Set<T>().AddRangeAsync(entityList);
        }

        public void Update<T>(T entity) where T : BaseEntity
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Set<T>().Update(entity);
        }

        public void Delete<T>(T entity) where T : BaseEntity
        {
            _context.Set<T>().Remove(entity);
        }

        public void SoftDelete<T>(T entity) where T : BaseEntity
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Set<T>().Update(entity);
        }

        public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            return await _context.Set<T>().AnyAsync(predicate);
        }

        public void DeleteRange<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            _context.Set<T>().RemoveRange(entities);
        }

        // Transaction Methods
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}