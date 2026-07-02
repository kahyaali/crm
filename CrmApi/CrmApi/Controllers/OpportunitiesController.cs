using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Opportunity;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Validators.OpportunityValidator;
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
    public class OpportunitiesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OpportunitiesController(IUnitOfWork unitOfWork,IMapper mapper,ILogService logService,IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _hubContext = hubContext;
        }

        [HttpGet]
      //  [HasPermission("opportunity.view")]
        public async Task<IActionResult> GetAll([FromQuery] OpportunityPaginationDto pagination)
        {
            try
            {
                var validator = new OpportunityPaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var query = _unitOfWork.Query<Opportunity>()
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedToPersonel)
                    .Include(o => o.CreatedByPersonel)
                    .AsQueryable();

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(o =>
                        o.Name.Contains(pagination.Search) ||
                        o.Customer.FirstName.Contains(pagination.Search) ||
                        o.Customer.LastName.Contains(pagination.Search));
                }

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Stage))
                    query = query.Where(o => o.Stage == pagination.Stage);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(o => o.CustomerId == pagination.CustomerId);

                if (pagination.AssignedToPersonelId.HasValue)
                    query = query.Where(o => o.AssignedToPersonelId == pagination.AssignedToPersonelId);

                if (pagination.StartDate.HasValue)
                    query = query.Where(o => o.ExpectedCloseDate >= pagination.StartDate);

                if (pagination.EndDate.HasValue)
                    query = query.Where(o => o.ExpectedCloseDate <= pagination.EndDate.Value.Date.AddDays(1));

                var totalCount = await query.CountAsync();
                var opportunities = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var opportunityDtos = _mapper.Map<List<OpportunityDto>>(opportunities);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Opportunity",
                    AdditionalInfo = $"Fırsat listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    Data = opportunityDtos,
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
                    RequestPath = "/api/opportunities",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/opportunities/{id}
        [HttpGet("{id}")]
       // [HasPermission("opportunity.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var opportunity = await _unitOfWork.Query<Opportunity>()
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedToPersonel)
                    .Include(o => o.CreatedByPersonel)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (opportunity == null)
                    return NotFound(new { message = "Fırsat bulunamadı" });

                var opportunityDto = _mapper.Map<OpportunityDto>(opportunity);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Opportunity",
                    EntityId = id,
                    AdditionalInfo = $"Fırsat detayı görüntülendi: {opportunity.Name}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(opportunityDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/opportunities/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/opportunities/customers
        [HttpGet("customers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _unitOfWork.Query<Customer>()
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .OrderBy(c => c.FirstName)
                    .Select(c => new { c.Id, c.FirstName, c.LastName, c.CompanyName })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/opportunities/personels
        [HttpGet("personels")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPersonels()
        {
            try
            {
                var personels = await _unitOfWork.Query<Personel>()
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.FirstName)
                    .Select(p => new { p.Id, p.FirstName, p.LastName })
                    .ToListAsync();

                return Ok(personels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/opportunities
        [HttpPost]
      //  [HasPermission("opportunity.create")]
        public async Task<IActionResult> Create([FromBody] CreateOpportunityDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Fırsat bilgileri eksik" });

                var validator = new CreateOpportunityValidator();
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

                var opportunity = _mapper.Map<Opportunity>(request);
                opportunity.CreatedAt = DateTime.UtcNow;
                opportunity.CreatedByPersonelId = currentPersonelId.Value;
                opportunity.Status = "Açık";

                await _unitOfWork.AddAsync(opportunity);
                await _unitOfWork.CompleteAsync();

                var createdOpportunity = await _unitOfWork.Query<Opportunity>()
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedToPersonel)
                    .Include(o => o.CreatedByPersonel)
                    .FirstOrDefaultAsync(o => o.Id == opportunity.Id);

                var opportunityDto = _mapper.Map<OpportunityDto>(createdOpportunity);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Opportunity",
                    EntityId = opportunity.Id,
                    AdditionalInfo = $"Yeni fırsat oluşturuldu: {opportunity.Name}, Tutar: {opportunity.Amount} TL",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Fırsat",
                    Message = $"{opportunity.Name} fırsatı oluşturuldu. Tutar: {opportunity.Amount:C2}",
                    Type = "OpportunityCreated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshOpportunities");

                return Ok(opportunityDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/opportunities",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/opportunities/{id}
        [HttpPut("{id}")]
       // [HasPermission("opportunity.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOpportunityDto request)
        {
            try
            {
                var validator = new UpdateOpportunityValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                if (request == null)
                    return BadRequest(new { message = "Fırsat bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var opportunity = await _unitOfWork.Query<Opportunity>()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (opportunity == null)
                    return NotFound(new { message = "Fırsat bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Sadece oluşturan kişi düzenleyebilir
                if (opportunity.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece fırsatı oluşturan kişi düzenleyebilir." });
                }

                var oldName = opportunity.Name;
                var oldStage = opportunity.Stage;
                var oldAmount = opportunity.Amount;

                _mapper.Map(request, opportunity);
                opportunity.UpdatedAt = DateTime.UtcNow;

                // Stage "Kapandı-Kazandı" ise status'u güncelle
                if (opportunity.Stage == "Kapandı-Kazandı" && string.IsNullOrEmpty(opportunity.ActualCloseDate.ToString()))
                {
                    opportunity.Status = "Kapandı";
                    opportunity.ActualCloseDate = DateTime.UtcNow;
                }
                else if (opportunity.Stage == "Kapandı-Kaybetti" && string.IsNullOrEmpty(opportunity.ActualCloseDate.ToString()))
                {
                    opportunity.Status = "Kapandı";
                    opportunity.ActualCloseDate = DateTime.UtcNow;
                }
                else if (opportunity.Stage != "Kapandı-Kazandı" && opportunity.Stage != "Kapandı-Kaybetti")
                {
                    opportunity.Status = "Açık";
                }

                _unitOfWork.Update(opportunity);
                await _unitOfWork.CompleteAsync();

                var updatedOpportunity = await _unitOfWork.Query<Opportunity>()
                    .Include(o => o.Customer)
                    .Include(o => o.AssignedToPersonel)
                    .Include(o => o.CreatedByPersonel)
                    .FirstOrDefaultAsync(o => o.Id == opportunity.Id);

                var opportunityDto = _mapper.Map<OpportunityDto>(updatedOpportunity);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Opportunity",
                    EntityId = id,
                    AdditionalInfo = $"Fırsat güncellendi: {oldName} -> {opportunity.Name}, Aşama: {oldStage} -> {opportunity.Stage}, Tutar: {oldAmount} -> {opportunity.Amount} TL",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Fırsat Güncellendi",
                    Message = $"{opportunity.Name} fırsatı güncellendi. Yeni aşama: {opportunity.Stage}",
                    Type = "OpportunityUpdated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshOpportunities");

                return Ok(opportunityDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/opportunities/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/opportunities/{id}
        [HttpDelete("{id}")]
      //  [HasPermission("opportunity.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var opportunity = await _unitOfWork.Query<Opportunity>()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (opportunity == null)
                    return NotFound(new { message = "Fırsat bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (opportunity.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece fırsatı oluşturan kişi silebilir." });
                }

                _unitOfWork.Delete(opportunity);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Opportunity",
                    EntityId = id,
                    AdditionalInfo = $"Fırsat silindi: {opportunity.Name}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Fırsat Silindi",
                    Message = $"{opportunity.Name} fırsatı silindi",
                    Type = "OpportunityDeleted",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshOpportunities");

                return Ok(new { message = "Fırsat başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/opportunities/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/opportunities/{id}/won (Kazanıldı)
        [HttpPost("{id}/won")]
      //  [HasPermission("opportunity.edit")]
        public async Task<IActionResult> MarkAsWon(int id)
        {
            try
            {
                var opportunity = await _unitOfWork.Query<Opportunity>()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (opportunity == null)
                    return NotFound(new { message = "Fırsat bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (opportunity.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece fırsatı oluşturan kişi işlem yapabilir." });
                }

                if (opportunity.Stage == "Kapandı-Kazandı")
                    return BadRequest(new { message = "Fırsat zaten kazanılmış." });

                opportunity.Stage = "Kapandı-Kazandı";
                opportunity.Status = "Kapandı";
                opportunity.ActualCloseDate = DateTime.UtcNow;
                opportunity.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(opportunity);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "WON",
                    EntityType = "Opportunity",
                    EntityId = id,
                    AdditionalInfo = $"Fırsat kazanıldı: {opportunity.Name}, Tutar: {opportunity.Amount} TL",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Fırsat Kazanıldı! 🎉",
                    Message = $"{opportunity.Name} fırsatı kazanıldı. Tutar: {opportunity.Amount:C2}",
                    Type = "OpportunityWon",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshOpportunities");

                return Ok(new { message = "Fırsat kazanıldı olarak işaretlendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/opportunities/{id}/lost (Kaybedildi)
        [HttpPost("{id}/lost")]
      //  [HasPermission("opportunity.edit")]
        public async Task<IActionResult> MarkAsLost(int id, [FromBody] string lostReason)
        {
            try
            {
                var opportunity = await _unitOfWork.Query<Opportunity>()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (opportunity == null)
                    return NotFound(new { message = "Fırsat bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (opportunity.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece fırsatı oluşturan kişi işlem yapabilir." });
                }

                if (opportunity.Stage == "Kapandı-Kaybetti")
                    return BadRequest(new { message = "Fırsat zaten kaybedilmiş." });

                opportunity.Stage = "Kapandı-Kaybetti";
                opportunity.Status = "Kapandı";
                opportunity.ActualCloseDate = DateTime.UtcNow;
                opportunity.LostReason = lostReason;
                opportunity.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(opportunity);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "LOST",
                    EntityType = "Opportunity",
                    EntityId = id,
                    AdditionalInfo = $"Fırsat kaybedildi: {opportunity.Name}, Sebep: {lostReason}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Fırsat Kaybedildi",
                    Message = $"{opportunity.Name} fırsatı kaybedildi.",
                    Type = "OpportunityLost",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshOpportunities");

                return Ok(new { message = "Fırsat kaybedildi olarak işaretlendi" });
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
