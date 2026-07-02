using Crm.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Infrastructure.Services
{
    public interface ILogService
    {
        // ========== BASIT LOG METODLARI (HttpContextHelper otomatik kullanır) ==========
        /// <summary>
        /// Kolay loglama için - HttpContext bilgilerini otomatik alır
        /// </summary>
        Task LogActionAsync(string actionType, string entityType, int? entityId = null, string? additionalInfo = null);

        /// <summary>
        /// Exception loglamak için kolay metod
        /// </summary>
        Task LogErrorAsync(Exception ex, string? errorLevel = "Error", string? additionalInfo = null);

        // ========== DETAYLI LOG METODLARI ==========
        Task LogActionAsync(ActionLog log);
        Task LogErrorAsync(ErrorLog log);

        // ========== GET METODLARI (Geliştirilmiş) ==========
        Task<List<ActionLog>> GetActionLogsAsync(int? userId = null, string? actionType = null,
            string? entityType = null, DateTime? startDate = null, DateTime? endDate = null);

        Task<List<ErrorLog>> GetErrorLogsAsync(string? errorLevel = null, bool? isResolved = null,
            DateTime? startDate = null, DateTime? endDate = null);

        // ========== SOFT DELETE ==========
        Task<bool> SoftDeleteActionLogsAsync(List<int> ids);
        Task<bool> SoftDeleteErrorLogsAsync(List<int> ids);
        Task<bool> SoftDeleteAllActionLogsAsync();
        Task<bool> SoftDeleteAllErrorLogsAsync();

        /// <summary>Belirtilen tarihten eski logları soft delete yapar</summary>
        Task<bool> SoftDeleteActionLogsByDateAsync(DateTime olderThan);
        Task<bool> SoftDeleteErrorLogsByDateAsync(DateTime olderThan);

        // ========== HARD DELETE ==========
        Task<bool> HardDeleteActionLogsAsync(List<int> ids);
        Task<bool> HardDeleteErrorLogsAsync(List<int> ids);
        Task<bool> HardDeleteAllActionLogsAsync();
        Task<bool> HardDeleteAllErrorLogsAsync();

        /// <summary>Belirtilen tarihten eski logları kalıcı olarak siler</summary>
        Task<bool> HardDeleteActionLogsByDateAsync(DateTime olderThan);
        Task<bool> HardDeleteErrorLogsByDateAsync(DateTime olderThan);

        // ========== ERROR RESOLUTION ==========
        Task<bool> ResolveErrorAsync(int errorId, string resolutionNote);

        // ========== STATISTICS (Yeni) ==========
        Task<int> GetActionLogCountAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetErrorLogCountAsync(bool? isResolved = null);
    }
}
