using AutoMapper;
using Crm.Application.DTOs.Customer;
using Crm.Application.DTOs.Lead;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.LeadValidator;
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
    public class LeadsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly INotificationService _notificationService;

        public LeadsController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IHubContext<NotificationHub> notificationHub, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _notificationHub = notificationHub;
            _notificationService = notificationService;
        }

        [HttpGet("source-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSourceList()
        {
            var sources = new List<object>
            {
                new { Value = "Web", Label = "🌐 Web Sitesi" },
                new { Value = "Referans", Label = "🤝 Referans" },
                new { Value = "Reklam", Label = "📢 Reklam" },
                new { Value = "Fuar", Label = "🏢 Fuar" },
                new { Value = "SosyalMedya", Label = "📱 Sosyal Medya" },
                new { Value = "Email", Label = "📧 Email" },
                new { Value = "Telefon", Label = "📞 Telefon" }
            };
            return Ok(sources);
        }

        [HttpGet("status-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatusList()
        {
            var statuses = new List<object>
            {
                new { Value = "Yeni", Label = "🆕 Yeni", Color = "blue" },
                new { Value = "IletisimeGecildi", Label = "📞 İletişime Geçildi", Color = "yellow" },
                new { Value = "TeklifSunuldu", Label = "📄 Teklif Sunuldu", Color = "purple" },
                new { Value = "MusteriOldu", Label = "✅ Müşteri Oldu", Color = "green" },
                new { Value = "Kaybedildi", Label = "❌ Kaybedildi", Color = "red" }
            };
            return Ok(statuses);
        }

        [HttpGet]
        //[HasPermission("lead.view")]
        public async Task<IActionResult> GetAll([FromQuery] LeadPaginationDto pagination)
        {
            try
            {
                var currentPersonelId = GetCurrentPersonelId();

                var query = _unitOfWork.Query<Lead>()
                    .Include(l => l.AssignedToPersonel)
                    .Include(l => l.ConvertedToCustomer)
                    .Include(l => l.Campaign)
                    .Where(l => l.Status != "MusteriOldu")
                    .AsQueryable();

                // Admin değilse sadece kendi lead'lerini görsün
                var isAdmin = await CheckPermissionAsync("lead.viewall");
                if (!isAdmin && currentPersonelId.HasValue)
                {
                    query = query.Where(l => l.AssignedToPersonelId == currentPersonelId.Value);
                }

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(l =>
                        l.CompanyName.Contains(pagination.Search) ||
                        l.ContactName.Contains(pagination.Search) ||
                        l.Email.Contains(pagination.Search) ||
                        l.Phone.Contains(pagination.Search));
                }

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(l => l.Status == pagination.Status);

                if (!string.IsNullOrEmpty(pagination.Source))
                    query = query.Where(l => l.Source == pagination.Source);

                if (pagination.AssignedToPersonelId.HasValue)
                    query = query.Where(l => l.AssignedToPersonelId == pagination.AssignedToPersonelId.Value);

                if (pagination.StartDate.HasValue)
                    query = query.Where(l => l.CreatedAt >= pagination.StartDate.Value);

                if (pagination.EndDate.HasValue)
                    query = query.Where(l => l.CreatedAt <= pagination.EndDate.Value);

                var totalCount = await query.CountAsync();
                var leads = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var leadDtos = _mapper.Map<List<LeadDto>>(leads);

                var response = new LeadPaginationResponse
                {
                    Data = leadDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/leads",
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
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var lead = await _unitOfWork.Query<Lead>()
                    .Include(l => l.AssignedToPersonel)
                    .Include(l => l.ConvertedToCustomer)
                    .Include(l => l.Campaign)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lead == null)
                    return NotFound(new { message = "Lead bulunamadı" });

                var leadDto = _mapper.Map<LeadDto>(lead);
                return Ok(leadDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/leads/{id}",
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
        //[HasPermission("lead.create")]
        public async Task<IActionResult> Create([FromBody] CreateLeadDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Lead bilgileri eksik" });

                var validator = new CreateLeadValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var lead = _mapper.Map<Lead>(request);
                lead.Status = "Yeni";
                lead.CreatedAt = DateTime.UtcNow;

                var currentPersonelId = GetCurrentPersonelId();
                if (currentPersonelId.HasValue && !lead.AssignedToPersonelId.HasValue)
                {
                    lead.AssignedToPersonelId = currentPersonelId.Value;
                }

                await _unitOfWork.AddAsync(lead);
                await _unitOfWork.CompleteAsync();

                var leadDto = _mapper.Map<LeadDto>(lead);

                //  SIGNALR BİLDİRİMİ (mevcut)
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "NewLead",
                        Title = "Yeni Potansiyel Müşteri",
                        Message = $"{lead.CompanyName} - {lead.ContactName} eklendi",
                        LeadId = lead.Id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                //  VERİTABANINA BİLDİRİM KAYDET (NotificationService ile)
                var notificationService = HttpContext.RequestServices.GetRequiredService<INotificationService>();

                // Atanan personele bildirim
                if (lead.AssignedToPersonelId.HasValue)
                {
                    await notificationService.SendToPersonelAsync(
                        personelId: lead.AssignedToPersonelId.Value,
                        title: "Yeni Potansiyel Müşteri",
                        message: $"{lead.CompanyName} - {lead.ContactName} size atandı",
                        type: "Lead",
                        relatedEntityId: lead.Id,
                        relatedEntityType: "Lead"
                    );
                }

                // Admin'lere bildirim
                await notificationService.SendToAdminsAsync(
                    title: "Yeni Potansiyel Müşteri",
                    message: $"{lead.CompanyName} - {lead.ContactName} eklendi",
                    type: "Lead",
                    relatedEntityId: lead.Id,
                    relatedEntityType: "Lead"
                );

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Lead",
                    EntityId = lead.Id,
                    AdditionalInfo = $"Yeni lead oluşturuldu: {lead.CompanyName} - {lead.ContactName}",
                    UserId = currentPersonelId ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(leadDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/leads",
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
        //[HasPermission("lead.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLeadDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Lead bilgileri eksik" });

                var validator = new UpdateLeadValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var lead = await _unitOfWork.GetByIdAsync<Lead>(id);
                if (lead == null)
                    return NotFound(new { message = "Lead bulunamadı" });

                var oldCompanyName = lead.CompanyName;
                var oldContactName = lead.ContactName;

                _mapper.Map(request, lead);
                lead.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(lead);
                await _unitOfWork.CompleteAsync();

                var leadDto = _mapper.Map<LeadDto>(lead);

                //  SIGNALR BİLDİRİMİ EKLE
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "LeadUpdated",
                        Title = "Lead Güncellendi",
                        Message = $"{lead.CompanyName} - {lead.ContactName} güncellendi",
                        LeadId = lead.Id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Lead",
                    EntityId = id,
                    AdditionalInfo = $"Lead güncellendi: {oldCompanyName} - {oldContactName} -> {lead.CompanyName} - {lead.ContactName}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(leadDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/leads/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        //[HasPermission("lead.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var lead = await _unitOfWork.GetByIdAsync<Lead>(id);
                if (lead == null)
                    return NotFound(new { message = "Lead bulunamadı" });

                var leadName = $"{lead.CompanyName} - {lead.ContactName}";

                _unitOfWork.Delete(lead);
                await _unitOfWork.CompleteAsync();

                // SIGNALR BİLDİRİMİ 
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "LeadDeleted",
                        Title = "Lead Silindi",
                        Message = $"{leadName} silindi",
                        LeadId = id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Lead",
                    EntityId = id,
                    AdditionalInfo = $"Lead silindi: {leadName}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Lead silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/leads/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost("{id}/convert-to-customer")]
        //[HasPermission("lead.convert")]
        public async Task<IActionResult> ConvertToCustomer(int id, [FromBody] ConvertLeadToCustomerDto request)
        {
            try
            {
                var validator = new ConvertLeadToCustomerValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var lead = await _unitOfWork.GetByIdAsync<Lead>(id);
                if (lead == null)
                    return NotFound(new { message = "Lead bulunamadı" });

                if (lead.ConvertedToCustomerId.HasValue)
                    return BadRequest(new { message = "Bu lead zaten müşteriye dönüştürülmüş" });

                // Ad soyad ayırma
                var nameParts = lead.ContactName?.Trim().Split(' ') ?? new string[0];
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                // Benzersiz cari hesap numarası oluştur
                var accountNumber = GenerateAccountNumber();

                // Müşteri oluştur
                var customer = new Customer
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    AccountNumber = accountNumber,
                    CustomerType = "Kurumsal",
                    Status = "Active",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByPersonelId = GetCurrentPersonelId(),
                    AssignedToPersonelId = lead.AssignedToPersonelId
                };

                // İsteğe bağlı alanlar
                if (!string.IsNullOrEmpty(request.TaxNumber))
                    customer.TaxNumber = request.TaxNumber;
                if (!string.IsNullOrEmpty(request.TaxOffice))
                    customer.TaxOffice = request.TaxOffice;
                if (!string.IsNullOrEmpty(request.Address))
                    customer.Address = request.Address;
                if (!string.IsNullOrEmpty(request.City))
                    customer.City = request.City;
                if (!string.IsNullOrEmpty(request.District))
                    customer.District = request.District;

                await _unitOfWork.AddAsync(customer);
                await _unitOfWork.CompleteAsync();

                // Lead'i güncelle
                lead.ConvertedToCustomerId = customer.Id;
                lead.Status = "MusteriOldu";
                lead.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(lead);
                await _unitOfWork.CompleteAsync();


                //  DÖNÜŞÜM BİLDİRİMİ (GÜVENLİ VERSİYON)
                var personelId = lead.AssignedToPersonelId ?? GetCurrentPersonelId();
                if (personelId.HasValue && personelId.Value > 0)
                {
                    await _notificationService.SendToPersonelAsync(
                        personelId: personelId.Value,
                        title: "Lead Müşteriye Dönüştürüldü",
                        message: $"{lead.CompanyName} - {lead.ContactName} artık müşteri (Cari No: {customer.AccountNumber})",
                        type: "Lead",
                        relatedEntityId: lead.Id,
                        relatedEntityType: "Lead"
                    );
                }

                // Admin'lere bildirim
                await _notificationService.SendToAdminsAsync(
                    title: "Lead Müşteriye Dönüştürüldü",
                    message: $"{lead.CompanyName} - {lead.ContactName} müşteri oldu",
                    type: "Lead",
                    relatedEntityId: lead.Id,
                    relatedEntityType: "Lead"
                );


                // SignalR bildirimi
                if (_notificationHub != null)
                {
                    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        Type = "LeadConverted",
                        Title = "Lead Müşteriye Dönüştürüldü",
                        Message = $"{lead.CompanyName} - {lead.ContactName} müşteri oldu",
                        LeadId = lead.Id,
                        CustomerId = customer.Id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CONVERT_TO_CUSTOMER",
                    EntityType = "Lead",
                    EntityId = id,
                    AdditionalInfo = $"Lead müşteriye dönüştürüldü: {lead.CompanyName} -> Müşteri No: {customer.AccountNumber}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    message = "Lead başarıyla müşteriye dönüştürüldü",
                    customerId = customer.Id,
                    customer = _mapper.Map<CustomerDto>(customer)
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/leads/{id}/convert-to-customer",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // HELPER METHODS
        private string GenerateAccountNumber()
        {
            var year = DateTime.Now.Year;
            var lastNumber = _unitOfWork.Query<Customer>()
                .Count(c => c.CreatedAt.Year == DateTime.Now.Year) + 1;
            return $"CR-{year}{lastNumber:D6}";
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

        // GET: api/leads/campaigns
        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns()
        {
            try
            {
                var campaigns = await _unitOfWork.Query<Campaign>()
                    .Where(c => !c.IsDeleted && c.Status == "Aktif")
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                return Ok(campaigns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}