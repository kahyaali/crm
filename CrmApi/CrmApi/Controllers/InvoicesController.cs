using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Invoice;
using Crm.Application.DTOs.Payment;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;

using CrmApi.Hubs;
using CrmApi.Validators.InvoiceValidator;
using CrmApi.Validators.PaymentValidator;
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
    public class InvoicesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public InvoicesController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _hubContext = hubContext;
        }

        // InvoicesController.cs - içine ekle

        // GET: api/invoices/customers
        [HttpGet("customers")]
        [AllowAnonymous] // veya [HasPermission("invoice.view")]
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

        // GET: api/invoices/products
        [HttpGet("products")]
        [AllowAnonymous] // veya [HasPermission("invoice.view")]
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

        // GET: api/invoices
        [HttpGet]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetAll([FromQuery] InvoicePaginationDto pagination)
        {
            try
            {
                var validator = new InvoicePaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var query = _unitOfWork.Query<Invoice>()
                    .Include(i => i.Customer)
                    .Include(i => i.CreatedByPersonel)
                    .Include(i => i.Items)
                        .ThenInclude(ii => ii.Product)
                    .Include(i => i.Payments)
                    .AsQueryable();

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(i =>
                        i.InvoiceNumber.Contains(pagination.Search) ||
                        i.Customer.FirstName.Contains(pagination.Search) ||
                        i.Customer.LastName.Contains(pagination.Search));
                }

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(i => i.Status == pagination.Status);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(i => i.CustomerId == pagination.CustomerId);

                if (pagination.StartDate.HasValue)
                    query = query.Where(i => i.InvoiceDate >= pagination.StartDate);

                if (pagination.EndDate.HasValue)
                    query = query.Where(i => i.InvoiceDate <= pagination.EndDate.Value.Date.AddDays(1));

                var totalCount = await query.CountAsync();
                var invoices = await query
                    .OrderByDescending(i => i.InvoiceDate)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var invoiceDtos = _mapper.Map<List<InvoiceDto>>(invoices);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Invoice",
                    AdditionalInfo = $"Fatura listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    Data = invoiceDtos,
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
                    RequestPath = "/api/invoices",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/invoices/{id}
        [HttpGet("{id}")]
        [HasPermission("invoice.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var invoice = await _unitOfWork.Query<Invoice>()
                    .Include(i => i.Customer)
                    .Include(i => i.CreatedByPersonel)
                    .Include(i => i.Items)
                        .ThenInclude(ii => ii.Product)
                    .Include(i => i.Payments)
                        .ThenInclude(p => p.ReceivedByPersonel)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return NotFound(new { message = "Fatura bulunamadı" });

                var invoiceDto = _mapper.Map<InvoiceDto>(invoice);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Invoice",
                    EntityId = id,
                    AdditionalInfo = $"Fatura detayı görüntülendi: {invoice.InvoiceNumber}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(invoiceDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/invoices/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/invoices
        [HttpPost]
       [HasPermission("invoice.create")]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Fatura bilgileri eksik" });

                var validator = new CreateInvoiceValidator();
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

                var invoice = _mapper.Map<Invoice>(request);

                // Fatura numarası oluştur
                var year = DateTime.UtcNow.Year;
                var lastInvoice = await _unitOfWork.Query<Invoice>()
                    .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}"))
                    .OrderByDescending(i => i.Id)
                    .FirstOrDefaultAsync();

                int lastNumber = 0;
                if (lastInvoice != null)
                {
                    var lastNumberStr = lastInvoice.InvoiceNumber.Split('-')[2];
                    lastNumber = int.Parse(lastNumberStr);
                }
                invoice.InvoiceNumber = $"INV-{year}-{(lastNumber + 1).ToString("D6")}";

                invoice.CreatedAt = DateTime.UtcNow;
                invoice.CreatedByPersonelId = currentPersonelId.Value;
                invoice.PaidAmount = 0;

                // Toplam hesaplamaları
                decimal subTotal = 0;
                foreach (var item in request.Items)
                {
                    var totalPrice = item.Quantity * item.UnitPrice;
                    subTotal += totalPrice;
                }

                invoice.SubTotal = subTotal;
                invoice.TaxAmount = subTotal * (invoice.TaxRate / 100);
                invoice.TotalAmount = subTotal + invoice.TaxAmount;

                //  DURUMU OTOMATİK AYARLA
                if (invoice.PaidAmount >= invoice.TotalAmount)
                    invoice.Status = "Ödendi";
                else if (invoice.PaidAmount > 0)
                    invoice.Status = "Kısmen Ödendi";
                else
                    invoice.Status = "Gönderildi";

                await _unitOfWork.AddAsync(invoice);
                await _unitOfWork.CompleteAsync();

                // InvoiceItem'ları ekle
                foreach (var item in request.Items)
                {
                    var invoiceItem = new InvoiceItem
                    {
                        InvoiceId = invoice.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AddAsync(invoiceItem);
                }
                await _unitOfWork.CompleteAsync();

                var createdInvoice = await _unitOfWork.Query<Invoice>()
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                        .ThenInclude(ii => ii.Product)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                var invoiceDto = _mapper.Map<InvoiceDto>(createdInvoice);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Invoice",
                    EntityId = invoice.Id,
                    AdditionalInfo = $"Yeni fatura oluşturuldu: {invoice.InvoiceNumber} - Tutar: {invoice.TotalAmount} TL, Durum: {invoice.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Fatura",
                    Message = $"{invoice.InvoiceNumber} numaralı fatura oluşturuldu. Tutar: {invoice.TotalAmount:C2}, Durum: {invoice.Status}",
                    Type = "InvoiceCreated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshInvoices");

                return Ok(invoiceDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/invoices",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/invoices/{id}
        [HttpPut("{id}")]
        [HasPermission("invoice.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceDto request)
        {
            try
            {
                var validator = new UpdateInvoiceValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                if (request == null)
                    return BadRequest(new { message = "Fatura bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var invoice = await _unitOfWork.Query<Invoice>()
                    .Include(i => i.Items)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return NotFound(new { message = "Fatura bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Sadece oluşturan kişi düzenleyebilir
                if (invoice.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece faturayı oluşturan kişi düzenleyebilir." });
                }

                var oldInvoiceNumber = invoice.InvoiceNumber;
                var oldTotalAmount = invoice.TotalAmount;
                var oldStatus = invoice.Status;

                _mapper.Map(request, invoice);
                invoice.UpdatedAt = DateTime.UtcNow;

                // Toplam hesaplamaları
                decimal subTotal = 0;
                foreach (var item in request.Items)
                {
                    var totalPrice = item.Quantity * item.UnitPrice;
                    subTotal += totalPrice;
                }

                invoice.SubTotal = subTotal;
                invoice.TaxAmount = subTotal * (invoice.TaxRate / 100);
                invoice.TotalAmount = subTotal + invoice.TaxAmount;

                // Item'ları güncelle
                var existingItemIds = invoice.Items.Select(i => i.Id).ToList();
                var newItemIds = request.Items.Where(i => i.Id.HasValue).Select(i => i.Id.Value).ToList();

                // Silinenler
                var deletedItemIds = existingItemIds.Except(newItemIds);
                foreach (var itemId in deletedItemIds)
                {
                    var item = invoice.Items.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                        _unitOfWork.Delete(item);
                }

                // Eklenen/Güncellenen
                foreach (var itemDto in request.Items)
                {
                    if (itemDto.Id.HasValue)
                    {
                        // Güncelle
                        var existingItem = invoice.Items.FirstOrDefault(i => i.Id == itemDto.Id);
                        if (existingItem != null)
                        {
                            existingItem.ProductId = itemDto.ProductId;
                            existingItem.Quantity = itemDto.Quantity;
                            existingItem.UnitPrice = itemDto.UnitPrice;
                            existingItem.TotalPrice = itemDto.Quantity * itemDto.UnitPrice;
                            existingItem.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Update(existingItem);
                        }
                    }
                    else
                    {
                        // Yeni ekle
                        var newItem = new InvoiceItem
                        {
                            InvoiceId = invoice.Id,
                            ProductId = itemDto.ProductId,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
                            TotalPrice = itemDto.Quantity * itemDto.UnitPrice,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.AddAsync(newItem);
                    }
                }

                //  DURUMU OTOMATİK GÜNCELLE (Ödemeleri koruyarak)
                // Mevcut ödenen tutar (Payments'tan hesapla)
                var currentPaidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0;
                invoice.PaidAmount = currentPaidAmount;

                // Durumu belirle
                if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = "Ödendi";
                }
                else if (invoice.PaidAmount > 0 && invoice.PaidAmount < invoice.TotalAmount)
                {
                    invoice.Status = "Kısmen Ödendi";
                }
                else if (invoice.PaidAmount == 0)
                {
                    invoice.Status = "Gönderildi";
                }

                // Gecikmiş kontrolü
                if (invoice.Status != "Ödendi" && invoice.Status != "İptal")
                {
                    if (DateTime.Now > invoice.DueDate)
                        invoice.Status = "Gecikmiş";
                }

                // Eğer frontend'den İptal gönderildiyse, onu koru
                if (request.Status == "İptal")
                {
                    invoice.Status = "İptal";
                }

                _unitOfWork.Update(invoice);
                await _unitOfWork.CompleteAsync();

                var updatedInvoice = await _unitOfWork.Query<Invoice>()
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                        .ThenInclude(ii => ii.Product)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                var invoiceDto = _mapper.Map<InvoiceDto>(updatedInvoice);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Invoice",
                    EntityId = id,
                    AdditionalInfo = $"Fatura güncellendi: {oldInvoiceNumber} -> {invoice.InvoiceNumber}, Tutar: {oldTotalAmount} -> {invoice.TotalAmount} TL, Durum: {oldStatus} -> {invoice.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Fatura Güncellendi",
                    Message = $"{invoice.InvoiceNumber} numaralı fatura güncellendi. Yeni tutar: {invoice.TotalAmount:C2}, Durum: {invoice.Status}",
                    Type = "InvoiceUpdated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshInvoices");

                return Ok(invoiceDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/invoices/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/invoices/{id}/cancel
        [HttpPost("{id}/cancel")]
        [HasPermission("invoice.edit")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var invoice = await _unitOfWork.Query<Invoice>()
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return NotFound(new { message = "Fatura bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Sadece oluşturan kişi iptal edebilir
                if (invoice.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece faturayı oluşturan kişi iptal edebilir." });
                }

                // Zaten iptal veya ödendiyse iptal edilemez
                if (invoice.Status == "İptal")
                    return BadRequest(new { message = "Fatura zaten iptal edilmiş." });

                if (invoice.Status == "Ödendi")
                    return BadRequest(new { message = "Ödenmiş fatura iptal edilemez." });

                var oldStatus = invoice.Status;
                invoice.Status = "İptal";
                invoice.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(invoice);
                await _unitOfWork.CompleteAsync();

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CANCEL",
                    EntityType = "Invoice",
                    EntityId = id,
                    AdditionalInfo = $"Fatura iptal edildi: {invoice.InvoiceNumber}, Eski Durum: {oldStatus}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Fatura İptal Edildi",
                    Message = $"{invoice.InvoiceNumber} numaralı fatura iptal edildi.",
                    Type = "InvoiceCancelled",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshInvoices");

                return Ok(new { message = "Fatura başarıyla iptal edildi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        [HttpPost("{id}/add-payment")]
       [HasPermission("invoice.edit")]
        public async Task<IActionResult> AddPayment(int id, [FromBody] AddPaymentDto request)
        {
            try
            {
                var validator = new AddPaymentValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var invoice = await _unitOfWork.Query<Invoice>()
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                    return NotFound(new { message = "Fatura bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Payment numarası oluştur
                var year = DateTime.UtcNow.Year;
                var lastPayment = await _unitOfWork.Query<Payment>()
                    .Where(p => p.PaymentNumber.StartsWith($"PAY-{year}"))
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync();

                int lastNumber = 0;
                if (lastPayment != null)
                {
                    var lastNumberStr = lastPayment.PaymentNumber.Split('-')[2];
                    lastNumber = int.Parse(lastNumberStr);
                }

                var payment = new Payment
                {
                    PaymentNumber = $"PAY-{year}-{(lastNumber + 1).ToString("D6")}",
                    InvoiceId = invoice.Id,
                    PaymentDate = request.PaymentDate,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    TransactionId = request.TransactionId,
                    Notes = request.Notes,
                    ReceivedByPersonelId = currentPersonelId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AddAsync(payment);
                await _unitOfWork.CompleteAsync();

                //  Faturayı YENİDEN yükle (güncel Payments ile)
                var freshInvoice = await _unitOfWork.Query<Invoice>()
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == id);

                var oldPaidAmount = freshInvoice.PaidAmount;
                var realTotalPaid = freshInvoice.Payments.Sum(p => p.Amount);

                freshInvoice.PaidAmount = realTotalPaid;
                freshInvoice.UpdatedAt = DateTime.UtcNow;

                // Fatura durumunu güncelle
                var oldStatus = freshInvoice.Status;
                if (freshInvoice.PaidAmount >= freshInvoice.TotalAmount)
                    freshInvoice.Status = "Ödendi";
                else if (freshInvoice.PaidAmount > 0)
                    freshInvoice.Status = "Kısmen Ödendi";

                _unitOfWork.Update(freshInvoice);
                await _unitOfWork.CompleteAsync();

                var paymentDto = _mapper.Map<PaymentDto>(payment);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Payment",
                    EntityId = payment.Id,
                    AdditionalInfo = $"Faturaya ödeme eklendi: {freshInvoice.InvoiceNumber} - Tutar: {request.Amount} TL, Önceki Ödenen: {oldPaidAmount} TL, Yeni Ödenen: {freshInvoice.PaidAmount} TL, Kalan: {freshInvoice.TotalAmount - freshInvoice.PaidAmount} TL, Durum: {oldStatus} -> {freshInvoice.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Ödeme",
                    Message = $"{freshInvoice.InvoiceNumber} numaralı faturaya {request.Amount} TL ödeme yapıldı. Kalan bakiye: {(freshInvoice.TotalAmount - freshInvoice.PaidAmount):C2}",
                    Type = "PaymentAdded",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshInvoices");

                return Ok(paymentDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/invoices/{id}/add-payment",
                    RequestMethod = "POST",
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