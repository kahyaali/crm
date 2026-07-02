using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Order;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.OrderValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public OrdersController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IExchangeRateService exchangeRateService,INotificationService notificationService, IHubContext<NotificationHub> notificationHub)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _exchangeRateService = exchangeRateService;
            _notificationService = notificationService;
            _notificationHub = notificationHub;
        }

        [HttpGet("customer-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomerList()
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

        // ========== ORDER NUMBER GENERATOR ==========
        private async Task<string> GenerateOrderNumberAsync()
        {
            // Tüm siparişlerden en büyük numarayı bul
            var allOrders = await _unitOfWork.Query<Order>()
                .IgnoreQueryFilters()  // Silinmiş olanları da al
                .Where(o => o.OrderNumber != null && o.OrderNumber.StartsWith("ORD-"))
                .ToListAsync();

            int maxNumber = 0;
            foreach (var order in allOrders)
            {
                var numStr = order.OrderNumber.Substring(4);
                if (int.TryParse(numStr, out int num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            return $"ORD-{(maxNumber + 1):D6}";
        }

        // ========== HESAPLAMA METOTLARI ==========
        private (decimal subTotal, decimal taxAmount, decimal totalAmount) CalculateTotals(List<OrderItem> items, decimal taxRate = 0.20m)
        {
            var subTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            var taxAmount = subTotal * taxRate;
            var totalAmount = subTotal + taxAmount;
            return (subTotal, taxAmount, totalAmount);
        }

        // ========== GET ALL (PAGINATION + FILTERS) ==========
        [HttpGet]
        [HasPermission("order.view")]
        public async Task<IActionResult> GetAll([FromQuery] OrderPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Order>()
                    .Include(o => o.Customer)
                    .Include(o => o.Quote)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .AsQueryable();

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(o =>
                        o.OrderNumber.Contains(pagination.Search) ||
                        (o.Customer != null && o.Customer.FirstName.Contains(pagination.Search)) ||
                        (o.Customer != null && o.Customer.LastName.Contains(pagination.Search)) ||
                        (o.Customer != null && o.Customer.Email.Contains(pagination.Search)));
                }

                if (pagination.CustomerId.HasValue)
                {
                    query = query.Where(o => o.CustomerId == pagination.CustomerId);
                }

                if (!string.IsNullOrEmpty(pagination.Status))
                {
                    query = query.Where(o => o.Status == pagination.Status);
                }

                if (!string.IsNullOrEmpty(pagination.PaymentStatus))
                {
                    query = query.Where(o => o.PaymentStatus == pagination.PaymentStatus);
                }

                if (pagination.StartDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= pagination.StartDate);
                }

                if (pagination.EndDate.HasValue)
                {
                    var endDate = pagination.EndDate.Value.AddDays(1);
                    query = query.Where(o => o.OrderDate <= endDate);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var orderDtos = _mapper.Map<List<OrderDto>>(items);

                var response = new OrderPaginationResponse
                {
                    Data = orderDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Order",
                    AdditionalInfo = $"Sipariş listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
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
                    RequestPath = "/api/orders",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== GET BY ID ==========
        [HttpGet("{id}")]
        [HasPermission("order.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var order = await _unitOfWork.Query<Order>()
                    .Include(o => o.Customer)
                    .Include(o => o.Quote)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = "Sipariş bulunamadı" });

                var orderDto = _mapper.Map<OrderDto>(order);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Order",
                    EntityId = id,
                    AdditionalInfo = $"Sipariş detayı görüntülendi: {order.OrderNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(orderDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/orders/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== CREATE ORDER ==========
        [HttpPost]
        [HasPermission("order.create")]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Sipariş bilgileri eksik" });

                // Validasyon
                var validator = new CreateOrderValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                // Müşteri kontrolü
                var customer = await _unitOfWork.GetByIdAsync<Customer>(request.CustomerId);
                if (customer == null)
                    return BadRequest(new { message = "Müşteri bulunamadı" });

                // Ürünlerin stok kontrolü
                foreach (var item in request.Items)
                {
                    var product = await _unitOfWork.GetByIdAsync<Product>(item.ProductId);
                    if (product == null)
                        return BadRequest(new { message = $"Ürün bulunamadı (ID: {item.ProductId})" });

                    if (product.StockQuantity < item.Quantity)
                        return BadRequest(new { message = $"Yetersiz stok: {product.Name} (Mevcut: {product.StockQuantity}, İstenen: {item.Quantity})" });
                }

                var order = _mapper.Map<Order>(request);
                order.OrderNumber = await GenerateOrderNumberAsync();
                order.OrderDate = request.OrderDate;
                order.Status = request.Status ?? "Pending";
                order.PaymentStatus = request.PaymentStatus ?? "Pending";
                order.Currency = request.Currency;

                // ========== ORDER ITEMS OLUŞTUR - KUR DÖNÜŞÜMÜ YOK! ==========
                var orderItems = new List<OrderItem>();
                foreach (var item in request.Items)
                {
                    //  KUR DÖNÜŞÜMÜ YAPMA! Frontend zaten doğru fiyatı gönderiyor
                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,  // Frontend'den gelen fiyat
                        TotalPrice = item.UnitPrice * item.Quantity
                    };
                    orderItems.Add(orderItem);

                    // Stok düş
                    var product = await _unitOfWork.GetByIdAsync<Product>(item.ProductId);
                    product.StockQuantity -= item.Quantity;
                    _unitOfWork.Update(product);
                }

                order.Items = orderItems;

                // ========== TOPLAMLARI HESAPLA ==========
                var subTotal = orderItems.Sum(i => i.TotalPrice);
                var taxAmount = subTotal * request.TaxRate;
                var totalAmount = subTotal + taxAmount;

                order.SubTotal = subTotal;
                order.TaxAmount = taxAmount;
                order.TotalAmount = totalAmount;

                await _unitOfWork.AddAsync(order);
                await _unitOfWork.CompleteAsync();

                //  Order'ı yeniden yükle (Customer ilişkisi için)
                var createdOrder = await _unitOfWork.Query<Order>()
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                //  SİPARİŞ BİLDİRİMİ
                var customerName = createdOrder?.Customer != null
                    ? $"{createdOrder.Customer.FirstName} {createdOrder.Customer.LastName}"
                    : "Müşteri";

                // Admin'lere bildirim
                await _notificationService.SendToAdminsAsync(
                    title: "Yeni Sipariş",
                    message: $"#{createdOrder.OrderNumber} - {customerName} - {createdOrder.TotalAmount:C}",
                    type: "Order",
                    relatedEntityId: createdOrder.Id,
                    relatedEntityType: "Order"
                );


                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Order",
                    EntityId = order.Id,
                    AdditionalInfo = $"Yeni sipariş oluşturuldu: {order.OrderNumber} - Toplam: {order.TotalAmount} {order.Currency}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                var result = await _unitOfWork.Query<Order>()
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                return Ok(_mapper.Map<OrderDto>(result));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/orders",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }








        // ========== UPDATE ORDER ==========
        [HttpPut("{id}")]
        [HasPermission("order.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Sipariş bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "URL'deki ID ile gönderilen ID uyuşmuyor" });

                var validator = new UpdateOrderValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var order = await _unitOfWork.Query<Order>()
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = "Sipariş bulunamadı" });

                if (order.Status == "Cancelled")
                    return BadRequest(new { message = "İptal edilmiş bir sipariş güncellenemez" });


                var oldStatus = order.Status;

                // ========== 1. MEVCUT TÜM ÜRÜNLERİN STOKLARINI GERİ EKLE ==========
                foreach (var existingItem in order.Items.ToList())
                {
                    var product = await _unitOfWork.GetByIdAsync<Product>(existingItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += existingItem.Quantity;
                        _unitOfWork.Update(product);
                    }
                }

                // ========== 2. MEVCUT TÜM ORDER ITEM'LARI SİL ==========
                _unitOfWork.DeleteRange(order.Items.ToList());

                // ========== 3. YENİ ÜRÜNLERİ EKLE ==========
                var newItems = new List<OrderItem>();
                foreach (var itemDto in request.Items)
                {
                    var product = await _unitOfWork.GetByIdAsync<Product>(itemDto.ProductId);
                    if (product == null)
                        return BadRequest(new { message = $"Ürün bulunamadı (ID: {itemDto.ProductId})" });

                    if (product.StockQuantity < itemDto.Quantity)
                        return BadRequest(new { message = $"Yetersiz stok: {product.Name}" });

                    var newItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TotalPrice = itemDto.UnitPrice * itemDto.Quantity,
                        CreatedAt = DateTime.UtcNow
                    };
                    newItems.Add(newItem);

                    // Stok düş
                    product.StockQuantity -= itemDto.Quantity;
                    _unitOfWork.Update(product);
                }

                foreach (var newItem in newItems)
                {
                    await _unitOfWork.AddAsync(newItem);
                }

                // ========== 4. TOPLAMLARI HESAPLA ==========
                var subTotal = newItems.Sum(i => i.TotalPrice);
                var taxAmount = subTotal * request.TaxRate;
                var totalAmount = subTotal + taxAmount;

                order.SubTotal = subTotal;
                order.TaxAmount = taxAmount;
                order.TotalAmount = totalAmount;
                order.DeliveryDate = request.DeliveryDate;
                order.Status = request.Status ?? order.Status;
                order.PaymentStatus = request.PaymentStatus ?? order.PaymentStatus;
                order.ShippingAddress = request.ShippingAddress ?? order.ShippingAddress;
                order.Notes = request.Notes ?? order.Notes;
                order.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(order);
                await _unitOfWork.CompleteAsync();


                //  DURUM DEĞİŞTİYSE BİLDİRİM
                if (oldStatus != order.Status)
                {
                    var customerName = order.Customer != null
                        ? $"{order.Customer.FirstName} {order.Customer.LastName}"
                        : "Müşteri";

                    // Admin'lere bildirim
                    await _notificationService.SendToAdminsAsync(
                        title: "Sipariş Durumu Güncellendi",
                        message: $"#{order.OrderNumber} - {customerName} sipariş durumu: {oldStatus} → {order.Status}",
                        type: "Order",
                        relatedEntityId: order.Id,
                        relatedEntityType: "Order"
                    );

                    // SignalR bildirimi
                    if (_notificationHub != null)
                    {
                        await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
                        {
                            Type = "OrderStatusChanged",
                            Title = "Sipariş Durumu Güncellendi",
                            Message = $"#{order.OrderNumber} sipariş durumu: {oldStatus} → {order.Status}",
                            OrderId = order.Id,
                            OldStatus = oldStatus,
                            NewStatus = order.Status,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }



                var updatedOrder = await _unitOfWork.Query<Order>()
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                return Ok(_mapper.Map<OrderDto>(updatedOrder));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/orders/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== UPDATE ORDER STATUS ==========
        [HttpPatch("{id}/status")]
        [HasPermission("order.edit")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            try
            {
                var order = await _unitOfWork.GetByIdAsync<Order>(id);
                if (order == null)
                    return NotFound(new { message = "Sipariş bulunamadı" });

                var oldStatus = order.Status;
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(order);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE_STATUS",
                    EntityType = "Order",
                    EntityId = order.Id,
                    AdditionalInfo = $"Sipariş durumu güncellendi: {order.OrderNumber} - {oldStatus} -> {status}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Sipariş durumu güncellendi", status = order.Status });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/orders/{id}/status",
                    RequestMethod = "PATCH",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== DELETE ORDER (SOFT DELETE) ==========
        [HttpDelete("{id}")]
        [HasPermission("order.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _unitOfWork.Query<Order>()
                    .Include(o => o.Invoices)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = "Sipariş bulunamadı" });

                // Fatura kontrolü
                var hasInvoices = order.Invoices != null && order.Invoices.Any(i => !i.IsDeleted);
                if (hasInvoices)
                {
                    return BadRequest(new { message = "Bu siparişe bağlı faturalar var. Önce faturaları silmelisiniz." });
                }

                var orderNumber = order.OrderNumber;

                // Soft delete
                _unitOfWork.SoftDelete(order);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Order",
                    EntityId = id,
                    AdditionalInfo = $"Sipariş soft delete edildi: {orderNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Sipariş silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/orders/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== GET ORDER STATUS LIST ==========
        [HttpGet("status-list")]
        [AllowAnonymous]
        public IActionResult GetStatusList()
        {
            var statuses = new[]
            {
                new { Value = "Pending", Label = "⏳ Beklemede" },
                new { Value = "Approved", Label = "✅ Onaylandı" },
                new { Value = "Preparing", Label = "📦 Hazırlanıyor" },
                new { Value = "Shipped", Label = "🚚 Kargolandı" },
                new { Value = "Delivered", Label = "🏠 Teslim Edildi" },
                new { Value = "Cancelled", Label = "❌ İptal Edildi" }
            };
            return Ok(statuses);
        }

        // ========== GET PAYMENT STATUS LIST ==========
        [HttpGet("payment-status-list")]
        [AllowAnonymous]
        public IActionResult GetPaymentStatusList()
        {
            var statuses = new[]
            {
                new { Value = "Pending", Label = "⏳ Beklemede" },
                new { Value = "Partial", Label = "🔄 Kısmen Ödendi" },
                new { Value = "Paid", Label = "✅ Ödendi" },
                new { Value = "Cancelled", Label = "❌ İptal" }
            };
            return Ok(statuses);
        }
    }
}
