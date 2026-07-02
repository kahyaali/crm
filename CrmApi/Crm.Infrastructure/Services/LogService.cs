using Crm.Domain.Entities;
using Crm.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Crm.Infrastructure.Helpers;


namespace Crm.Infrastructure.Services
{
    public class LogService:ILogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        public LogService(AppDbContext context)
        {
            _context = context;
            _httpContextAccessor = null;
        }
        // Yeni constructor (IHttpContextAccessor ile)
        public LogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ========== YENİ BASIT METODLAR ==========

        public async Task LogActionAsync(string actionType, string entityType, int? entityId = null, string? additionalInfo = null)
        {
            var log = new ActionLog
            {
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                AdditionalInfo = additionalInfo,
                CreatedAt = DateTime.UtcNow
            };

            if (_httpContextAccessor?.HttpContext != null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                log.UserId = HttpContextHelper.GetCurrentUserId(httpContext);
                log.IpAddress = HttpContextHelper.GetClientIp(httpContext);
                log.UserAgent = HttpContextHelper.GetUserAgent(httpContext);
            }

            await LogActionAsync(log);
        }

        public async Task LogErrorAsync(Exception ex, string? errorLevel = "Error", string? additionalInfo = null)
        {
            var log = new ErrorLog
            {
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                ErrorLevel = errorLevel,
                ResolutionNote = additionalInfo,
                CreatedAt = DateTime.UtcNow
            };

            if (_httpContextAccessor?.HttpContext != null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                log.RequestPath = httpContext.Request.Path;
                log.RequestMethod = httpContext.Request.Method;
                log.IpAddress = HttpContextHelper.GetClientIp(httpContext);
                log.UserId = HttpContextHelper.GetCurrentUserId(httpContext);
            }

            await LogErrorAsync(log);
        }

        // ========== MEVCUT DETAYLI METODLAR ==========

        public async Task LogActionAsync(ActionLog log)
        {
            if (log.CreatedAt == default)
                log.CreatedAt = DateTime.UtcNow;
            await _context.ActionLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogErrorAsync(ErrorLog log)
        {
            if (log.CreatedAt == default)
                log.CreatedAt = DateTime.UtcNow;
            await _context.ErrorLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        // ========== GET METODLARI (Geliştirilmiş) ==========

        public async Task<List<ActionLog>> GetActionLogsAsync(int? userId = null, string? actionType = null,
            string? entityType = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ActionLogs
                .Include(x => x.User)
                .Include(x => x.Personel)
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(x => x.ActionType == actionType);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(x => x.EntityType == entityType);

            if (startDate.HasValue)
                query = query.Where(x => x.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(x => x.CreatedAt <= endDate);

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<List<ErrorLog>> GetErrorLogsAsync(string? errorLevel = null, bool? isResolved = null,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ErrorLogs
                .Include(x => x.User)
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(errorLevel))
                query = query.Where(x => x.ErrorLevel == errorLevel);

            if (isResolved.HasValue)
                query = query.Where(x => x.IsResolved == isResolved);

            if (startDate.HasValue)
                query = query.Where(x => x.CreatedAt >= startDate);

            if (endDate.HasValue)
                query = query.Where(x => x.CreatedAt <= endDate);

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        // ========== SOFT DELETE ==========

        public async Task<bool> SoftDeleteActionLogsAsync(List<int> ids)
        {
            await _context.ActionLogs
                .Where(x => ids.Contains(x.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            return true;
        }

        public async Task<bool> SoftDeleteErrorLogsAsync(List<int> ids)
        {
            await _context.ErrorLogs
                .Where(x => ids.Contains(x.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            return true;
        }

        public async Task<bool> SoftDeleteAllActionLogsAsync()
        {
            await _context.ActionLogs
                .Where(x => !x.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            return true;
        }

        public async Task<bool> SoftDeleteAllErrorLogsAsync()
        {
            await _context.ErrorLogs
                .Where(x => !x.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            return true;
        }

        public async Task<bool> SoftDeleteActionLogsByDateAsync(DateTime olderThan)
        {
            await _context.ActionLogs
                .Where(x => x.CreatedAt < olderThan && !x.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            return true;
        }

        public async Task<bool> SoftDeleteErrorLogsByDateAsync(DateTime olderThan)
        {
            await _context.ErrorLogs
                .Where(x => x.CreatedAt < olderThan && !x.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
            return true;
        }

        // ========== HARD DELETE ==========

        public async Task<bool> HardDeleteActionLogsAsync(List<int> ids)
        {
            await _context.ActionLogs.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
            return true;
        }

        public async Task<bool> HardDeleteErrorLogsAsync(List<int> ids)
        {
            await _context.ErrorLogs.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
            return true;
        }

        public async Task<bool> HardDeleteAllActionLogsAsync()
        {
            await _context.ActionLogs.ExecuteDeleteAsync();
            return true;
        }

        public async Task<bool> HardDeleteAllErrorLogsAsync()
        {
            await _context.ErrorLogs.ExecuteDeleteAsync();
            return true;
        }

        public async Task<bool> HardDeleteActionLogsByDateAsync(DateTime olderThan)
        {
            await _context.ActionLogs.Where(x => x.CreatedAt < olderThan).ExecuteDeleteAsync();
            return true;
        }

        public async Task<bool> HardDeleteErrorLogsByDateAsync(DateTime olderThan)
        {
            await _context.ErrorLogs.Where(x => x.CreatedAt < olderThan).ExecuteDeleteAsync();
            return true;
        }

        // ========== ERROR RESOLUTION ==========

        public async Task<bool> ResolveErrorAsync(int errorId, string resolutionNote)
        {
            var error = await _context.ErrorLogs.FindAsync(errorId);
            if (error == null) return false;

            error.IsResolved = true;
            error.ResolvedAt = DateTime.UtcNow;
            error.ResolutionNote = resolutionNote;
            error.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== STATISTICS ==========

        public async Task<int> GetActionLogCountAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ActionLogs.Where(x => !x.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(x => x.CreatedAt >= startDate);
            if (endDate.HasValue)
                query = query.Where(x => x.CreatedAt <= endDate);

            return await query.CountAsync();
        }

        public async Task<int> GetErrorLogCountAsync(bool? isResolved = null)
        {
            var query = _context.ErrorLogs.Where(x => !x.IsDeleted);

            if (isResolved.HasValue)
                query = query.Where(x => x.IsResolved == isResolved);

            return await query.CountAsync();
        }


    }
}
