using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Quote;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Validators.QuoteValidator;
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
    public class QuotesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public QuotesController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _hubContext = hubContext;
        }

        // GET: api/quotes/customers
        [HttpGet("customers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomers()
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
                        c.CompanyName,
                        c.Email
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/quotes/products
        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _unitOfWork.Query<Product>()
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.Name)
                    .Select(p => new {
                        p.Id,
                        p.Name,
                        p.Sku,
                        p.Price,
                        p.StockQuantity
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/quotes/opportunities
        [HttpGet("opportunities")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOpportunities()
        {
            try
            {
                var opportunities = await _unitOfWork.Query<Opportunity>()
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Name)
                    .Select(o => new {
                        o.Id,
                        o.Name,
                        o.Stage
                    })
                    .ToListAsync();

                return Ok(opportunities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/quotes
        [HttpGet]
        [HasPermission("quote.view")]
        public async Task<IActionResult> GetAll([FromQuery] QuotePaginationDto pagination)
        {
            try
            {
                var validator = new QuotePaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var query = _unitOfWork.Query<Quote>()
                    .Include(q => q.Customer)
                    .Include(q => q.Opportunity)
                    .Include(q => q.CreatedByPersonel)
                    .Include(q => q.Items)
                        .ThenInclude(qi => qi.Product)
                    .AsQueryable();

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(q =>
                        q.QuoteNumber.Contains(pagination.Search) ||
                        q.Customer.FirstName.Contains(pagination.Search) ||
                        q.Customer.LastName.Contains(pagination.Search));
                }

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(q => q.Status == pagination.Status);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(q => q.CustomerId == pagination.CustomerId);

                if (pagination.OpportunityId.HasValue)
                    query = query.Where(q => q.OpportunityId == pagination.OpportunityId);

                if (pagination.StartDate.HasValue)
                    query = query.Where(q => q.QuoteDate >= pagination.StartDate);

                if (pagination.EndDate.HasValue)
                    query = query.Where(q => q.QuoteDate <= pagination.EndDate.Value.Date.AddDays(1));

                var totalCount = await query.CountAsync();
                var quotes = await query
                    .OrderByDescending(q => q.QuoteDate)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var quoteDtos = _mapper.Map<List<QuoteDto>>(quotes);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Quote",
                    AdditionalInfo = $"Teklif listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    Data = quoteDtos,
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
                    RequestPath = "/api/quotes",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/quotes/{id}
        [HttpGet("{id}")]
        [HasPermission("quote.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var quote = await _unitOfWork.Query<Quote>()
                    .Include(q => q.Customer)
                    .Include(q => q.Opportunity)
                    .Include(q => q.CreatedByPersonel)
                    .Include(q => q.Items)
                        .ThenInclude(qi => qi.Product)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quote == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                var quoteDto = _mapper.Map<QuoteDto>(quote);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Quote",
                    EntityId = id,
                    AdditionalInfo = $"Teklif detayı görüntülendi: {quote.QuoteNumber}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(quoteDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/quotes/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/quotes
        [HttpPost]
        [HasPermission("quote.create")]
        public async Task<IActionResult> Create([FromBody] CreateQuoteDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Teklif bilgileri eksik" });

                var validator = new CreateQuoteValidator();
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

                var quote = _mapper.Map<Quote>(request);

                // Teklif numarası oluştur
                var year = DateTime.UtcNow.Year;
                var lastQuote = await _unitOfWork.Query<Quote>()
                    .Where(q => q.QuoteNumber.StartsWith($"QTE-{year}"))
                    .OrderByDescending(q => q.Id)
                    .FirstOrDefaultAsync();

                int lastNumber = 0;
                if (lastQuote != null)
                {
                    var lastNumberStr = lastQuote.QuoteNumber.Split('-')[2];
                    lastNumber = int.Parse(lastNumberStr);
                }
                quote.QuoteNumber = $"QTE-{year}-{(lastNumber + 1).ToString("D6")}";

                quote.CreatedAt = DateTime.UtcNow;
                quote.CreatedByPersonelId = currentPersonelId.Value;

                // Toplam hesaplamaları
                decimal subTotal = 0;
                foreach (var item in request.Items)
                {
                    var totalPrice = item.Quantity * item.UnitPrice;
                    subTotal += totalPrice;
                }

                quote.SubTotal = subTotal;
                quote.TaxAmount = subTotal * (quote.TaxRate / 100);
                quote.TotalAmount = subTotal + quote.TaxAmount;

                await _unitOfWork.AddAsync(quote);
                await _unitOfWork.CompleteAsync();

                // QuoteItem'ları ekle
                foreach (var item in request.Items)
                {
                    var quoteItem = new QuoteItem
                    {
                        QuoteId = quote.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice,
                        Description = item.Description,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AddAsync(quoteItem);
                }
                await _unitOfWork.CompleteAsync();

                var createdQuote = await _unitOfWork.Query<Quote>()
                    .Include(q => q.Customer)
                    .Include(q => q.Items)
                        .ThenInclude(qi => qi.Product)
                    .FirstOrDefaultAsync(q => q.Id == quote.Id);

                var quoteDto = _mapper.Map<QuoteDto>(createdQuote);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Quote",
                    EntityId = quote.Id,
                    AdditionalInfo = $"Yeni teklif oluşturuldu: {quote.QuoteNumber} - Tutar: {quote.TotalAmount} TL",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Teklif",
                    Message = $"{quote.QuoteNumber} numaralı teklif oluşturuldu. Tutar: {quote.TotalAmount:C2}",
                    Type = "QuoteCreated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshQuotes");

                return Ok(quoteDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/quotes",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/quotes/{id}
        [HttpPut("{id}")]
        [HasPermission("quote.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQuoteDto request)
        {
            try
            {
                var validator = new UpdateQuoteValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                if (request == null)
                    return BadRequest(new { message = "Teklif bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var quote = await _unitOfWork.Query<Quote>()
                    .Include(q => q.Items)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quote == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Sadece oluşturan kişi düzenleyebilir
                if (quote.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece teklifi oluşturan kişi düzenleyebilir." });
                }

                var oldQuoteNumber = quote.QuoteNumber;
                var oldTotalAmount = quote.TotalAmount;
                var oldStatus = quote.Status;

                _mapper.Map(request, quote);
                quote.UpdatedAt = DateTime.UtcNow;

                // Toplam hesaplamaları
                decimal subTotal = 0;
                foreach (var item in request.Items)
                {
                    var totalPrice = item.Quantity * item.UnitPrice;
                    subTotal += totalPrice;
                }

                quote.SubTotal = subTotal;
                quote.TaxAmount = subTotal * (quote.TaxRate / 100);
                quote.TotalAmount = subTotal + quote.TaxAmount;

                // Item'ları güncelle
                var existingItemIds = quote.Items.Select(i => i.Id).ToList();
                var newItemIds = request.Items.Where(i => i.Id.HasValue).Select(i => i.Id.Value).ToList();

                // Silinenler
                var deletedItemIds = existingItemIds.Except(newItemIds);
                foreach (var itemId in deletedItemIds)
                {
                    var item = quote.Items.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                        _unitOfWork.Delete(item);
                }

                // Eklenen/Güncellenen
                foreach (var itemDto in request.Items)
                {
                    if (itemDto.Id.HasValue)
                    {
                        // Güncelle
                        var existingItem = quote.Items.FirstOrDefault(i => i.Id == itemDto.Id);
                        if (existingItem != null)
                        {
                            existingItem.ProductId = itemDto.ProductId;
                            existingItem.Quantity = itemDto.Quantity;
                            existingItem.UnitPrice = itemDto.UnitPrice;
                            existingItem.TotalPrice = itemDto.Quantity * itemDto.UnitPrice;
                            existingItem.Description = itemDto.Description;
                            existingItem.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Update(existingItem);
                        }
                    }
                    else
                    {
                        // Yeni ekle
                        var newItem = new QuoteItem
                        {
                            QuoteId = quote.Id,
                            ProductId = itemDto.ProductId,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
                            TotalPrice = itemDto.Quantity * itemDto.UnitPrice,
                            Description = itemDto.Description,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.AddAsync(newItem);
                    }
                }

                _unitOfWork.Update(quote);
                await _unitOfWork.CompleteAsync();

                var updatedQuote = await _unitOfWork.Query<Quote>()
                    .Include(q => q.Customer)
                    .Include(q => q.Items)
                        .ThenInclude(qi => qi.Product)
                    .FirstOrDefaultAsync(q => q.Id == quote.Id);

                var quoteDto = _mapper.Map<QuoteDto>(updatedQuote);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Quote",
                    EntityId = id,
                    AdditionalInfo = $"Teklif güncellendi: {oldQuoteNumber} -> {quote.QuoteNumber}, Tutar: {oldTotalAmount} -> {quote.TotalAmount} TL, Durum: {oldStatus} -> {quote.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Teklif Güncellendi",
                    Message = $"{quote.QuoteNumber} numaralı teklif güncellendi. Yeni tutar: {quote.TotalAmount:C2}",
                    Type = "QuoteUpdated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshQuotes");

                return Ok(quoteDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/quotes/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/quotes/{id}
        [HttpDelete("{id}")]
        [HasPermission("quote.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var quote = await _unitOfWork.Query<Quote>()
                    .Include(q => q.Items)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quote == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Sadece oluşturan kişi silebilir
                if (quote.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece teklifi oluşturan kişi silebilir." });
                }

                _unitOfWork.Delete(quote);
                await _unitOfWork.CompleteAsync();

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Quote",
                    EntityId = id,
                    AdditionalInfo = $"Teklif silindi: {quote.QuoteNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Teklif Silindi",
                    Message = $"{quote.QuoteNumber} numaralı teklif silindi",
                    Type = "QuoteDeleted",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshQuotes");

                return Ok(new { message = "Teklif başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/quotes/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/quotes/{id}/approve (Onayla)
        [HttpPost("{id}/approve")]
        [HasPermission("quote.edit")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var quote = await _unitOfWork.Query<Quote>()
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quote == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (quote.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece teklifi oluşturan kişi onaylayabilir." });
                }

                if (quote.Status == "Onaylandı")
                    return BadRequest(new { message = "Teklif zaten onaylanmış." });

                if (quote.Status == "Reddedildi" || quote.Status == "İptal")
                    return BadRequest(new { message = "Reddedilmiş veya iptal edilmiş teklif onaylanamaz." });

                quote.Status = "Onaylandı";
                quote.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(quote);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "APPROVE",
                    EntityType = "Quote",
                    EntityId = id,
                    AdditionalInfo = $"Teklif onaylandı: {quote.QuoteNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Teklif Onaylandı",
                    Message = $"{quote.QuoteNumber} numaralı teklif onaylandı.",
                    Type = "QuoteApproved",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshQuotes");

                return Ok(new { message = "Teklif başarıyla onaylandı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/quotes/{id}/reject (Reddet)
        [HttpPost("{id}/reject")]
        [HasPermission("quote.edit")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var quote = await _unitOfWork.Query<Quote>()
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quote == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (quote.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece teklifi oluşturan kişi reddedebilir." });
                }

                if (quote.Status == "Reddedildi")
                    return BadRequest(new { message = "Teklif zaten reddedilmiş." });

                if (quote.Status == "Onaylandı" || quote.Status == "İptal")
                    return BadRequest(new { message = "Onaylanmış veya iptal edilmiş teklif reddedilemez." });

                quote.Status = "Reddedildi";
                quote.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(quote);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "REJECT",
                    EntityType = "Quote",
                    EntityId = id,
                    AdditionalInfo = $"Teklif reddedildi: {quote.QuoteNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Teklif Reddedildi",
                    Message = $"{quote.QuoteNumber} numaralı teklif reddedildi.",
                    Type = "QuoteRejected",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshQuotes");

                return Ok(new { message = "Teklif başarıyla reddedildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/quotes/{id}/cancel (İptal)
        [HttpPost("{id}/cancel")]
        [HasPermission("quote.edit")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var quote = await _unitOfWork.Query<Quote>()
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quote == null)
                    return NotFound(new { message = "Teklif bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (quote.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece teklifi oluşturan kişi iptal edebilir." });
                }

                if (quote.Status == "İptal")
                    return BadRequest(new { message = "Teklif zaten iptal edilmiş." });

                quote.Status = "İptal";
                quote.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(quote);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CANCEL",
                    EntityType = "Quote",
                    EntityId = id,
                    AdditionalInfo = $"Teklif iptal edildi: {quote.QuoteNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Teklif İptal Edildi",
                    Message = $"{quote.QuoteNumber} numaralı teklif iptal edildi.",
                    Type = "QuoteCancelled",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshQuotes");

                return Ok(new { message = "Teklif başarıyla iptal edildi" });
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
