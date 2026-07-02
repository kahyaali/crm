using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Ticket;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.TicketValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly INotificationService _notificationService;

        public TicketsController(IUnitOfWork unitOfWork,IMapper mapper,ILogService logService,IHubContext<NotificationHub> notificationHub,INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _notificationHub = notificationHub;
            _notificationService = notificationService;
        }

        [HttpGet("status-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatusList()
        {
            var statuses = new List<object>
            {
                new { Value = "Open", Label = "🟢 Açık" },
                new { Value = "InProgress", Label = "🔵 İşlemde" },
                new { Value = "OnHold", Label = "🟡 Beklemede" },
                new { Value = "Resolved", Label = "✅ Çözüldü" },
                new { Value = "Closed", Label = "🔒 Kapandı" }
            };
            return Ok(statuses);
        }

        [HttpGet("priority-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPriorityList()
        {
            var priorities = new List<object>
            {
                new { Value = "Low", Label = "🟢 Düşük" },
                new { Value = "Medium", Label = "🟡 Orta" },
                new { Value = "High", Label = "🟠 Yüksek" },
                new { Value = "Critical", Label = "🔴 Acil" }
            };
            return Ok(priorities);
        }

        [HttpGet("category-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryList()
        {
            var categories = new List<object>
            {
                new { Value = "Complaint", Label = "Şikayet" },
                new { Value = "Request", Label = "Talep" },
                new { Value = "Information", Label = "Bilgi" },
                new { Value = "Technical", Label = "Teknik Destek" }
            };
            return Ok(categories);
        }

        [HttpGet("all")]
       // [HasPermission("ticket.viewall")]
        public async Task<IActionResult> GetAllIncludingAll([FromQuery] TicketPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Ticket>()
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedToPersonel)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(t =>
                        (t.TicketNumber != null && t.TicketNumber.Contains(pagination.Search)) ||
                        t.Subject.Contains(pagination.Search) ||
                        (t.Customer != null && (t.Customer.FirstName + " " + t.Customer.LastName).Contains(pagination.Search)));
                }

                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(t => t.Status != null && t.Status == pagination.Status);

                if (!string.IsNullOrEmpty(pagination.Priority))
                    query = query.Where(t => t.Priority != null && t.Priority == pagination.Priority);

                if (!string.IsNullOrEmpty(pagination.Category))
                    query = query.Where(t => t.Category != null && t.Category == pagination.Category);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(t => t.CustomerId == pagination.CustomerId.Value);

                if (pagination.AssignedToPersonelId.HasValue)
                    query = query.Where(t => t.AssignedToPersonelId == pagination.AssignedToPersonelId.Value);

                if (pagination.StartDate.HasValue)
                    query = query.Where(t => t.CreatedAt >= pagination.StartDate.Value);

                if (pagination.EndDate.HasValue)
                    query = query.Where(t => t.CreatedAt <= pagination.EndDate.Value);

                var totalCount = await query.CountAsync();
                var tickets = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var ticketDtos = _mapper.Map<List<TicketDto>>(tickets);

                var response = new TicketPaginationResponse
                {
                    Data = ticketDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                var currentUserId = GetCurrentPersonelId();
                if (currentUserId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "VIEW",
                        EntityType = "Ticket",
                        AdditionalInfo = $"Tüm ticketlar listelendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                        UserId = currentUserId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tickets/all",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        private async Task<bool> CheckPermissionAsync(string permission)
        {
            // Önce PersonelId'yi bul
            var personelId = GetCurrentPersonelId();
            if (!personelId.HasValue) return false;

            // Personel üzerinden User'a ulaş
            var personel = await _unitOfWork.Query<Personel>()
                .Include(p => p.User)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(p => p.Id == personelId.Value);

            if (personel?.User == null) return false;

            // SystemAdmin veya Admin ise her şeyi görebilir
            var roleName = personel.User.Role?.Name;
            if (roleName == "SystemAdmin" || roleName == "Admin")
                return true;

            // Normal personel için permission kontrolü
            return personel.User.Role?.RolePermissions?
                .Any(rp => rp.Permission.Name == permission) ?? false;
        }



        [HttpGet]
       // [HasPermission("ticket.view")]
        public async Task<IActionResult> GetAll([FromQuery] TicketPaginationDto pagination)
        {
            try
            {
                var currentPersonelId = GetCurrentPersonelId();
                var isAdmin = await CheckPermissionAsync("ticket.viewall");  

                var query = _unitOfWork.Query<Ticket>()
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedToPersonel)
                    .Where(t => t.Status != "Closed")
                    .AsQueryable();

                //  PERSONEL FİLTRESİ - SADECE KENDİ TICKET'LARINI GÖRSÜN
                if (!isAdmin && currentPersonelId.HasValue)
                {
                    query = query.Where(t =>
                        t.CreatedByPersonelId == currentPersonelId.Value ||
                        t.AssignedToPersonelId == currentPersonelId.Value);
                }

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(t =>
                        (t.TicketNumber != null && t.TicketNumber.Contains(pagination.Search)) ||
                        t.Subject.Contains(pagination.Search) ||
                        (t.Customer != null && (t.Customer.FirstName + " " + t.Customer.LastName).Contains(pagination.Search)));
                }

                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(t => t.Status != null && t.Status == pagination.Status);

                if (!string.IsNullOrEmpty(pagination.Priority))
                    query = query.Where(t => t.Priority != null && t.Priority == pagination.Priority);

                if (!string.IsNullOrEmpty(pagination.Category))
                    query = query.Where(t => t.Category != null && t.Category == pagination.Category);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(t => t.CustomerId == pagination.CustomerId.Value);

                var totalCount = await query.CountAsync();
                var tickets = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var ticketDtos = _mapper.Map<List<TicketDto>>(tickets);

                var response = new TicketPaginationResponse
                {
                    Data = ticketDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                if (currentPersonelId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "VIEW",
                        EntityType = "Ticket",
                        AdditionalInfo = $"Aktif ticketlar listelendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                        UserId = currentPersonelId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tickets",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        //[HasPermission("ticket.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var ticket = await _unitOfWork.Query<Ticket>()
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedToPersonel)
                    .Include(t => t.CreatedByPersonel)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Personel)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                {
                    return NotFound(new { message = $"Ticket bulunamadı (ID: {id})" });
                }

                var ticketDto = _mapper.Map<TicketDetailDto>(ticket);

                var currentUserId = GetCurrentPersonelId();
                if (currentUserId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "VIEW",
                        EntityType = "Ticket",
                        EntityId = id,
                        AdditionalInfo = $"Ticket detayı görüntülendi: {ticket.TicketNumber}",
                        UserId = currentUserId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(ticketDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tickets/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }



        [HttpPost]
        //[HasPermission("ticket.create")]
        public async Task<IActionResult> Create([FromBody] CreateTicketDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Ticket bilgileri eksik" });

                var validator = new CreateTicketValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var ticket = _mapper.Map<Ticket>(request);
                ticket.TicketNumber = GenerateTicketNumber();
                ticket.Status = "Open";
                ticket.CreatedAt = DateTime.UtcNow;

                var currentPersonelId = GetCurrentPersonelId();
                if (currentPersonelId.HasValue)
                {
                    ticket.CreatedByPersonelId = currentPersonelId.Value;
                }

                // Atanan personel seçilmediyse kendine ata
                if (!ticket.AssignedToPersonelId.HasValue && currentPersonelId.HasValue)
                {
                    ticket.AssignedToPersonelId = currentPersonelId.Value;
                }

                await _unitOfWork.AddAsync(ticket);
                await _unitOfWork.CompleteAsync();

                ticket = await _unitOfWork.Query<Ticket>()
                    .Include(t => t.Customer)
                    .Include(t => t.AssignedToPersonel)
                    .FirstOrDefaultAsync(t => t.Id == ticket.Id);

                var ticketDto = _mapper.Map<TicketDto>(ticket);

                //  SIGNALR BİLDİRİMİ (mevcut)
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "NewTicket",
                        Title = "Yeni Destek Talebi",
                        Message = $"#{ticket?.TicketNumber} - {ticket?.Subject} talebi oluşturuldu",
                        TicketId = ticket?.Id,
                        TicketNumber = ticket?.TicketNumber,
                        Timestamp = DateTime.UtcNow
                    });
                }

                //  VERİTABANINA BİLDİRİM KAYDET (NotificationService ile)
                var notificationService = HttpContext.RequestServices.GetRequiredService<INotificationService>();

                // Ticket atanan personele bildirim
                if (ticket.AssignedToPersonelId.HasValue)
                {
                    await notificationService.SendToPersonelAsync(
                        personelId: ticket.AssignedToPersonelId.Value,
                        title: "Yeni Destek Talebi",
                        message: $"#{ticket.TicketNumber} - {ticket.Subject} size atandı",
                        type: "Ticket",
                        relatedEntityId: ticket.Id,
                        relatedEntityType: "Ticket"
                    );
                }

                // Admin'lere bildirim
                await notificationService.SendToAdminsAsync(
                    title: "Yeni Destek Talebi",
                    message: $"#{ticket.TicketNumber} - {ticket.Subject} oluşturuldu",
                    type: "Ticket",
                    relatedEntityId: ticket.Id,
                    relatedEntityType: "Ticket"
                );

                // Ticket oluşturan kişiye bildirim (eğer atanan kişiden farklıysa)
                if (currentPersonelId.HasValue && ticket.AssignedToPersonelId != currentPersonelId)
                {
                    await notificationService.SendToPersonelAsync(
                        personelId: currentPersonelId.Value,
                        title: "Ticket Oluşturuldu",
                        message: $"#{ticket.TicketNumber} - {ticket.Subject} talebi başarıyla oluşturuldu",
                        type: "Ticket",
                        relatedEntityId: ticket.Id,
                        relatedEntityType: "Ticket"
                    );
                }

                if (currentPersonelId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "CREATE",
                        EntityType = "Ticket",
                        EntityId = ticket?.Id,
                        AdditionalInfo = $"Yeni ticket oluşturuldu: {ticket?.TicketNumber} - {ticket?.Subject}",
                        UserId = currentPersonelId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(ticketDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tickets",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

       

        [HttpPut("{id}")]
        // [HasPermission("ticket.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Ticket bilgileri eksik" });

                var ticket = await _unitOfWork.GetByIdAsync<Ticket>(id);
                if (ticket == null)
                    return NotFound(new { message = $"Ticket bulunamadı (ID: {id})" });

                var validator = new UpdateTicketValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var oldStatus = ticket.Status;

                _mapper.Map(request, ticket);
                ticket.UpdatedAt = DateTime.UtcNow;

                if (request.Status == "Resolved" && ticket.ResolvedAt == null)
                    ticket.ResolvedAt = DateTime.UtcNow;
                else if (request.Status == "Closed" && ticket.ClosedAt == null)
                    ticket.ClosedAt = DateTime.UtcNow;

                _unitOfWork.Update(ticket);
                await _unitOfWork.CompleteAsync();

                var ticketDto = _mapper.Map<TicketDto>(ticket);



                //  STATUS DEĞİŞTİYSE BİLDİRİM
                if (oldStatus != ticket.Status)
                {
                    // Atanan personele bildirim
                    var personelId = ticket.AssignedToPersonelId ?? ticket.CreatedByPersonelId;
                    if (personelId.HasValue && personelId.Value > 0)
                    {
                        await _notificationService.SendToPersonelAsync(
                            personelId: personelId.Value,
                            title: "Ticket Durumu Güncellendi",
                            message: $"#{ticket.TicketNumber} durumu: {oldStatus} → {ticket.Status}",
                            type: "Ticket",
                            relatedEntityId: ticket.Id,
                            relatedEntityType: "Ticket"
                        );
                    }

                    // Admin'lere bildirim
                    await _notificationService.SendToAdminsAsync(
                        title: "Ticket Durumu Güncellendi",
                        message: $"#{ticket.TicketNumber} durumu: {oldStatus} → {ticket.Status}",
                        type: "Ticket",
                        relatedEntityId: ticket.Id,
                        relatedEntityType: "Ticket"
                    );
                }


                //  HER ZAMAN BİLDİRİM GÖNDER (Status değişmese bile)
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "TicketUpdated",
                        Title = "Ticket Güncellendi",
                        Message = $"#{ticket.TicketNumber} ticket'ı güncellendi",
                        TicketId = ticket.Id,
                        TicketNumber = ticket.TicketNumber,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Status değiştiyse ekstra bildirim gönder
                if (oldStatus != ticket.Status && _notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "TicketStatusChanged",
                        Title = "Ticket Durumu Güncellendi",
                        Message = $"#{ticket.TicketNumber} durumu: {oldStatus} → {ticket.Status}",
                        TicketId = ticket.Id,
                        OldStatus = oldStatus,
                        NewStatus = ticket.Status,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var currentUserId = GetCurrentPersonelId();
                if (currentUserId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "UPDATE",
                        EntityType = "Ticket",
                        EntityId = id,
                        AdditionalInfo = $"Ticket güncellendi: {ticket.TicketNumber} (Status: {oldStatus} → {ticket.Status})",
                        UserId = currentUserId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(ticketDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tickets/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        [HttpPost("{ticketId}/comments")]
      //  [HasPermission("ticket.comment")]
        public async Task<IActionResult> AddComment(int ticketId, [FromBody] CreateTicketCommentDto request)
        {
            try
            {
                var ticket = await _unitOfWork.GetByIdAsync<Ticket>(ticketId);
                if (ticket == null)
                    return NotFound(new { message = $"Ticket bulunamadı (ID: {ticketId})" });

                var comment = _mapper.Map<TicketComment>(request);
                comment.TicketId = ticketId;

                var currentPersonelId = GetCurrentPersonelId();
                if (currentPersonelId.HasValue)
                {
                    comment.PersonelId = currentPersonelId.Value;
                }
                comment.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.AddAsync(comment);

                bool statusChanged = false;
                string oldStatus = ticket.Status;

                if (request.IsSolution && ticket.Status != "Resolved")
                {
                    ticket.Status = "Resolved";
                    ticket.ResolvedAt = DateTime.UtcNow;
                    _unitOfWork.Update(ticket);
                    statusChanged = true;
                }

                await _unitOfWork.CompleteAsync();

                comment = await _unitOfWork.Query<TicketComment>()
                    .Include(c => c.Personel)
                    .FirstOrDefaultAsync(c => c.Id == comment.Id);

                var commentResponse = _mapper.Map<TicketCommentDto>(comment);


                //  YORUM BİLDİRİMİ (GÜVENLİ VERSİYON)
                var personelId = ticket.AssignedToPersonelId ?? ticket.CreatedByPersonelId;
                if (personelId.HasValue && personelId.Value > 0)
                {
                    await _notificationService.SendToPersonelAsync(
                        personelId: personelId.Value,
                        title: "Yeni Yorum",
                        message: $"#{ticket.TicketNumber} ticket'ına yeni yorum eklendi",
                        type: "Ticket",
                        relatedEntityId: ticket.Id,
                        relatedEntityType: "Ticket"
                    );
                }

                // Admin'lere de bildirim gönder (çözüm notu için)
                if (request.IsSolution)
                {
                    await _notificationService.SendToAdminsAsync(
                        title: "Ticket Çözüldü",
                        message: $"#{ticket.TicketNumber} ticket'ı çözüldü",
                        type: "Ticket",
                        relatedEntityId: ticket.Id,
                        relatedEntityType: "Ticket"
                    );
                }

                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "NewComment",
                        Title = "Yeni Yorum",
                        Message = $"#{ticket.TicketNumber} ticket'ına yeni yorum eklendi",
                        TicketId = ticket.Id,
                        TicketNumber = ticket.TicketNumber,
                        Comment = request.Comment,
                        IsSolution = request.IsSolution,
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (currentPersonelId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "COMMENT",
                        EntityType = "Ticket",
                        EntityId = ticketId,
                        AdditionalInfo = $"Ticket'a yorum eklendi: {ticket.TicketNumber} (Çözüm: {request.IsSolution})",
                        UserId = currentPersonelId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(commentResponse);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tickets/{ticketId}/comments",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        [HttpDelete("comments/{commentId}")]
        // [HasPermission("ticket.comment.delete")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                var comment = await _unitOfWork.GetByIdAsync<TicketComment>(commentId);
                if (comment == null)
                    return NotFound(new { message = "Yorum bulunamadı" });

                var ticket = await _unitOfWork.GetByIdAsync<Ticket>(comment.TicketId);

                _unitOfWork.Delete(comment);
                await _unitOfWork.CompleteAsync();

                // Bildirim gönder
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "CommentDeleted",
                        Title = "Yorum Silindi",
                        Message = $"#{ticket?.TicketNumber} ticket'ından bir yorum silindi",
                        TicketId = comment.TicketId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new { message = "Yorum silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpGet("stats")]
      //  [HasPermission("ticket.view")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var query = _unitOfWork.Query<Ticket>();

                var stats = new TicketStatsDto
                {
                    Total = await query.CountAsync(),
                    Open = await query.CountAsync(t => t.Status == "Open"),
                    InProgress = await query.CountAsync(t => t.Status == "InProgress"),
                    Resolved = await query.CountAsync(t => t.Status == "Resolved"),
                    Closed = await query.CountAsync(t => t.Status == "Closed"),
                    Critical = await query.CountAsync(t => t.Priority == "Critical" && t.Status != "Closed"),
                    AverageResolutionTime = await query
                        .Where(t => t.ResolvedAt != null)
                        .AverageAsync(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tickets/stats",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        //[HasPermission("ticket.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ticket = await _unitOfWork.GetByIdAsync<Ticket>(id);
                if (ticket == null)
                    return NotFound(new { message = $"Ticket bulunamadı (ID: {id})" });

                var ticketNumber = ticket.TicketNumber;

                _unitOfWork.Delete(ticket);
                await _unitOfWork.CompleteAsync();

                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "TicketDeleted",
                        Title = "Ticket Silindi",
                        Message = $"#{ticketNumber} ticket'ı silindi",
                        TicketId = id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var currentUserId = GetCurrentPersonelId();
                if (currentUserId.HasValue)
                {
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "DELETE",
                        EntityType = "Ticket",
                        EntityId = id,
                        AdditionalInfo = $"Ticket silindi: {ticketNumber}",
                        UserId = currentUserId.Value,
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return Ok(new { message = "Ticket silindi", id = id });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tickets/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // HELPER METHODS
        private string GenerateTicketNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month.ToString("D2");
            var count = _unitOfWork.Query<Ticket>()
                .Count(t => t.CreatedAt.Year == DateTime.Now.Year && t.CreatedAt.Month == DateTime.Now.Month) + 1;
            return $"TKT-{year}{month}-{count:D4}";
        }


      
        private int? GetCurrentPersonelId()
        {
            // Token'dan PersonelId claim'ini al
            var personelIdClaim = User.FindFirst("PersonelId")?.Value;
            if (personelIdClaim != null && int.TryParse(personelIdClaim, out int personelId))
            {
                return personelId;
            }

            // Yoksa UserId'den Personel bul (yedek)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {
                var personel = _unitOfWork.Query<Personel>()
                    .FirstOrDefault(p => p.UserId == userId);
                return personel?.Id;
            }

            return null;
        }


        // GET: api/tickets/personel-list
        [HttpGet("personel-list")]
        [AllowAnonymous]  // Geçici, sonra [HasPermission("ticket.view")] yaparsın
        public async Task<IActionResult> GetPersonelList()
        {
            try
            {
                var personels = await _unitOfWork.Query<Personel>()
                    .Where(p => p.IsActive && !p.IsDeleted)
                    .Select(p => new {
                        p.Id,
                        p.FirstName,
                        p.LastName,
                        p.Email
                    })
                    .OrderBy(p => p.FirstName)
                    .ToListAsync();

                return Ok(personels);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tickets/personel-list",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // TicketsController.cs - EKLE

        // GET: api/tickets/customer-list
        [HttpGet("customer-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomerList()
        {
            try
            {
                var customers = await _unitOfWork.Query<Customer>()
                    .Where(c => !c.IsDeleted && c.IsActive)  
                    .OrderBy(c => c.FirstName)
                    .Select(c => new {
                        c.Id,
                        c.FirstName,
                        c.LastName,
                        c.CompanyName
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tickets/customer-list",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }
    }
}