using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.ActionLogs;
using Crm.Application.DTOs.BulkDeleteLogs;
using Crm.Application.DTOs.ErrorLogs;
using Crm.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [HasPermission("logs.view")]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly IMapper _mapper;

        public LogsController(ILogService logService, IMapper mapper)
        {
            _logService = logService;
            _mapper = mapper;
        }

    
        // ========== ACTION LOGS ==========

        [HttpGet("actions")]
        public async Task<IActionResult> GetActionLogs([FromQuery] ActionLogPaginationDto pagination)
        {
            var logs = await _logService.GetActionLogsAsync(
                userId: pagination.UserId,
                actionType: pagination.ActionType,
                entityType: pagination.EntityType,
                startDate: pagination.StartDate,
                endDate: pagination.EndDate
            );

            var query = logs.AsQueryable();

            if (!string.IsNullOrEmpty(pagination.Search))
                query = query.Where(x =>
                    x.EntityType.Contains(pagination.Search) ||
                    (x.User != null && x.User.Email.Contains(pagination.Search)) ||
                    (x.AdditionalInfo != null && x.AdditionalInfo.Contains(pagination.Search))
                );

            if (pagination.PersonelId.HasValue)
                query = query.Where(x => x.PersonelId == pagination.PersonelId);

            var totalCount = query.Count();
            var items = query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            var dtos = _mapper.Map<List<ActionLogDto>>(items);

            return Ok(new
            {
                data = dtos,
                totalCount,
                page = pagination.Page,
                pageSize = pagination.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            });
        }

        // ========== ERROR LOGS ==========

        [HttpGet("errors")]
        public async Task<IActionResult> GetErrorLogs([FromQuery] ErrorLogPaginationDto pagination)
        {
            var logs = await _logService.GetErrorLogsAsync(
                errorLevel: pagination.ErrorLevel,
                isResolved: pagination.IsResolved,
                startDate: pagination.StartDate,
                endDate: pagination.EndDate
            );

            var query = logs.AsQueryable();

            if (!string.IsNullOrEmpty(pagination.Search))
                query = query.Where(x =>
                    x.ErrorMessage.Contains(pagination.Search) ||
                    (x.ResolutionNote != null && x.ResolutionNote.Contains(pagination.Search))
                );

            var totalCount = query.Count();
            var items = query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            var dtos = _mapper.Map<List<ErrorLogDto>>(items);

            return Ok(new
            {
                data = dtos,
                totalCount,
                page = pagination.Page,
                pageSize = pagination.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
            });
        }

        // ========== SOFT DELETE (ACTION LOGS) ==========

        [HttpPost("actions/soft-delete")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> SoftDeleteActions([FromBody] BulkDeleteLogDto request)
        {
            if (request.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "En az bir ID seçmelisiniz" });

            await _logService.SoftDeleteActionLogsAsync(request.Ids);
            return Ok(new { message = $"{request.Ids.Count} adet action log soft delete edildi" });
        }

        // ========== HARD DELETE (ACTION LOGS) ==========

        [HttpPost("actions/hard-delete")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> HardDeleteActions([FromBody] BulkDeleteLogDto request)
        {
            if (request.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "En az bir ID seçmelisiniz" });

            await _logService.HardDeleteActionLogsAsync(request.Ids);
            return Ok(new { message = $"{request.Ids.Count} adet action log kalıcı olarak silindi" });
        }

        // ========== SOFT DELETE (ERROR LOGS) ==========

        [HttpPost("errors/soft-delete")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> SoftDeleteErrors([FromBody] BulkDeleteErrorLogDto request)
        {
            if (request.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "En az bir ID seçmelisiniz" });

            await _logService.SoftDeleteErrorLogsAsync(request.Ids);
            return Ok(new { message = $"{request.Ids.Count} adet error log soft delete edildi" });
        }

        // ========== HARD DELETE (ERROR LOGS) ==========

        [HttpPost("errors/hard-delete")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> HardDeleteErrors([FromBody] BulkDeleteErrorLogDto request)
        {
            if (request.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "En az bir ID seçmelisiniz" });

            await _logService.HardDeleteErrorLogsAsync(request.Ids);
            return Ok(new { message = $"{request.Ids.Count} adet error log kalıcı olarak silindi" });
        }

        // ========== RESOLVE ERROR ==========

        [HttpPost("errors/{id}/resolve")]
        [HasPermission("logs.edit")]
        public async Task<IActionResult> ResolveError(int id, [FromBody] ResolveErrorDto request)
        {
            if (string.IsNullOrEmpty(request.ResolutionNote))
                return BadRequest(new { message = "Çözüm notu zorunludur" });

            var result = await _logService.ResolveErrorAsync(id, request.ResolutionNote);
            if (!result)
                return NotFound(new { message = "Hata logu bulunamadı" });

            return Ok(new { message = "Hata çözüldü olarak işaretlendi" });
        }

        // ========== DELETE ALL ==========

        [HttpDelete("actions/all/soft")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> SoftDeleteAllActions()
        {
            await _logService.SoftDeleteAllActionLogsAsync();
            return Ok(new { message = "Tüm action loglar soft delete edildi" });
        }

        [HttpDelete("actions/all/hard")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> HardDeleteAllActions()
        {
            await _logService.HardDeleteAllActionLogsAsync();
            return Ok(new { message = "Tüm action loglar kalıcı olarak silindi" });
        }

        [HttpDelete("errors/all/soft")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> SoftDeleteAllErrors()
        {
            await _logService.SoftDeleteAllErrorLogsAsync();
            return Ok(new { message = "Tüm error loglar soft delete edildi" });
        }

        [HttpDelete("errors/all/hard")]
        [HasPermission("logs.delete")]
        public async Task<IActionResult> HardDeleteAllErrors()
        {
            await _logService.HardDeleteAllErrorLogsAsync();
            return Ok(new { message = "Tüm error loglar kalıcı olarak silindi" });
        }

        // ========== STATISTICS ==========

        [HttpGet("stats/actions")]
        public async Task<IActionResult> GetActionLogStats()
        {
            var totalCount = await _logService.GetActionLogCountAsync();
            var last7DaysCount = await _logService.GetActionLogCountAsync(
                startDate: DateTime.UtcNow.AddDays(-7),
                endDate: DateTime.UtcNow
            );

            var logs = await _logService.GetActionLogsAsync(startDate: DateTime.UtcNow.AddDays(-30));

            var stats = new
            {
                TotalActions = totalCount,
                Last7DaysActions = last7DaysCount,
                ActionsByType = logs.GroupBy(x => x.ActionType)
                    .Select(g => new { ActionType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count),
                ActionsByEntity = logs.GroupBy(x => x.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count),
                Last7DaysDetail = logs
                    .Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
            };

            return Ok(stats);
        }

        [HttpGet("stats/errors")]
        public async Task<IActionResult> GetErrorLogStats()
        {
            var totalCount = await _logService.GetErrorLogCountAsync();
            var resolvedCount = await _logService.GetErrorLogCountAsync(isResolved: true);
            var unresolvedCount = await _logService.GetErrorLogCountAsync(isResolved: false);

            var logs = await _logService.GetErrorLogsAsync();

            var stats = new
            {
                TotalErrors = totalCount,
                ResolvedErrors = resolvedCount,
                UnresolvedErrors = unresolvedCount,
                ErrorsByLevel = logs.GroupBy(x => x.ErrorLevel)
                    .Select(g => new { ErrorLevel = g.Key ?? "Unknown", Count = g.Count() })
                    .OrderByDescending(x => x.Count),
                MostCommonErrors = logs.GroupBy(x => x.ErrorMessage)
                    .Select(g => new { ErrorMessage = g.Key.Length > 100 ? g.Key.Substring(0, 100) + "..." : g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
            };

            return Ok(stats);
        }
    }
}