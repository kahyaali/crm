using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Campaign;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Validators.CampaignValidator;
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
    public class CampaignsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public CampaignsController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _hubContext = hubContext;
        }

        // GET: api/campaigns
        [HttpGet]
        [HasPermission("campaign.view")]
        public async Task<IActionResult> GetAll([FromQuery] CampaignPaginationDto pagination)
        {
            try
            {
                var validator = new CampaignPaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var query = _unitOfWork.Query<Campaign>()
                    .Include(c => c.CreatedByPersonel)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(c =>
                        c.Name.Contains(pagination.Search) ||
                        c.Description.Contains(pagination.Search));
                }

                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(c => c.Status == pagination.Status);

                if (!string.IsNullOrEmpty(pagination.Type))
                    query = query.Where(c => c.Type == pagination.Type);

                if (pagination.StartDate.HasValue)
                    query = query.Where(c => c.StartDate >= pagination.StartDate);

                if (pagination.EndDate.HasValue)
                    query = query.Where(c => c.EndDate <= pagination.EndDate.Value.Date.AddDays(1));

                var totalCount = await query.CountAsync();
                var campaigns = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var campaignDtos = _mapper.Map<List<CampaignDto>>(campaigns);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Campaign",
                    AdditionalInfo = $"Kampanya listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    Data = campaignDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/campaigns",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/campaigns/{id}
        [HttpGet("{id}")]
       // [HasPermission("campaign.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var campaign = await _unitOfWork.Query<Campaign>()
                    .Include(c => c.CreatedByPersonel)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (campaign == null)
                    return NotFound(new { message = "Kampanya bulunamadı" });

                var campaignDto = _mapper.Map<CampaignDto>(campaign);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Campaign",
                    EntityId = id,
                    AdditionalInfo = $"Kampanya detayı görüntülendi: {campaign.Name}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(campaignDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/campaigns/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/campaigns
        [HttpPost]
     //   [HasPermission("campaign.create")]
        public async Task<IActionResult> Create([FromBody] CreateCampaignDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Kampanya bilgileri eksik" });

                var validator = new CreateCampaignValidator();
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

                var campaign = _mapper.Map<Campaign>(request);
                campaign.CreatedAt = DateTime.UtcNow;
                campaign.CreatedByPersonelId = currentPersonelId.Value;

                await _unitOfWork.AddAsync(campaign);
                await _unitOfWork.CompleteAsync();

                //  DATABASE'E BİLDİRİM KAYDET (Kampanya sahibine)
                var notification = new Notification
                {
                    PersonelId = currentPersonelId.Value,
                    Title = "Kampanya Oluşturuldu",
                    Message = $"{campaign.Name} kampanyası başarıyla oluşturuldu.",
                    Type = "Campaign",
                    RelatedEntityId = campaign.Id,
                    RelatedEntityType = "Campaign",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AddAsync(notification);

                //  ADMIN'LERE DE BİLDİRİM KAYDET
                var admins = await _unitOfWork.Query<Personel>()
                    .Where(p => p.User.Role.Name == "SystemAdmin" || p.User.Role.Name == "Admin")
                    .ToListAsync();

                foreach (var admin in admins)
                {
                    if (admin.Id != currentPersonelId.Value)
                    {
                        var adminNotification = new Notification
                        {
                            PersonelId = admin.Id,
                            Title = "Yeni Kampanya Oluşturuldu",
                            Message = $"{campaign.Name} kampanyası oluşturuldu.",
                            Type = "Campaign",
                            RelatedEntityId = campaign.Id,
                            RelatedEntityType = "Campaign",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.AddAsync(adminNotification);
                    }
                }

                await _unitOfWork.CompleteAsync();

             
                var createdCampaign = await _unitOfWork.Query<Campaign>()
                    .Include(c => c.CreatedByPersonel)
                    .FirstOrDefaultAsync(c => c.Id == campaign.Id);

                var campaignDto = _mapper.Map<CampaignDto>(createdCampaign);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Campaign",
                    EntityId = campaign.Id,
                    AdditionalInfo = $"Yeni kampanya oluşturuldu: {campaign.Name}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Kampanya",
                    Message = $"{campaign.Name} kampanyası oluşturuldu",
                    Type = "CampaignCreated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshCampaigns");

                return Ok(campaignDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/campaigns",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/campaigns/{id}
        [HttpPut("{id}")]
      //  [HasPermission("campaign.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCampaignDto request)
        {
            try
            {
                var validator = new UpdateCampaignValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                if (request == null)
                    return BadRequest(new { message = "Kampanya bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var campaign = await _unitOfWork.Query<Campaign>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (campaign == null)
                    return NotFound(new { message = "Kampanya bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (campaign.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece kampanyayı oluşturan kişi düzenleyebilir." });
                }

                var oldName = campaign.Name;
                var oldStatus = campaign.Status;

                _mapper.Map(request, campaign);
                campaign.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(campaign);
                await _unitOfWork.CompleteAsync();

                var updatedCampaign = await _unitOfWork.Query<Campaign>()
                    .Include(c => c.CreatedByPersonel)
                    .FirstOrDefaultAsync(c => c.Id == campaign.Id);

                var campaignDto = _mapper.Map<CampaignDto>(updatedCampaign);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Campaign",
                    EntityId = id,
                    AdditionalInfo = $"Kampanya güncellendi: {oldName} -> {campaign.Name}, Durum: {oldStatus} -> {campaign.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Kampanya Güncellendi",
                    Message = $"{campaign.Name} kampanyası güncellendi",
                    Type = "CampaignUpdated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshCampaigns");

                return Ok(campaignDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/campaigns/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/campaigns/{id}
        [HttpDelete("{id}")]
      //  [HasPermission("campaign.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var campaign = await _unitOfWork.Query<Campaign>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (campaign == null)
                    return NotFound(new { message = "Kampanya bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (campaign.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece kampanyayı oluşturan kişi silebilir." });
                }

                _unitOfWork.Delete(campaign);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Campaign",
                    EntityId = id,
                    AdditionalInfo = $"Kampanya silindi: {campaign.Name}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Kampanya Silindi",
                    Message = $"{campaign.Name} kampanyası silindi",
                    Type = "CampaignDeleted",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshCampaigns");

                return Ok(new { message = "Kampanya başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/campaigns/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/campaigns/{id}/activate
        [HttpPost("{id}/activate")]
     //   [HasPermission("campaign.edit")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var campaign = await _unitOfWork.Query<Campaign>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (campaign == null)
                    return NotFound(new { message = "Kampanya bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (campaign.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece kampanyayı oluşturan kişi aktifleştirebilir." });
                }

                if (campaign.Status == "Aktif")
                    return BadRequest(new { message = "Kampanya zaten aktif." });

                campaign.Status = "Aktif";
                campaign.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(campaign);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Campaign",
                    EntityId = id,
                    AdditionalInfo = $"Kampanya aktifleştirildi: {campaign.Name}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Kampanya Aktifleştirildi",
                    Message = $"{campaign.Name} kampanyası aktifleştirildi",
                    Type = "CampaignActivated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshCampaigns");

                return Ok(new { message = "Kampanya başarıyla aktifleştirildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/campaigns/{id}/complete
        [HttpPost("{id}/complete")]
      //  [HasPermission("campaign.edit")]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var campaign = await _unitOfWork.Query<Campaign>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (campaign == null)
                    return NotFound(new { message = "Kampanya bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (campaign.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece kampanyayı oluşturan kişi tamamlayabilir." });
                }

                if (campaign.Status == "Tamamlandı")
                    return BadRequest(new { message = "Kampanya zaten tamamlanmış." });

                campaign.Status = "Tamamlandı";
                campaign.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(campaign);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "COMPLETE",
                    EntityType = "Campaign",
                    EntityId = id,
                    AdditionalInfo = $"Kampanya tamamlandı: {campaign.Name}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Kampanya Tamamlandı",
                    Message = $"{campaign.Name} kampanyası tamamlandı",
                    Type = "CampaignCompleted",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshCampaigns");

                return Ok(new { message = "Kampanya başarıyla tamamlandı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/campaigns/{id}/cancel
        [HttpPost("{id}/cancel")]
      //  [HasPermission("campaign.edit")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var campaign = await _unitOfWork.Query<Campaign>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (campaign == null)
                    return NotFound(new { message = "Kampanya bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (campaign.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece kampanyayı oluşturan kişi iptal edebilir." });
                }

                if (campaign.Status == "İptal")
                    return BadRequest(new { message = "Kampanya zaten iptal edilmiş." });

                campaign.Status = "İptal";
                campaign.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(campaign);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CANCEL",
                    EntityType = "Campaign",
                    EntityId = id,
                    AdditionalInfo = $"Kampanya iptal edildi: {campaign.Name}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Kampanya İptal Edildi",
                    Message = $"{campaign.Name} kampanyası iptal edildi",
                    Type = "CampaignCancelled",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshCampaigns");

                return Ok(new { message = "Kampanya başarıyla iptal edildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
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
