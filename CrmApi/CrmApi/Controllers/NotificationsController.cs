using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Notification;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.NotificationValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly INotificationService _notificationService;

        public NotificationsController(IUnitOfWork unitOfWork,IMapper mapper,ILogService logService,IHubContext<NotificationHub> notificationHub, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _notificationHub = notificationHub;
            _notificationService = notificationService;
        }

        // GET: api/notifications
        [HttpGet]
       // [HasPermission("notification.view")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] NotificationPaginationDto pagination)
        {
            try
            {
                // Validasyon
                var validator = new NotificationPaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized(new { message = "Personel bilgisi bulunamadı" });

                var query = _unitOfWork.Query<Notification>()
                    .Where(n => n.PersonelId == currentPersonelId.Value || n.PersonelId == null)
                    .OrderByDescending(n => n.CreatedAt)
                    .AsQueryable();

                if (pagination.IsRead.HasValue)
                    query = query.Where(n => n.IsRead == pagination.IsRead.Value);

                if (!string.IsNullOrEmpty(pagination.Type))
                    query = query.Where(n => n.Type == pagination.Type);

                if (pagination.StartDate.HasValue)
                    query = query.Where(n => n.CreatedAt >= pagination.StartDate.Value);

                if (pagination.EndDate.HasValue)
                    query = query.Where(n => n.CreatedAt <= pagination.EndDate.Value);

                var totalCount = await query.CountAsync();
                var notifications = await query
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var notificationDtos = _mapper.Map<List<NotificationDto>>(notifications);

                var unreadCount = await _unitOfWork.Query<Notification>()
                    .Where(n => (n.PersonelId == currentPersonelId.Value || n.PersonelId == null) && !n.IsRead)
                    .CountAsync();

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Notification",
                    AdditionalInfo = $"Bildirimler listelendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    data = notificationDtos,
                    totalCount = totalCount,
                    unreadCount = unreadCount,
                    page = pagination.Page,
                    pageSize = pagination.PageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/notifications",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized(new { message = "Personel bilgisi bulunamadı" });

                var count = await _unitOfWork.Query<Notification>()
                    .Where(n => (n.PersonelId == currentPersonelId.Value || n.PersonelId == null) && !n.IsRead)
                    .CountAsync();

                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/notifications/unread-count",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/notifications/{id}/mark-as-read
        [HttpPost("{id}/mark-as-read")]
       // [HasPermission("notification.edit")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var notification = await _unitOfWork.GetByIdAsync<Notification>(id);
                if (notification == null)
                    return NotFound(new { message = "Bildirim bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (notification.PersonelId.HasValue && notification.PersonelId != currentPersonelId)
                    return Forbid();

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                _unitOfWork.Update(notification);
                await _unitOfWork.CompleteAsync();

                // SignalR ile okundu bildirimi
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("SendNotificationToUser", currentPersonelId.Value,
                        "Bildirim Okundu", notification.Title);
                }

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Notification",
                    EntityId = id,
                    AdditionalInfo = $"Bildirim okundu olarak işaretlendi: {notification.Title}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Bildirim okundu olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/notifications/{id}/mark-as-read",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/notifications/mark-all-as-read
        [HttpPost("mark-all-as-read")]
       // [HasPermission("notification.edit")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized(new { message = "Personel bilgisi bulunamadı" });

                var notifications = await _unitOfWork.Query<Notification>()
                    .Where(n => (n.PersonelId == currentPersonelId.Value || n.PersonelId == null) && !n.IsRead)
                    .ToListAsync();

                var count = notifications.Count;

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _unitOfWork.CompleteAsync();

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Notification",
                    AdditionalInfo = $"Tüm bildirimler okundu olarak işaretlendi ({count} adet)",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Tüm bildirimler okundu olarak işaretlendi", count = count });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/notifications/mark-all-as-read",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/notifications/{id}
        [HttpDelete("{id}")]
       // [HasPermission("notification.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var notification = await _unitOfWork.GetByIdAsync<Notification>(id);
                if (notification == null)
                    return NotFound(new { message = "Bildirim bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                var title = notification.Title;

                _unitOfWork.Delete(notification);
                await _unitOfWork.CompleteAsync();

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Notification",
                    EntityId = id,
                    AdditionalInfo = $"Bildirim silindi: {title}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Bildirim silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/notifications/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/notifications/delete-all-read
        [HttpDelete("delete-all-read")]
       // [HasPermission("notification.delete")]
        public async Task<IActionResult> DeleteAllRead()
        {
            try
            {
                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized(new { message = "Personel bilgisi bulunamadı" });

                var notifications = await _unitOfWork.Query<Notification>()
                    .Where(n => (n.PersonelId == currentPersonelId.Value || n.PersonelId == null) && n.IsRead)
                    .ToListAsync();

                var count = notifications.Count;
                _unitOfWork.DeleteRange(notifications);
                await _unitOfWork.CompleteAsync();

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Notification",
                    AdditionalInfo = $"Okunmuş bildirimler toplu silindi ({count} adet)",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = $"{count} adet okunmuş bildirim silindi", count = count });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/notifications/delete-all-read",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        private int? GetCurrentPersonelId()
        {
            var personelIdClaim = User.FindFirst("PersonelId")?.Value;
            if (personelIdClaim != null && int.TryParse(personelIdClaim, out int personelId))
                return personelId;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                var personel = _unitOfWork.Query<Personel>().FirstOrDefault(p => p.UserId == userId);
                return personel?.Id;
            }

            return null;
        }


       
    }
}