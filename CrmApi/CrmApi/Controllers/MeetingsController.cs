using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Meeting;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.MeetingValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MeetingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public MeetingsController(IUnitOfWork unitOfWork,IMapper mapper,ILogService logService,INotificationService notificationService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }

        
        // GET: api/meetings/customer-list
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
                    RequestPath = "/api/meetings/customer-list",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/meetings/lead-list
        [HttpGet("lead-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeadList()
        {
            try
            {
                var leads = await _unitOfWork.Query<Lead>()
                    .Where(l => !l.IsDeleted && l.Status != "MusteriOldu")
                    .OrderBy(l => l.CompanyName)
                    .Select(l => new {
                        l.Id,
                        l.CompanyName,
                        l.ContactName
                    })
                    .ToListAsync();

                return Ok(leads);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/meetings/lead-list",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpGet("status-list")]
        [AllowAnonymous]
        public IActionResult GetStatusList()
        {
            var statuses = new[]
            {
                new { Value = "Planlandı", Label = "📅 Planlandı", Color = "blue" },
                new { Value = "Devam Ediyor", Label = "🔄 Devam Ediyor", Color = "yellow" },
                new { Value = "Tamamlandı", Label = "✅ Tamamlandı", Color = "green" },
                new { Value = "İptal", Label = "❌ İptal", Color = "red" }
            };
            return Ok(statuses);
        }

        // GET: api/meetings
        [HttpGet]
      //  [HasPermission("meeting.view")]
        public async Task<IActionResult> GetAll([FromQuery] MeetingPaginationDto pagination)
        {
            try
            {
                var validator = new MeetingPaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var currentPersonelId = GetCurrentPersonelId();

                var query = _unitOfWork.Query<Meeting>()
                    .Include(m => m.Customer)
                    .Include(m => m.Lead)
                    .Include(m => m.Attendees)
                        .ThenInclude(a => a.Personel)
                    .AsQueryable();

                // Admin değilse sadece katılımcısı olduğu toplantıları görsün
                var isAdmin = await CheckPermissionAsync("meeting.viewall");
                if (!isAdmin && currentPersonelId.HasValue)
                {
                    query = query.Where(m => m.Attendees.Any(a => a.PersonelId == currentPersonelId.Value) ||
                                             m.CreatedByPersonelId == currentPersonelId.Value);
                }

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(m =>
                        m.Title.Contains(pagination.Search) ||
                        (m.Customer != null && m.Customer.FirstName.Contains(pagination.Search)) ||
                        (m.Customer != null && m.Customer.LastName.Contains(pagination.Search)) ||
                        (m.Lead != null && m.Lead.CompanyName.Contains(pagination.Search)));
                }

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(m => m.Status == pagination.Status);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(m => m.CustomerId == pagination.CustomerId);

                if (pagination.LeadId.HasValue)
                    query = query.Where(m => m.LeadId == pagination.LeadId);

                // Tarih filtreleri
                if (pagination.StartDate.HasValue)
                {
                    var startDate = pagination.StartDate.Value.Date;
                    query = query.Where(m => m.StartTime >= startDate);
                }

                if (pagination.EndDate.HasValue)
                {
                    var endDate = pagination.EndDate.Value.Date.AddDays(1);
                    query = query.Where(m => m.StartTime < endDate);
                }

                var totalCount = await query.CountAsync();
                var meetings = await query
                    .OrderByDescending(m => m.StartTime)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var meetingDtos = _mapper.Map<List<MeetingDto>>(meetings);

                var response = new MeetingPaginationResponse
                {
                    Data = meetingDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Meeting",
                    AdditionalInfo = $"Toplantı listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = currentPersonelId ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/meetings",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/meetings/{id}
        [HttpGet("{id}")]
       // [HasPermission("meeting.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var meeting = await _unitOfWork.Query<Meeting>()
                    .Include(m => m.Customer)
                    .Include(m => m.Lead)
                    .Include(m => m.Attendees)
                        .ThenInclude(a => a.Personel)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (meeting == null)
                    return NotFound(new { message = "Toplantı bulunamadı" });

                var meetingDto = _mapper.Map<MeetingDto>(meeting);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Meeting",
                    EntityId = id,
                    AdditionalInfo = $"Toplantı detayı görüntülendi: {meeting.Title}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(meetingDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/meetings/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // POST: api/meetings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMeetingDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Toplantı bilgileri eksik" });

                var validator = new CreateMeetingValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                var meeting = _mapper.Map<Meeting>(request);
                meeting.CreatedAt = DateTime.UtcNow;
                meeting.CreatedByPersonelId = currentPersonelId.Value;

                await _unitOfWork.AddAsync(meeting);
                await _unitOfWork.CompleteAsync();

                //  OLUŞTURAN KİŞİYİ OTOMATİK KATILIMCI YAP
                var allAttendeeIds = new List<int>(request.AttendeePersonelIds);
                if (!allAttendeeIds.Contains(currentPersonelId.Value))
                {
                    allAttendeeIds.Add(currentPersonelId.Value);
                }

                // Katılımcıları ekle
                foreach (var personelId in allAttendeeIds)
                {
                    var attendee = new MeetingAttendee
                    {
                        MeetingId = meeting.Id,
                        PersonelId = personelId,
                        AttendanceStatus = personelId == currentPersonelId.Value ? "Katılıyorum" : "Beklemede",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AddAsync(attendee);
                }
                await _unitOfWork.CompleteAsync();

                // Toplantıyı yeniden yükle
                var createdMeeting = await _unitOfWork.Query<Meeting>()
                    .Include(m => m.Attendees)
                        .ThenInclude(a => a.Personel)
                    .FirstOrDefaultAsync(m => m.Id == meeting.Id);

                var meetingDto = _mapper.Map<MeetingDto>(createdMeeting);

                // BİLDİRİM GÖNDER
                foreach (var attendee in allAttendeeIds)
                {
                    if (attendee != currentPersonelId.Value)
                    {
                        await _notificationService.SendToPersonelAsync(
                            personelId: attendee,
                            title: "Yeni Toplantı Daveti",
                            message: $"{meeting.Title} toplantısına davet edildiniz. Tarih: {meeting.StartTime:dd.MM.yyyy HH:mm}",
                            type: "Meeting",
                            relatedEntityId: meeting.Id,
                            relatedEntityType: "Meeting"
                        );
                    }
                }

                await _notificationService.SendToAdminsAsync(
                    title: "Yeni Toplantı",
                    message: $"{meeting.Title} toplantısı oluşturuldu. Tarih: {meeting.StartTime:dd.MM.yyyy HH:mm}",
                    type: "Meeting",
                    relatedEntityId: meeting.Id,
                    relatedEntityType: "Meeting"
                );

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Meeting",
                    EntityId = meeting.Id,
                    AdditionalInfo = $"Yeni toplantı oluşturuldu: {meeting.Title} (Tarih: {meeting.StartTime})",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });


                // Signalr bildirim
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Toplantı",
                    Message = $"{meeting.Title} toplantısı oluşturuldu",
                    Type = "MeetingCreated",
                    Timestamp = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("RefreshMeetings");

                return Ok(meetingDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/meetings",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }




        // PUT: api/meetings/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMeetingDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Toplantı bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var validator = new UpdateMeetingValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var meeting = await _unitOfWork.Query<Meeting>()
                    .Include(m => m.Attendees)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (meeting == null)
                    return NotFound(new { message = "Toplantı bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized(new { message = "Kullanıcı bilgileriniz alınamadı." });

                // SADECE TOPLANTIYI AÇAN KİŞİ DÜZENLEYEBİLİR
                if (meeting.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new
                    {
                        message = "Sadece toplantıyı açan kişi düzenleyebilir."
                    });
                }

                var oldTitle = meeting.Title;

                _mapper.Map(request, meeting);
                meeting.UpdatedAt = DateTime.UtcNow;

                var existingAttendeeIds = meeting.Attendees.Select(a => a.PersonelId).ToList();
                var newAttendeeIds = request.AttendeePersonelIds ?? new List<int>();

                var addedAttendees = newAttendeeIds.Except(existingAttendeeIds);
                foreach (var personelId in addedAttendees)
                {
                    var attendee = new MeetingAttendee
                    {
                        MeetingId = meeting.Id,
                        PersonelId = personelId,
                        AttendanceStatus = "Beklemede",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AddAsync(attendee);
                }

                //  Çıkarılanlar - CREATOR KENDİNİ ÇIKARAMAZ
                var removedAttendees = existingAttendeeIds.Except(newAttendeeIds);
                foreach (var personelId in removedAttendees)
                {
                    // Creator kendini çıkaramaz
                    if (personelId == meeting.CreatedByPersonelId)
                    {
                        continue; 
                    }

                    var attendee = meeting.Attendees.FirstOrDefault(a => a.PersonelId == personelId);
                    if (attendee != null)
                    {
                        _unitOfWork.Delete(attendee);
                    }
                }

                _unitOfWork.Update(meeting);
                await _unitOfWork.CompleteAsync();


                //  DATABASE'E BİLDİRİM KAYDET (Toplantı sahibine)
                var creatorNotification = new Notification
                {
                    PersonelId = meeting.CreatedByPersonelId,
                    Title = "Toplantı Güncellendi",
                    Message = $"{meeting.Title} toplantısı güncellendi. Tarih: {meeting.StartTime:dd.MM.yyyy HH:mm}",
                    Type = "Meeting",
                    RelatedEntityId = meeting.Id,
                    RelatedEntityType = "Meeting",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AddAsync(creatorNotification);

                //  Katılımcılara da bildirim kaydet
                foreach (var attendee in meeting.Attendees)
                {
                    if (attendee.PersonelId != meeting.CreatedByPersonelId)
                    {
                        var attendeeNotification = new Notification
                        {
                            PersonelId = attendee.PersonelId,
                            Title = "Toplantı Güncellendi",
                            Message = $"{meeting.Title} toplantısı güncellendi. Tarih: {meeting.StartTime:dd.MM.yyyy HH:mm}",
                            Type = "Meeting",
                            RelatedEntityId = meeting.Id,
                            RelatedEntityType = "Meeting",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.AddAsync(attendeeNotification);
                    }
                }

                await _unitOfWork.CompleteAsync();




                var updatedMeeting = await _unitOfWork.Query<Meeting>()
                    .Include(m => m.Customer)
                    .Include(m => m.Lead)
                    .Include(m => m.Attendees)
                        .ThenInclude(a => a.Personel)
                    .FirstOrDefaultAsync(m => m.Id == meeting.Id);

                var meetingDto = _mapper.Map<MeetingDto>(updatedMeeting);


                // Signalr bildirim
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Toplantı Güncellendi",
                    Message = $"{meeting.Title} toplantısı güncellendi",
                    Type = "MeetingUpdated",
                    Timestamp = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("RefreshMeetings");

                return Ok(meetingDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/meetings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var meeting = await _unitOfWork.Query<Meeting>()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (meeting == null)
                    return NotFound(new { message = "Toplantı bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized(new { message = "Kullanıcı bilgileriniz alınamadı." });

                // SADECE TOPLANTIYI AÇAN KİŞİ SİLEBİLİR
                if (meeting.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new
                    {
                        message = "Sadece toplantıyı açan kişi silebilir."
                    });
                }

                _unitOfWork.Delete(meeting);
                await _unitOfWork.CompleteAsync();


                // Signalr bildirim
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Toplantı Silindi",
                    Message = $"{meeting.Title} toplantısı silindi",
                    Type = "MeetingDeleted",
                    Timestamp = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("RefreshMeetings");

                return Ok(new { message = "Toplantı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


      

        // POST: api/meetings/attendance/status
        [HttpPost("attendance/status")]
       // [HasPermission("meeting.view")]
        public async Task<IActionResult> UpdateAttendanceStatus([FromBody] UpdateAttendanceStatusDto request)
        {
            try
            {
                var attendee = await _unitOfWork.Query<MeetingAttendee>()
                    .FirstOrDefaultAsync(a => a.MeetingId == request.MeetingId && a.PersonelId == request.PersonelId);

                if (attendee == null)
                    return NotFound(new { message = "Katılımcı bulunamadı" });

                attendee.AttendanceStatus = request.AttendanceStatus;
                attendee.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(attendee);
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "Katılım durumu güncellendi", status = attendee.AttendanceStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private async Task<bool> CheckPermissionAsync(string permission)
        {
            var personelId = GetCurrentPersonelId();
            if (!personelId.HasValue) return false;

            var personel = await _unitOfWork.Query<Personel>()
                .Include(p => p.User)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(p => p.Id == personelId.Value);

            if (personel?.User?.Role?.Name == "SystemAdmin" || personel?.User?.Role?.Name == "Admin")
                return true;

            return personel?.User?.Role?.RolePermissions?
                .Any(rp => rp.Permission.Name == permission) ?? false;
        }

        private int? GetCurrentPersonelId()
        {
            var personelIdClaim = User.FindFirst("PersonelId")?.Value;
            if (!string.IsNullOrEmpty(personelIdClaim) && int.TryParse(personelIdClaim, out int personelId))
                return personelId;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var personel = _unitOfWork.Query<Personel>().FirstOrDefault(p => p.UserId == userId);
                return personel?.Id;
            }

            return null;
        }

       
    }
}
