using AutoMapper;
using ClosedXML.Excel;
using Crm.API.Attributes;
using Crm.Application.DTOs.Customer;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Services;
using CrmApi.Validators.CustomerValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDataFilterService _dataFilterService;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;

        public CustomersController(IUnitOfWork unitOfWork, IDataFilterService dataFilterService, IMapper mapper, ILogService logService,INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _dataFilterService = dataFilterService;
            _mapper = mapper;
            _logService = logService;
            _notificationService = notificationService;
        }


        [HttpGet("select-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSelectList()
        {
            var personels = await _unitOfWork.Query<Personel>()
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.FirstName)
                .Select(p => new { p.Id, p.FirstName, p.LastName })
                .ToListAsync();

            return Ok(personels);
        }

        // GET: api/customers
        [HttpGet]
        [HasPermission("customer.view")]
        public async Task<IActionResult> GetAll([FromQuery] CustomerPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Customer>()
                    .Include(c => c.AssignedToPersonel)
                    .Where(c => !c.IsDeleted)
                    .AsQueryable();

                query = await _dataFilterService.FilterCustomersByRole(query);

                // Mevcut arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(c =>
                        c.FirstName.Contains(pagination.Search) ||
                        c.LastName.Contains(pagination.Search) ||
                        c.Email.Contains(pagination.Search) ||
                        (c.CompanyName != null && c.CompanyName.Contains(pagination.Search)) ||
                        (c.AccountNumber != null && c.AccountNumber.Contains(pagination.Search)) ||
                        (c.Phone != null && c.Phone.Contains(pagination.Search)) ||
                        (c.TaxNumber != null && c.TaxNumber.Contains(pagination.Search)));
                }

                if (!string.IsNullOrEmpty(pagination.CustomerType))
                {
                    query = query.Where(c => c.CustomerType == pagination.CustomerType);
                }

                if (pagination.AssignedToPersonelId.HasValue)
                {
                    query = query.Where(c => c.AssignedToPersonelId == pagination.AssignedToPersonelId);
                }

                if (!string.IsNullOrEmpty(pagination.PaymentType))
                {
                    query = query.Where(c => c.PaymentType == pagination.PaymentType);
                }

                if (pagination.MinCreditLimit.HasValue)
                {
                    query = query.Where(c => c.CreditLimit >= pagination.MinCreditLimit);
                }

                if (pagination.MaxCreditLimit.HasValue)
                {
                    query = query.Where(c => c.CreditLimit <= pagination.MaxCreditLimit);
                }

  
                if (!string.IsNullOrEmpty(pagination.Status))
                {
                    if (pagination.Status == "Active")
                    {
                        query = query.Where(c => c.IsActive == true);
                    }
                    else if (pagination.Status == "Passive")
                    {
                        query = query.Where(c => c.IsActive == false);
                    }
                    else
                    {
                        query = query.Where(c => c.Status == pagination.Status);
                    }
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var customerDtos = _mapper.Map<List<CustomerDto>>(items);

                var response = new CustomerPaginationResponse
                {
                    Data = customerDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Customer",
                    AdditionalInfo = $"Müşteri listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/customers",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/customers/{id}
        [HttpGet("{id}")]
      //  [HasPermission("customer.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var customer = await _unitOfWork.Query<Customer>()
            .Include(c => c.AssignedToPersonel)  
            .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                    return NotFound(new { message = "Müşteri bulunamadı" });

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Customer",
                    EntityId = id,
                    AdditionalInfo = $"Müşteri detayı görüntülendi: {customer.FirstName} {customer.LastName}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<CustomerDto>(customer));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/customers/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/customers
        [HttpPost]
        [HasPermission("customer.create")]
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Müşteri bilgileri eksik" });

           
                var validator = new CreateCustomerDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                //  Email benzerlik kontrolü
                if (await _unitOfWork.AnyAsync<Customer>(c => c.Email == request.Email))
                    return BadRequest(new { message = "Bu email adresi zaten kayıtlı" });

                //  AccountNumber benzerlik kontrolü
                if (await _unitOfWork.AnyAsync<Customer>(c => c.AccountNumber == request.AccountNumber))
                    return BadRequest(new { message = "Bu cari hesap numarası zaten kullanımda" });


                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserId = !string.IsNullOrEmpty(userIdClaim) ? int.Parse(userIdClaim) : 0;
                var currentPersonel = await _unitOfWork.Query<Personel>().FirstOrDefaultAsync(p => p.UserId == currentUserId);

                var customer = _mapper.Map<Customer>(request);
                customer.CreatedAt = DateTime.UtcNow;
                customer.UserId = currentUserId;
                customer.CreatedByPersonelId = currentPersonel?.Id;

                await _unitOfWork.AddAsync(customer);
                await _unitOfWork.CompleteAsync();

                //  MÜŞTERİ BİLDİRİMİ
                await _notificationService.SendToAdminsAsync(
                    title: "Yeni Müşteri",
                    message: $"{customer.FirstName} {customer.LastName} - {(customer.CompanyName ?? "Bireysel")} kaydedildi",
                    type: "Customer",
                    relatedEntityId: customer.Id,
                    relatedEntityType: "Customer"
                );

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Customer",
                    EntityId = customer.Id,
                    AdditionalInfo = $"Yeni müşteri oluşturuldu: {customer.FirstName} {customer.LastName} (Email: {customer.Email},Cari No: {customer.AccountNumber})",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<CustomerDto>(customer));
            }
            catch (DbUpdateException ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/customers",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/customers",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/customers/{id}
        [HttpPut("{id}")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto request)
        {
            try
            {

                if (request == null)
                    return BadRequest(new { message = "Müşteri bilgileri eksik" });

                // ID eşleşmesi kontrolü
                if (id != request.Id)
                    return BadRequest(new { message = "URL'deki ID ile gönderilen ID uyuşmuyor" });

                //  FluentValidation ile validasyon
                var validator = new UpdateCustomerDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var customer = await _unitOfWork.GetByIdAsync<Customer>(id);
                if (customer == null)
                    return NotFound(new { message = "Müşteri bulunamadı" });

                var oldName = $"{customer.FirstName} {customer.LastName}";
                var oldEmail = customer.Email;
                var oldAccountNumber = customer.AccountNumber;

                //  Email kontrolü kendi hariç
                if (customer.Email != request.Email && await _unitOfWork.AnyAsync<Customer>(c => c.Email == request.Email))
                    return BadRequest(new { message = "Bu email adresi başka bir müşteri tarafından kullanılıyor" });

                // AccountNumber kontrolü kendi hariç
                if (customer.AccountNumber != request.AccountNumber &&
                    await _unitOfWork.AnyAsync<Customer>(c => c.AccountNumber == request.AccountNumber))
                    return BadRequest(new { message = "Bu cari hesap numarası başka bir müşteri tarafından kullanılıyor" });

                _mapper.Map(request, customer);
                customer.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(customer);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Customer",
                    EntityId = customer.Id,
                    AdditionalInfo = $"Müşteri güncellendi: {oldName} -> {customer.FirstName} {customer.LastName}, Email: {oldEmail} -> {customer.Email},Cari No: {oldAccountNumber} -> {customer.AccountNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<CustomerDto>(customer));
            }
            catch (DbUpdateException ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/customers/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/customers/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/customers/{id}
        [HttpDelete("{id}")]
        [HasPermission("customer.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var customer = await _unitOfWork.GetByIdAsync<Customer>(id);
                if (customer == null)
                    return NotFound(new { message = "Müşteri bulunamadı" });

                var hasOrders = await _unitOfWork.Query<Order>().AnyAsync(o => o.CustomerId == id && !o.IsDeleted);
                var hasInvoices = await _unitOfWork.Query<Invoice>().AnyAsync(i => i.CustomerId == id && !i.IsDeleted);
                var hasTickets = await _unitOfWork.Query<Ticket>().AnyAsync(t => t.CustomerId == id && !t.IsDeleted);

                if (hasOrders || hasInvoices || hasTickets)
                {
                    return BadRequest(new
                    {
                        message = "Bu müşteriye bağlı sipariş, fatura veya ticket var. Önce onları silmelisiniz."
                    });
                }

                var customerName = $"{customer.FirstName} {customer.LastName}";

                _unitOfWork.SoftDelete(customer);
                await _unitOfWork.CompleteAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Customer",
                    EntityId = id,
                    AdditionalInfo = $"Müşteri soft delete edildi: {customerName}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Müşteri silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/customers/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/customers/my-customers
        [HttpGet("my-customers")]
        public async Task<IActionResult> GetMyCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            try
            {
                var currentPersonel = await _dataFilterService.GetCurrentPersonel();
                if (currentPersonel == null)
                    return NotFound(new { message = "Personel kaydınız bulunamadı" });

                var query = _unitOfWork.Query<Customer>()
                     .Where(c => c.AssignedToPersonelId == currentPersonel.Id && !c.IsDeleted && c.IsActive)
                     .AsQueryable();
                query = await _dataFilterService.FilterCustomersByRole(query);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.FirstName.Contains(search) ||
                        c.LastName.Contains(search) ||
                        c.Email.Contains(search) ||
                        (c.CompanyName != null && c.CompanyName.Contains(search)));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var customerDtos = _mapper.Map<List<CustomerDto>>(items);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW_MY_CUSTOMERS",
                    EntityType = "Customer",
                    AdditionalInfo = $"Kendi müşteri listesi görüntülendi. Sayfa: {page}, Toplam: {totalCount}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    totalCount = totalCount,
                    customers = customerDtos,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/customers/my-customers",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        [HttpPost("{id}/activate")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var customer = await _unitOfWork.GetByIdAsync<Customer>(id);
                if (customer == null)
                    return NotFound(new { message = "Müşteri bulunamadı" });

                customer.IsActive = true;
                customer.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(customer);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Customer",
                    EntityId = id,
                    AdditionalInfo = $"{customer.FirstName} {customer.LastName} müşterisi aktif hale getirildi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),  
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Müşteri aktif hale getirildi", isActive = true });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/customers/{id}/activate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/deactivate")]
        [HasPermission("customer.edit")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var customer = await _unitOfWork.GetByIdAsync<Customer>(id);
                if (customer == null)
                    return NotFound(new { message = "Müşteri bulunamadı" });

                // Aktif siparişleri var mı kontrol et
                var hasActiveOrders = await _unitOfWork.Query<Order>()
                    .AnyAsync(o => o.CustomerId == id && o.Status != "Completed" && o.Status != "Cancelled");

                if (hasActiveOrders)
                {
                    return BadRequest(new { message = "Bu müşterinin aktif siparişleri var. Önce siparişleri tamamlayın veya iptal edin." });
                }

                customer.IsActive = false;
                customer.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(customer);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DEACTIVATE",
                    EntityType = "Customer",
                    EntityId = id,
                    AdditionalInfo = $"{customer.FirstName} {customer.LastName} müşterisi pasif hale getirildi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),  
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Müşteri pasif hale getirildi", isActive = false });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/customers/{id}/deactivate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = ex.Message });
            }
        }



        //======== Toplu Müşteri Yükleme Excell ================

        // ========== 1. EXCEL ŞABLON İNDİRME ==========
        [HttpGet("download-template")]
        [HasPermission("customer.create")]
        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Müşteri Şablonu");

                    // ===== BAŞLIK (MERGE YOK) =====
                    worksheet.Cell(1, 1).Value = "📋 MÜŞTERİ TOPLU YÜKLEME ŞABLONU";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // ===== AÇIKLAMA (MERGE YOK) =====
                    worksheet.Cell(2, 1).Value = "⚠️ Zorunlu alanlar: Cari No*, Ad*, Soyad*, Email*, Telefon*, Müşteri Tipi*";
                    worksheet.Cell(2, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Red;
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(3, 1).Value = "💡 Müşteri Tipi: Bireysel | Kurumsal | Potansiyel";
                    worksheet.Cell(3, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromArgb(255, 128, 0);
                    worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(4, 1).Value = "📌 Kurumsal tip seçildiğinde Şirket Adı, Vergi No, Vergi Dairesi ZORUNLUDUR";
                    worksheet.Cell(4, 1).Style.Font.FontSize = 9;
                    worksheet.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;
                    worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Row(4).Height = 15;

                    // ===== HEADER SATIRI =====
                    int headerRow = 5;
                    var headers = new string[]
                    {
                "Cari No*", "Ad*", "Soyad*", "Email*", "Telefon*",
                "Müşteri Tipi*", "Şirket Adı", "Vergi No", "Vergi Dairesi",
                "Vergi İdaresi", "Adres", "Şehir", "İlçe", "Posta Kodu",
                "Web Sitesi", "İlgili Kişi", "İlgili Kişi Telefon",
                "Ödeme Tipi", "Kredi Limiti", "Vade Gün", "İndirim %",
                "Teslimat Adresi", "Fatura Adresi", "Notlar", "Durum"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(headerRow, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontSize = 11;
                        cell.Style.Font.FontColor = XLColor.Black;
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 225, 230);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                        cell.Style.Border.BottomBorderColor = XLColor.Black;
                        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        worksheet.Column(i + 1).Width = 18;
                    }

                    // ===== ÖRNEK VERİ SATIRI (Bireysel) =====
                    int dataRow = headerRow + 1;

                    var sampleData = new object[]
                    {
                "CARİ-001", "Ahmet", "Yılmaz", "ahmet@example.com", "0532 123 45 67",
                "Bireysel", "", "", "",
                "", "İstanbul", "İstanbul", "Kadıköy", "34700",
                "", "", "",
                "Cash", "10000", "30", "5",
                "", "", "Bireysel müşteri", "Active"
                    };

                    for (int i = 0; i < sampleData.Length; i++)
                    {
                        var cell = worksheet.Cell(dataRow, i + 1);
                        cell.Value = sampleData[i]?.ToString() ?? "";
                        cell.Style.Font.FontColor = XLColor.Gray;
                        cell.Style.Font.Italic = true;
                        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    // ===== KURUMSAL ÖRNEK (MERGE YOK) =====
                    int infoRow = dataRow + 2;
                    worksheet.Cell(infoRow, 1).Value = "📌 Kurumsal örnek (opsiyonel):";
                    worksheet.Cell(infoRow, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(infoRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(infoRow, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                    worksheet.Cell(infoRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    int corporateExampleRow = infoRow + 1;
                    var corporateExample = new string[]
                    {
                "CARİ-002", "Ayşe", "Demir", "ayse@firma.com", "0532 234 56 78",
                "Kurumsal", "Demir Holding A.Ş.", "1234567890", "İstanbul V.D.",
                "İstanbul V.D.", "Maslak Mah. No:1", "İstanbul", "Sarıyer", "34485",
                "www.demirholding.com", "Mehmet Demir", "0532 345 67 89",
                "Credit", "50000", "60", "10",
                "Maslak Mahallesi No:1", "Maslak Mahallesi No:1", "Kurumsal müşteri", "Active"
                    };

                    for (int i = 0; i < corporateExample.Length; i++)
                    {
                        var cell = worksheet.Cell(corporateExampleRow, i + 1);
                        cell.Value = corporateExample[i];
                        cell.Style.Font.FontColor = XLColor.DarkGray;
                        cell.Style.Font.Italic = true;
                        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    // ===== VALİDASYON MESAJLARI =====
                    int validationRow = corporateExampleRow + 3;
                    var validations = new string[]
                    {
                "📌 KOLON AÇIKLAMALARI:",
                "  • (*) ile işaretli alanlar ZORUNLUDUR",
                "  • Cari No: Benzersiz olmalı (örn: CARİ-001)",
                "  • Müşteri Tipi: Bireysel, Kurumsal, Potansiyel",
                "  • Ödeme Tipi: Cash, Credit, Deferred (opsiyonel)",
                "  • Durum: Active, Passive, Pending, Lead, Lost (opsiyonel, varsayılan: Pending)",
                "  • Kurumsal tip seçildiğinde Şirket Adı, Vergi No, Vergi Dairesi ZORUNLUDUR",
                "  • Email benzersiz olmalıdır",
                "  • Telefon formatı: 0532 123 45 67 veya 5321234567",
                "  • Kredi Limiti, Vade Gün, İndirim % sayısal değer olmalıdır"
                    };

                    for (int i = 0; i < validations.Length; i++)
                    {
                        var cell = worksheet.Cell(validationRow + i, 1);
                        cell.Value = validations[i];
                        cell.Style.Font.FontSize = i == 0 ? 11 : 9;
                        cell.Style.Font.Bold = i == 0;
                        cell.Style.Font.FontColor = i == 0 ? XLColor.Black : XLColor.DarkGray;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    // ===== FOOTER =====
                    int footerRow = validationRow + validations.Length + 1;
                    var footerCell = worksheet.Cell(footerRow, 1);
                    footerCell.Value = $"Şablon oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm} | CRM Sistemi v1.0";
                    footerCell.Style.Font.FontSize = 8;
                    footerCell.Style.Font.FontColor = XLColor.Gray;
                    footerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // ===== VERİ SATIRLARINI KİLİTSİZ YAP =====
                    for (int row = dataRow; row <= 1000; row++)
                    {
                        for (int col = 1; col <= 25; col++)
                        {
                            worksheet.Cell(row, col).Style.Protection.Locked = false;
                        }
                    }

                    // ===== KORUMA =====
                    worksheet.Protect("TemplateProtection");

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var bytes = stream.ToArray();
                        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Musteri_Toplu_Yukleme_Sablonu.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/customers/download-template",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Şablon oluşturulamadı: {ex.Message}" });
            }
        }

        // ========== 2. EXCEL'DEN TOPLU MÜŞTERİ YÜKLEME ==========
        [HttpPost("upload-excel")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [HasPermission("customer.create")]
        public async Task<IActionResult> UploadExcel(IFormFile file, [FromQuery] string uploadId)
        {

            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Dosya seçilmedi" });

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                    return BadRequest(new { message = "Sadece .xlsx ve .xls dosyaları desteklenmektedir" });

                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest(new { message = "Dosya boyutu 10 MB'dan büyük olamaz" });

                var excelData = await ReadCustomerExcelFileAsync(file);

                if (excelData == null || excelData.Count == 0)
                    return BadRequest(new { message = "Excel dosyasında veri bulunamadı" });

                int totalRows = excelData.Count;
                Console.WriteLine($"📊 TOPLAM SATIR: {totalRows}");

                var progress = new Progress<CustomerBulkUploadProgressDto>(report =>
                {
                    report.UploadId = uploadId;
                    report.TotalRows = totalRows;
                    Console.WriteLine($"📊 Progress: {report.CurrentRow}/{report.TotalRows} - %{report.Percentage}");
                    _notificationService.SendUploadProgressAsync(report).Wait();
                });

                var result = await ProcessBulkCustomerImportAsync(excelData, uploadId, progress);

                await _notificationService.SendUploadProgressAsync(new CustomerBulkUploadProgressDto
                {
                    UploadId = uploadId,
                    CurrentRow = totalRows,
                    TotalRows = totalRows,
                    CurrentEmail = "Tamamlandı! 🎉",
                    Status = "Completed",
                    Percentage = 100
                });

                return Ok(new { message = "İşlem tamamlandı", result = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Excel yüklenirken hata oluştu",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // ========== EXCEL OKUMA ==========
        private async Task<List<CustomerExcelDto>> ReadCustomerExcelFileAsync(IFormFile file)
        {
            var result = new List<CustomerExcelDto>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed().ToList();

                    if (rows.Count < 6)
                        return result;

                    var headerRow = rows[4];

                    // Kolon indekslerini bul
                    int colAccountNumber = 1, colFirstName = 2, colLastName = 3, colEmail = 4, colPhone = 5;
                    int colCustomerType = 6, colCompanyName = 7, colTaxNumber = 8, colTaxOffice = 9;
                    int colTaxAdministration = 10, colAddress = 11, colCity = 12, colDistrict = 13, colPostalCode = 14;
                    int colWebsite = 15, colContactPerson = 16, colContactPersonPhone = 17;
                    int colPaymentType = 18, colCreditLimit = 19, colPaymentTermDays = 20, colDiscountRate = 21;
                    int colShippingAddress = 22, colInvoiceAddress = 23, colNotes = 24, colStatus = 25;

                    for (int i = 1; i <= 25; i++)
                    {
                        var cellValue = headerRow.Cell(i).GetString();
                        if (string.IsNullOrEmpty(cellValue)) continue;

                        if (cellValue.Contains("Cari No")) colAccountNumber = i;
                        else if (cellValue.Contains("Ad") && cellValue.Contains("*")) colFirstName = i;
                        else if (cellValue.Contains("Soyad") && cellValue.Contains("*")) colLastName = i;
                        else if (cellValue.Contains("Email") && cellValue.Contains("*")) colEmail = i;
                        else if (cellValue.Contains("Telefon") && cellValue.Contains("*")) colPhone = i;
                        else if (cellValue.Contains("Müşteri Tipi") && cellValue.Contains("*")) colCustomerType = i;
                        else if (cellValue.Contains("Şirket Adı")) colCompanyName = i;
                        else if (cellValue.Contains("Vergi No")) colTaxNumber = i;
                        else if (cellValue.Contains("Vergi Dairesi")) colTaxOffice = i;
                        else if (cellValue.Contains("Vergi İdaresi")) colTaxAdministration = i;
                        else if (cellValue.Contains("Adres")) colAddress = i;
                        else if (cellValue.Contains("Şehir")) colCity = i;
                        else if (cellValue.Contains("İlçe")) colDistrict = i;
                        else if (cellValue.Contains("Posta Kodu")) colPostalCode = i;
                        else if (cellValue.Contains("Web Sitesi")) colWebsite = i;
                        else if (cellValue.Contains("İlgili Kişi")) colContactPerson = i;
                        else if (cellValue.Contains("İlgili Kişi Telefon")) colContactPersonPhone = i;
                        else if (cellValue.Contains("Ödeme Tipi")) colPaymentType = i;
                        else if (cellValue.Contains("Kredi Limiti")) colCreditLimit = i;
                        else if (cellValue.Contains("Vade Gün")) colPaymentTermDays = i;
                        else if (cellValue.Contains("İndirim")) colDiscountRate = i;
                        else if (cellValue.Contains("Teslimat Adresi")) colShippingAddress = i;
                        else if (cellValue.Contains("Fatura Adresi")) colInvoiceAddress = i;
                        else if (cellValue.Contains("Notlar")) colNotes = i;
                        else if (cellValue.Contains("Durum")) colStatus = i;
                    }

                    // Veri satırlarını oku
                    for (int i = 5; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        var firstCell = row.Cell(1).GetString();

                        if (string.IsNullOrWhiteSpace(firstCell))
                            continue;

                        var dto = new CustomerExcelDto
                        {
                            AccountNumber = row.Cell(colAccountNumber).GetString().Trim(),
                            FirstName = row.Cell(colFirstName).GetString().Trim(),
                            LastName = row.Cell(colLastName).GetString().Trim(),
                            Email = row.Cell(colEmail).GetString().Trim(),
                            Phone = row.Cell(colPhone).GetString().Trim(),
                            CustomerType = row.Cell(colCustomerType).GetString().Trim(),
                            CompanyName = row.Cell(colCompanyName).GetString().Trim(),
                            TaxNumber = row.Cell(colTaxNumber).GetString().Trim(),
                            TaxOffice = row.Cell(colTaxOffice).GetString().Trim(),
                            TaxAdministration = row.Cell(colTaxAdministration).GetString().Trim(),
                            Address = row.Cell(colAddress).GetString().Trim(),
                            City = row.Cell(colCity).GetString().Trim(),
                            District = row.Cell(colDistrict).GetString().Trim(),
                            PostalCode = row.Cell(colPostalCode).GetString().Trim(),
                            Website = row.Cell(colWebsite).GetString().Trim(),
                            ContactPerson = row.Cell(colContactPerson).GetString().Trim(),
                            ContactPersonPhone = row.Cell(colContactPersonPhone).GetString().Trim(),
                            PaymentType = row.Cell(colPaymentType).GetString().Trim(),
                            ShippingAddress = row.Cell(colShippingAddress).GetString().Trim(),
                            InvoiceAddress = row.Cell(colInvoiceAddress).GetString().Trim(),
                            Notes = row.Cell(colNotes).GetString().Trim(),
                            Status = row.Cell(colStatus).GetString().Trim()
                        };

                        // Kredi Limiti
                        var creditLimitStr = row.Cell(colCreditLimit).GetString().Trim();
                        if (!string.IsNullOrEmpty(creditLimitStr) && decimal.TryParse(creditLimitStr, out decimal creditLimit))
                        {
                            dto.CreditLimit = creditLimit;
                        }

                        // Vade Gün
                        var paymentTermDaysStr = row.Cell(colPaymentTermDays).GetString().Trim();
                        if (!string.IsNullOrEmpty(paymentTermDaysStr) && int.TryParse(paymentTermDaysStr, out int paymentTermDays))
                        {
                            dto.PaymentTermDays = paymentTermDays;
                        }

                        // İndirim Oranı
                        var discountRateStr = row.Cell(colDiscountRate).GetString().Trim();
                        if (!string.IsNullOrEmpty(discountRateStr) && decimal.TryParse(discountRateStr, out decimal discountRate))
                        {
                            dto.DiscountRate = discountRate;
                        }

                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private async Task<CustomerBulkUploadResultDto> ProcessBulkCustomerImportAsync(
    List<CustomerExcelDto> excelData,
    string uploadId,
    IProgress<CustomerBulkUploadProgressDto> progress = null)
        {
            var result = new CustomerBulkUploadResultDto
            {
                TotalRows = excelData.Count
            };

            try
            {
                // ===== MEVCUT VERİLER =====
                var existingCustomers = await _unitOfWork.Query<Customer>().IgnoreQueryFilters().ToListAsync();
                var personels = await _unitOfWork.Query<Personel>().ToListAsync();

                var existingEmails = existingCustomers.Select(c => c.Email.ToLowerInvariant()).ToHashSet();
                var existingAccountNumbers = existingCustomers
                    .Where(c => !string.IsNullOrEmpty(c.AccountNumber))
                    .Select(c => c.AccountNumber.ToLowerInvariant())
                    .ToHashSet();

                var validCustomers = new List<CreateCustomerDto>();
                var errors = new List<CustomerBulkUploadErrorDto>();

                var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processedAccountNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < excelData.Count; i++)
                {
                    var row = excelData[i];
                    var rowNumber = i + 6;
                    var rowErrors = new List<string>();

                    // ===== ZORUNLU ALANLAR =====
                    if (string.IsNullOrWhiteSpace(row.AccountNumber))
                        rowErrors.Add("Cari No zorunludur");
                    if (string.IsNullOrWhiteSpace(row.FirstName))
                        rowErrors.Add("Ad zorunludur");
                    if (string.IsNullOrWhiteSpace(row.LastName))
                        rowErrors.Add("Soyad zorunludur");
                    if (string.IsNullOrWhiteSpace(row.Email))
                        rowErrors.Add("Email zorunludur");
                    else if (!IsValidEmail(row.Email))
                        rowErrors.Add("Geçersiz email formatı");
                    if (string.IsNullOrWhiteSpace(row.Phone))
                        rowErrors.Add("Telefon zorunludur");
                    if (string.IsNullOrWhiteSpace(row.CustomerType))
                        rowErrors.Add("Müşteri tipi zorunludur");
                    else
                    {
                        var validTypes = new[] { "Bireysel", "Kurumsal", "Potansiyel" };
                        if (!validTypes.Contains(row.CustomerType))
                            rowErrors.Add($"Müşteri tipi '{row.CustomerType}' geçersiz. Geçerli değerler: {string.Join(", ", validTypes)}");
                    }

                    // ===== BENZERSİZLİK KONTROLLERİ =====
                    if (!string.IsNullOrEmpty(row.AccountNumber))
                    {
                        var accKey = row.AccountNumber.ToLowerInvariant();
                        if (existingAccountNumbers.Contains(accKey))
                            rowErrors.Add($"Cari No '{row.AccountNumber}' sistemde zaten kayıtlı");
                        if (processedAccountNumbers.Contains(accKey))
                            rowErrors.Add($"Cari No '{row.AccountNumber}' bu dosyada tekrar ediyor");
                        else
                            processedAccountNumbers.Add(accKey);
                    }

                    if (!string.IsNullOrEmpty(row.Email))
                    {
                        var emailKey = row.Email.ToLowerInvariant();
                        if (existingEmails.Contains(emailKey))
                            rowErrors.Add($"Email '{row.Email}' sistemde zaten kayıtlı");
                        if (processedEmails.Contains(emailKey))
                            rowErrors.Add($"Email '{row.Email}' bu dosyada tekrar ediyor");
                        else
                            processedEmails.Add(emailKey);
                    }

                    // ===== KURUMSAL KONTROLLER =====
                    if (row.CustomerType == "Kurumsal")
                    {
                        if (string.IsNullOrWhiteSpace(row.CompanyName))
                            rowErrors.Add("Kurumsal müşteri için Şirket Adı zorunludur");
                        if (string.IsNullOrWhiteSpace(row.TaxNumber))
                            rowErrors.Add("Kurumsal müşteri için Vergi No zorunludur");
                        if (string.IsNullOrWhiteSpace(row.TaxOffice))
                            rowErrors.Add("Kurumsal müşteri için Vergi Dairesi zorunludur");
                    }

                    // ===== BİREYSEL KONTROLLER =====
                    if (row.CustomerType == "Bireysel")
                    {
                        if (!string.IsNullOrEmpty(row.CompanyName))
                            rowErrors.Add("Bireysel müşteri için Şirket Adı girilmemelidir");
                        if (!string.IsNullOrEmpty(row.TaxNumber))
                            rowErrors.Add("Bireysel müşteri için Vergi No girilmemelidir");
                        if (!string.IsNullOrEmpty(row.TaxOffice))
                            rowErrors.Add("Bireysel müşteri için Vergi Dairesi girilmemelidir");
                    }

                    // ===== OPSİYONEL ALAN VALİDASYONLARI =====
                    if (!string.IsNullOrEmpty(row.PaymentType))
                    {
                        var validPaymentTypes = new[] { "Cash", "Credit", "Deferred" };
                        if (!validPaymentTypes.Contains(row.PaymentType))
                            rowErrors.Add($"Ödeme tipi '{row.PaymentType}' geçersiz. Geçerli değerler: {string.Join(", ", validPaymentTypes)}");
                    }

                    if (row.CreditLimit.HasValue && row.CreditLimit < 0)
                        rowErrors.Add("Kredi limiti 0'dan küçük olamaz");

                    if (row.PaymentTermDays.HasValue && (row.PaymentTermDays < 1 || row.PaymentTermDays > 360))
                        rowErrors.Add("Vade gün sayısı 1-360 arasında olmalıdır");

                    if (row.DiscountRate.HasValue && (row.DiscountRate < 0 || row.DiscountRate > 100))
                        rowErrors.Add("İndirim oranı 0-100 arasında olmalıdır");

                    if (!string.IsNullOrEmpty(row.Status))
                    {
                        var validStatuses = new[] { "Active", "Passive", "Pending", "Lead", "Lost" };
                        if (!validStatuses.Contains(row.Status))
                            rowErrors.Add($"Durum '{row.Status}' geçersiz. Geçerli değerler: {string.Join(", ", validStatuses)}");
                    }

                    if (rowErrors.Any())
                    {
                        errors.Add(new CustomerBulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            Email = row.Email,
                            AccountNumber = row.AccountNumber,
                            ErrorMessage = string.Join(" | ", rowErrors)
                        });
                        continue;
                    }

                    // ===== VALİD MÜŞTERİ OLUŞTUR =====
                    var customerDto = new CreateCustomerDto
                    {
                        AccountNumber = row.AccountNumber.Trim(),
                        FirstName = row.FirstName.Trim(),
                        LastName = row.LastName.Trim(),
                        Email = row.Email.Trim().ToLower(),
                        Phone = row.Phone.Trim(),
                        CustomerType = row.CustomerType.Trim(),
                        CompanyName = row.CustomerType == "Kurumsal" ? row.CompanyName?.Trim() : null,
                        TaxNumber = row.CustomerType == "Kurumsal" ? row.TaxNumber?.Trim() : null,
                        TaxOffice = row.CustomerType == "Kurumsal" ? row.TaxOffice?.Trim() : null,
                        TaxAdministration = row.TaxAdministration?.Trim(),
                        Address = row.Address?.Trim(),
                        City = row.City?.Trim(),
                        District = row.District?.Trim(),
                        PostalCode = row.PostalCode?.Trim(),
                        Website = row.Website?.Trim(),
                        ContactPerson = row.ContactPerson?.Trim(),
                        ContactPersonPhone = row.ContactPersonPhone?.Trim(),
                        PaymentType = row.PaymentType?.Trim(),
                        CreditLimit = row.CreditLimit,
                        PaymentTermDays = row.PaymentTermDays,
                        DiscountRate = row.DiscountRate,
                        ShippingAddress = row.ShippingAddress?.Trim(),
                        InvoiceAddress = row.InvoiceAddress?.Trim(),
                        Notes = row.Notes?.Trim(),
                        Status = string.IsNullOrEmpty(row.Status) ? "Pending" : row.Status.Trim(),
                        IsActive = true
                    };

                    validCustomers.Add(customerDto);
                }

                result.Errors = errors;
                result.ErrorCount = errors.Count;

                // ===== KAYDET =====
                if (validCustomers.Any())
                {
                    int totalValid = validCustomers.Count;
                    int successCount = 0;
                    var createdCustomers = new List<CustomerDto>();

                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var currentUserId = !string.IsNullOrEmpty(userIdClaim) ? int.Parse(userIdClaim) : 0;
                    var currentPersonel = await _unitOfWork.Query<Personel>().FirstOrDefaultAsync(p => p.UserId == currentUserId);

                    // =====  BAŞLANGIÇ PROGRESS =====
                    progress?.Report(new CustomerBulkUploadProgressDto
                    {
                        UploadId = uploadId,
                        CurrentRow = 0,
                        TotalRows = totalValid,
                        CurrentEmail = "Başlatılıyor...",
                        Status = "Processing",
                        Percentage = 0
                    });

                    for (int index = 0; index < validCustomers.Count; index++)
                    {
                        var dto = validCustomers[index];
                        var percent = (int)((index + 1) * 100.0 / totalValid);

                        // =====  HER SATIR PROGRESS =====
                        progress?.Report(new CustomerBulkUploadProgressDto
                        {
                            UploadId = uploadId,
                            CurrentRow = index + 1,
                            TotalRows = totalValid,
                            CurrentEmail = dto.Email ?? "İşleniyor...",
                            Status = "Processing",
                            Percentage = percent
                        });

                        try
                        {
                            var customer = _mapper.Map<Customer>(dto);
                            customer.CreatedAt = DateTime.UtcNow;
                            customer.UserId = currentUserId;
                            customer.CreatedByPersonelId = currentPersonel?.Id;

                            await _unitOfWork.AddAsync(customer);
                            await _unitOfWork.CompleteAsync();

                            successCount++;
                            createdCustomers.Add(_mapper.Map<CustomerDto>(customer));
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add(new CustomerBulkUploadErrorDto
                            {
                                RowNumber = index + 1,
                                Email = dto.Email,
                                AccountNumber = dto.AccountNumber,
                                ErrorMessage = $"Kayıt hatası: {ex.InnerException?.Message ?? ex.Message}"
                            });
                        }
                    }

                    // =====  TAMAMLANDI PROGRESS =====
                    progress?.Report(new CustomerBulkUploadProgressDto
                    {
                        UploadId = uploadId,
                        CurrentRow = totalValid,
                        TotalRows = totalValid,
                        CurrentEmail = "Tamamlandı! 🎉",
                        Status = "Completed",
                        Percentage = 100
                    });

                    result.SuccessCount = successCount;
                    result.CreatedCustomers = createdCustomers;
                }
            }
            catch (Exception ex)
            {
                // =====  HATA PROGRESS =====
                progress?.Report(new CustomerBulkUploadProgressDto
                {
                    UploadId = uploadId,
                    Status = "Error",
                    CurrentEmail = ex.Message
                });

                result.Errors.Add(new CustomerBulkUploadErrorDto
                {
                    RowNumber = 0,
                    Email = "SİSTEM",
                    AccountNumber = "",
                    ErrorMessage = $"Sistem hatası: {ex.Message}"
                });
                result.ErrorCount = excelData.Count;
            }

            return result;
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

    }
}