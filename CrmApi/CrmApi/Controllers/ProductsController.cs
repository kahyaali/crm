using AutoMapper;
using ClosedXML.Excel;
using Crm.API.Attributes;
using Crm.Application.DTOs.Product;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Hubs;
using CrmApi.Services;
using CrmApi.Validators.ProductValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ProductsController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, INotificationService notificationService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _notificationService = notificationService;  
            _hubContext = hubContext;
        }

        // GET: api/products
        [HttpGet]
        [HasPermission("product.view")]
        public async Task<IActionResult> GetAll([FromQuery] ProductPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Product>()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => !p.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(p =>
                        p.Name.Contains(pagination.Search) ||
                        (p.Sku != null && p.Sku.Contains(pagination.Search)) ||
                        (p.Barcode != null && p.Barcode.Contains(pagination.Search)));
                }

                if (pagination.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == pagination.CategoryId);
                }

                //  Brand filtresi 
                if (pagination.BrandId.HasValue)
                {
                    query = query.Where(p => p.BrandId == pagination.BrandId);
                }

            

                if (pagination.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == pagination.IsActive);
                }

                if (pagination.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= pagination.MinPrice);
                }
                if (pagination.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= pagination.MaxPrice);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var productDtos = _mapper.Map<List<ProductDto>>(items);

                var response = new ProductPaginationResponse
                {
                    Data = productDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Product",
                    AdditionalInfo = $"Ürün listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/products",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        [HasPermission("product.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var product = await _unitOfWork.Query<Product>()
            .Include(p => p.Category)  
            .Include(p => p.Brand)      
            .FirstOrDefaultAsync(p => p.Id == id);


                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Product",
                    EntityId = id,
                    AdditionalInfo = $"Ürün detayı görüntülendi: {product.Name} (SKU: {product.Sku})",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<ProductDto>(product));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/products
        [HttpPost]
        [HasPermission("product.create")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Ürün bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new CreateProductDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                //  SADECE VERİTABANI KONTROLÜ (manuel)
                if (!string.IsNullOrEmpty(request.Sku) && await _unitOfWork.AnyAsync<Product>(p => p.Sku == request.Sku))
                    return BadRequest(new { message = "Bu SKU kodu zaten mevcut" });

                var product = _mapper.Map<Product>(request);
                product.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.AddAsync(product);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Product",
                    EntityId = product.Id,
                    AdditionalInfo = $"Yeni ürün oluşturuldu: {product.Name} (SKU: {product.Sku}, Fiyat: {product.Price} {product.Currency})",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<ProductDto>(product));
            }
            catch (DbUpdateException ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/products",
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
                    RequestPath = "/api/products",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }



        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Ürün bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new UpdateProductDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var product = await _unitOfWork.GetByIdAsync<Product>(id);
                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                var oldName = product.Name;
                var oldPrice = product.Price;

                //  SADECE VERİTABANI KONTROLÜ (manuel)
                if (!string.IsNullOrEmpty(request.Sku) && product.Sku != request.Sku && await _unitOfWork.AnyAsync<Product>(p => p.Sku == request.Sku))
                    return BadRequest(new { message = "Bu SKU kodu zaten mevcut" });

                _mapper.Map(request, product);
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(product);
                await _unitOfWork.CompleteAsync();

                var updatedProduct = await _unitOfWork.Query<Product>()
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Product",
                    EntityId = product.Id,
                    AdditionalInfo = $"Ürün güncellendi: {oldName} -> {product.Name}, Fiyat: {oldPrice} -> {product.Price}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<ProductDto>(updatedProduct));
            }
            catch (DbUpdateException ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}",
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
                    RequestPath = $"/api/products/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [HasPermission("product.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _unitOfWork.GetByIdAsync<Product>(id);
                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                // İlişki kontrolü
                var hasOrderItems = await _unitOfWork.Query<OrderItem>()
                    .AnyAsync(oi => oi.ProductId == id && !oi.IsDeleted);

                var productName = product.Name;

                if (hasOrderItems)
                {
                    // İlişki varsa SADECE pasif yap (soft delete)
                    product.IsActive = false;
                    product.IsDeleted = true;
                    product.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.Update(product);
                    await _unitOfWork.CompleteAsync();

                    // ACTION LOG - Soft Delete
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "SOFT_DELETE",
                        EntityType = "Product",
                        EntityId = id,
                        AdditionalInfo = $"Ürün pasif hale getirildi: {productName} (siparişlerde kullanıldığı için)",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });

                    return Ok(new
                    {
                        message = "Ürün siparişlerde kullanıldığı için pasif hale getirildi",
                        isSoftDeleted = true
                    });
                }
                else
                {
                    //  İlişki yoksa TAMAMEN sil (hard delete)
                    _unitOfWork.Delete(product);
                    await _unitOfWork.CompleteAsync();

                    // ACTION LOG - Hard Delete
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "HARD_DELETE",
                        EntityType = "Product",
                        EntityId = id,
                        AdditionalInfo = $"Ürün tamamen silindi: {productName}",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });

                    return Ok(new
                    {
                        message = "Ürün tamamen silindi",
                        isHardDeleted = true
                    });
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost("{id}/deactivate")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var product = await _unitOfWork.GetByIdAsync<Product>(id);
                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                // İlişki kontrolü
                var hasOrderItems = await _unitOfWork.Query<OrderItem>()
                    .AnyAsync(oi => oi.ProductId == id && !oi.IsDeleted);

                if (hasOrderItems)
                {
                    return BadRequest(new
                    {
                        message = "Bu ürün siparişlerde kullanıldığı için pasif yapılamaz. Önce siparişlerden kaldırılmalıdır."
                    });
                }

                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(product);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DEACTIVATE",
                    EntityType = "Product",
                    EntityId = id,
                    AdditionalInfo = $"Ürün pasif hale getirildi: {product.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Ürün pasif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}/deactivate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost("{id}/activate")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var product = await _unitOfWork.GetByIdAsync<Product>(id);
                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                product.IsActive = true;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(product);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Product",
                    EntityId = id,
                    AdditionalInfo = $"Ürün aktif hale getirildi: {product.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Ürün aktif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}/activate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/products/categories-list
        [HttpGet("categories-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoriesList()
        {
            var categories = await _unitOfWork.Query<ProductCategory>()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/products/brands-list
        [HttpGet("brands-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBrandsList()
        {
            var brands = await _unitOfWork.Query<Brand>()  
                .Where(b => !b.IsDeleted && b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();

            return Ok(brands);
        }

        // ========== ÜRÜN RESMİ YÜKLEME ==========
        // POST: api/products/{id}/image
        [HttpPost("{id}/image")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            try
            {
                var product = await _unitOfWork.GetByIdAsync<Product>(id);
                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Dosya seçilmedi" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Sadece resim dosyaları yüklenebilir" });

                if (file.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Dosya boyutu 2MB'dan küçük olmalıdır" });

                var fileName = $"product_{product.Id}_{DateTime.Now.Ticks}{extension}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Products");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var imageUrl = $"/Products/{fileName}";

                // Eski resmi sil
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    try
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Eski resim silinemedi: {ex.Message}");
                    }
                }

                product.ImageUrl = imageUrl;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(product);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPLOAD_IMAGE",
                    EntityType = "Product",
                    EntityId = product.Id,
                    AdditionalInfo = $"{product.Name} ürününün resmi güncellendi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { imageUrl = imageUrl, message = "Resim başarıyla yüklendi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}/image",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Dosya yüklenirken hata oluştu: {ex.Message}" });
            }
        }

        // ========== ÜRÜN RESMİ SİLME ==========
        // DELETE: api/products/{id}/image
        [HttpDelete("{id}/image")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                var product = await _unitOfWork.GetByIdAsync<Product>(id);
                if (product == null)
                    return NotFound(new { message = "Ürün bulunamadı" });

                if (string.IsNullOrEmpty(product.ImageUrl))
                    return BadRequest(new { message = "Silinecek resim bulunamadı" });

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                var oldImageUrl = product.ImageUrl;
                product.ImageUrl = null;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(product);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE_IMAGE",
                    EntityType = "Product",
                    EntityId = product.Id,
                    AdditionalInfo = $"{product.Name} ürününün resmi silindi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Resim başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/products/{id}/image",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Resim silinirken hata oluştu: {ex.Message}" });
            }
        }


        //=========== Excel Toplu Ürün Ekleme ===================

        // ========== 1. EXCEL ŞABLON İNDİRME ==========
        [HttpGet("download-template")]
        [HasPermission("product.create")]
        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Ürün Şablonu");

                    // ===== BAŞLIK =====
                    worksheet.Cell(1, 1).Value = "📋 ÜRÜN TOPLU YÜKLEME ŞABLONU";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // ===== AÇIKLAMA =====
                    worksheet.Cell(2, 1).Value = "⚠️ Zorunlu alanlar: Ürün Adı*, Fiyat*, Stok Miktarı*";
                    worksheet.Cell(2, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Red;
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(3, 1).Value = "💡 Kategori ve Marka isimleri sistemdeki isimlerle BİREBİR aynı olmalıdır";
                    worksheet.Cell(3, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromArgb(255, 128, 0);
                    worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(4, 1).Value = "📌 Para Birimi: TRY, USD, EUR, GBP | Durum: Aktif, Pasif";
                    worksheet.Cell(4, 1).Style.Font.FontSize = 9;
                    worksheet.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;
                    worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Row(4).Height = 15;

                    // ===== HEADER SATIRI =====
                    int headerRow = 5;
                    var headers = new string[]
                    {
                "Ürün Adı*", "SKU", "Barkod", "Açıklama",
                "Fiyat*", "Para Birimi", "Stok Miktarı*", "Min. Stok",
                "Max. Stok", "Kategori", "Marka", "Birim",
                "Durum", "Stok Takibi"
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
                        worksheet.Column(i + 1).Width = 20;
                    }

                    // ===== ÖRNEK VERİ SATIRI =====
                    int dataRow = headerRow + 1;
                    var sampleData = new string[]
                    {
                "Laptop", "LAP-001", "1234567890", "16GB RAM, 512GB SSD",
                "15000", "TRY", "50", "5", "100",
                "Elektronik", "Apple", "Adet", "Aktif", "Evet"
                    };

                    for (int i = 0; i < sampleData.Length; i++)
                    {
                        var cell = worksheet.Cell(dataRow, i + 1);
                        cell.Value = sampleData[i];
                        cell.Style.Font.FontColor = XLColor.Gray;
                        cell.Style.Font.Italic = true;
                        cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }

                    // ===== VALİDASYON MESAJLARI =====
                    int validationRow = dataRow + 2;
                    var validations = new string[]
                    {
                "📌 KOLON AÇIKLAMALARI:",
                "• Ürün Adı, Fiyat, Stok Miktarı ZORUNLUDUR",
                "• SKU ve Barkod benzersiz olmalıdır, boş geçilebilir",
                "• Kategori ve Marka sistemdeki isimlerle birebir aynı olmalı",
                "• Para Birimi: TRY, USD, EUR, GBP (varsayılan: TRY)",
                "• Birim: Adet, Kg, Litre, Metre (varsayılan: Adet)",
                "• Durum: Aktif, Pasif (varsayılan: Aktif)",
                "• Stok Takibi: Evet, Hayır (varsayılan: Evet)"
                    };

                    for (int i = 0; i < validations.Length; i++)
                    {
                        var cell = worksheet.Cell(validationRow + i, 1);
                        cell.Value = validations[i];
                        cell.Style.Font.FontSize = i == 0 ? 11 : 9;
                        cell.Style.Font.Bold = i == 0;
                        cell.Style.Font.FontColor = i == 0 ? XLColor.Black : XLColor.DarkGray;
                    }

                    // ===== FOOTER =====
                    int footerRow = validationRow + validations.Length + 1;
                    var footerCell = worksheet.Cell(footerRow, 1);
                    footerCell.Value = $"Şablon oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm} | CRM Sistemi v1.0";
                    footerCell.Style.Font.FontSize = 8;
                    footerCell.Style.Font.FontColor = XLColor.Gray;

                    // ===== SADECE VERİ SATIRLARINI KİLİTSİZ YAP =====
                    for (int row = dataRow; row <= 1000; row++)
                    {
                        for (int cellCol = 1; cellCol <= 14; cellCol++)
                        {
                            worksheet.Cell(row, cellCol).Style.Protection.Locked = false;
                        }
                    }

                    worksheet.Protect("TemplateProtection");

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var bytes = stream.ToArray();
                        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Urun_Toplu_Yukleme_Sablonu.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/products/download-template",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Şablon oluşturulamadı: {ex.Message}" });
            }
        }

        // ========== 2. EXCEL'DEN TOPLU ÜRÜN YÜKLEME ==========
        [HttpPost("upload-excel")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [HasPermission("product.create")]
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

                var excelData = await ReadProductExcelFileAsync(file);

                if (excelData == null || excelData.Count == 0)
                    return BadRequest(new { message = "Excel dosyasında veri bulunamadı" });

                int totalRows = excelData.Count;
                Console.WriteLine($"📊 TOPLAM SATIR: {totalRows}");

                var progress = new Progress<ProductBulkUploadProgressDto>(report =>
                {
                    report.UploadId = uploadId;
                    report.TotalRows = totalRows;
                    Console.WriteLine($"📊 Progress: {report.CurrentRow}/{report.TotalRows} - %{report.Percentage}");
                    _notificationService.SendUploadProgressAsync(report).Wait();
                });

                var result = await ProcessBulkProductImportAsync(excelData, uploadId, progress);

                await _notificationService.SendUploadProgressAsync(new ProductBulkUploadProgressDto
                {
                    UploadId = uploadId,
                    CurrentRow = totalRows,
                    TotalRows = totalRows,
                    CurrentName = "Tamamlandı! 🎉",
                    Status = "Completed",
                    Percentage = 100
                });

                return Ok(new { message = "İşlem tamamlandı", result = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: {ex.Message}");

                await _notificationService.SendUploadProgressAsync(new ProductBulkUploadProgressDto
                {
                    UploadId = uploadId,
                    Status = "Error",
                    CurrentName = ex.Message
                });

                return StatusCode(500, new
                {
                    error = "Excel yüklenirken hata oluştu",
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        private async Task<List<ProductExcelDto>> ReadProductExcelFileAsync(IFormFile file)
        {
            var result = new List<ProductExcelDto>();

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
                    int colName = 1, colSku = 2, colBarcode = 3, colDescription = 4;
                    int colPrice = 5, colCurrency = 6, colStockQuantity = 7, colMinStock = 8;
                    int colMaxStock = 9, colCategory = 10, colBrand = 11, colUnit = 12;
                    int colStatus = 13, colStockTrackable = 14;

                    for (int i = 1; i <= 14; i++)
                    {
                        var cellValue = headerRow.Cell(i).GetString();
                        if (string.IsNullOrEmpty(cellValue)) continue;

                        if (cellValue.Contains("Ürün Adı") && cellValue.Contains("*")) colName = i;
                        else if (cellValue.Contains("SKU")) colSku = i;
                        else if (cellValue.Contains("Barkod")) colBarcode = i;
                        else if (cellValue.Contains("Açıklama")) colDescription = i;
                        else if (cellValue.Contains("Fiyat") && cellValue.Contains("*")) colPrice = i;
                        else if (cellValue.Contains("Para Birimi")) colCurrency = i;
                        else if (cellValue.Contains("Stok Miktarı") && cellValue.Contains("*")) colStockQuantity = i;
                        else if (cellValue.Contains("Min. Stok")) colMinStock = i;
                        else if (cellValue.Contains("Max. Stok")) colMaxStock = i;
                        else if (cellValue.Contains("Kategori")) colCategory = i;
                        else if (cellValue.Contains("Marka")) colBrand = i;
                        else if (cellValue.Contains("Birim")) colUnit = i;
                        else if (cellValue.Contains("Durum")) colStatus = i;
                        else if (cellValue.Contains("Stok Takibi")) colStockTrackable = i;
                    }

                    // Veri satırlarını oku
                    for (int i = 5; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        var firstCell = row.Cell(1).GetString();

                        if (string.IsNullOrWhiteSpace(firstCell))
                            continue;

                        var dto = new ProductExcelDto
                        {
                            Name = row.Cell(colName).GetString().Trim(),
                            Sku = row.Cell(colSku).GetString().Trim(),
                            Barcode = row.Cell(colBarcode).GetString().Trim(),
                            Description = row.Cell(colDescription).GetString().Trim(),
                            Currency = row.Cell(colCurrency).GetString().Trim(),
                            CategoryName = row.Cell(colCategory).GetString().Trim(),
                            BrandName = row.Cell(colBrand).GetString().Trim(),
                            Unit = row.Cell(colUnit).GetString().Trim(),
                            IsActive = row.Cell(colStatus).GetString().Trim(),
                            IsStockTrackable = row.Cell(colStockTrackable).GetString().Trim()
                        };

                        // Fiyat
                        var priceStr = row.Cell(colPrice).GetString().Trim();
                        if (!string.IsNullOrEmpty(priceStr) && decimal.TryParse(priceStr, out decimal price))
                        {
                            dto.Price = price;
                        }

                        // Stok Miktarı
                        var stockStr = row.Cell(colStockQuantity).GetString().Trim();
                        if (!string.IsNullOrEmpty(stockStr) && int.TryParse(stockStr, out int stock))
                        {
                            dto.StockQuantity = stock;
                        }

                        // Min Stok
                        var minStockStr = row.Cell(colMinStock).GetString().Trim();
                        if (!string.IsNullOrEmpty(minStockStr) && int.TryParse(minStockStr, out int minStock))
                        {
                            dto.MinStockLevel = minStock;
                        }

                        // Max Stok
                        var maxStockStr = row.Cell(colMaxStock).GetString().Trim();
                        if (!string.IsNullOrEmpty(maxStockStr) && int.TryParse(maxStockStr, out int maxStock))
                        {
                            dto.MaxStockLevel = maxStock;
                        }

                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private async Task<ProductBulkUploadResultDto> ProcessBulkProductImportAsync(
    List<ProductExcelDto> excelData,
    string uploadId,
    IProgress<ProductBulkUploadProgressDto> progress = null)
        {
            var result = new ProductBulkUploadResultDto
            {
                TotalRows = excelData.Count
            };

            try
            {
                // ===== MEVCUT VERİLER =====
                var categories = await _unitOfWork.Query<ProductCategory>().ToListAsync();
                var brands = await _unitOfWork.Query<Brand>().ToListAsync();
                var existingProducts = await _unitOfWork.Query<Product>().IgnoreQueryFilters().ToListAsync();

                var existingSkus = existingProducts
                    .Where(p => !string.IsNullOrEmpty(p.Sku))
                    .Select(p => p.Sku.ToLowerInvariant())
                    .ToHashSet();

                var existingBarcodes = existingProducts
                    .Where(p => !string.IsNullOrEmpty(p.Barcode))
                    .Select(p => p.Barcode.ToLowerInvariant())
                    .ToHashSet();

                var categoryDict = categories
                    .GroupBy(c => c.Name.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var brandDict = brands
                    .GroupBy(b => b.Name.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                // ===== VALIDASYON =====
                var validProducts = new List<CreateProductDto>();
                var errors = new List<ProductBulkUploadErrorDto>();
                var processedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processedBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < excelData.Count; i++)
                {
                    var row = excelData[i];
                    var rowNumber = i + 6;
                    var rowErrors = new List<string>();

                    // ===== ZORUNLU ALANLAR =====
                    if (string.IsNullOrWhiteSpace(row.Name))
                        rowErrors.Add("Ürün adı zorunludur");

                    if (!row.Price.HasValue || row.Price <= 0)
                        rowErrors.Add("Geçerli bir fiyat giriniz");

                    if (!row.StockQuantity.HasValue || row.StockQuantity < 0)
                        rowErrors.Add("Geçerli bir stok miktarı giriniz");

                    // ===== BENZERSİZLİK KONTROLLERİ =====
                    if (!string.IsNullOrEmpty(row.Sku))
                    {
                        var skuKey = row.Sku.ToLowerInvariant();
                        if (existingSkus.Contains(skuKey))
                            rowErrors.Add($"SKU '{row.Sku}' sistemde zaten kayıtlı");
                        if (processedSkus.Contains(skuKey))
                            rowErrors.Add($"SKU '{row.Sku}' bu dosyada tekrar ediyor");
                        else
                            processedSkus.Add(skuKey);
                    }

                    if (!string.IsNullOrEmpty(row.Barcode))
                    {
                        var barcodeKey = row.Barcode.ToLowerInvariant();
                        if (existingBarcodes.Contains(barcodeKey))
                            rowErrors.Add($"Barkod '{row.Barcode}' sistemde zaten kayıtlı");
                        if (processedBarcodes.Contains(barcodeKey))
                            rowErrors.Add($"Barkod '{row.Barcode}' bu dosyada tekrar ediyor");
                        else
                            processedBarcodes.Add(barcodeKey);
                    }

                    // ===== KATEGORI =====
                    int? categoryId = null;
                    if (!string.IsNullOrEmpty(row.CategoryName))
                    {
                        var catKey = row.CategoryName.Trim().ToLowerInvariant();
                        if (categoryDict.TryGetValue(catKey, out int catId))
                            categoryId = catId;
                        else
                            rowErrors.Add($"Kategori '{row.CategoryName}' sistemde bulunamadı");
                    }

                    // ===== MARKA =====
                    int? brandId = null;
                    if (!string.IsNullOrEmpty(row.BrandName))
                    {
                        var brandKey = row.BrandName.Trim().ToLowerInvariant();
                        if (brandDict.TryGetValue(brandKey, out int brId))
                            brandId = brId;
                        else
                            rowErrors.Add($"Marka '{row.BrandName}' sistemde bulunamadı");
                    }

                    // ===== PARA BİRİMİ =====
                    var validCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };
                    if (!string.IsNullOrEmpty(row.Currency) && !validCurrencies.Contains(row.Currency.ToUpper()))
                        rowErrors.Add($"Para birimi '{row.Currency}' geçersiz");

                    // ===== DURUM =====
                    bool? isActive = null;
                    if (!string.IsNullOrEmpty(row.IsActive))
                    {
                        if (row.IsActive.Equals("Aktif", StringComparison.OrdinalIgnoreCase))
                            isActive = true;
                        else if (row.IsActive.Equals("Pasif", StringComparison.OrdinalIgnoreCase))
                            isActive = false;
                        else
                            rowErrors.Add($"Durum '{row.IsActive}' geçersiz. Aktif veya Pasif olmalı");
                    }

                    // ===== STOK TAKİBİ =====
                    bool? isStockTrackable = null;
                    if (!string.IsNullOrEmpty(row.IsStockTrackable))
                    {
                        if (row.IsStockTrackable.Equals("Evet", StringComparison.OrdinalIgnoreCase))
                            isStockTrackable = true;
                        else if (row.IsStockTrackable.Equals("Hayır", StringComparison.OrdinalIgnoreCase))
                            isStockTrackable = false;
                        else
                            rowErrors.Add($"Stok takibi '{row.IsStockTrackable}' geçersiz. Evet veya Hayır olmalı");
                    }

                    // ===== BİRİM =====
                    var validUnits = new[] { "Adet", "Kg", "Litre", "Metre" };
                    if (!string.IsNullOrEmpty(row.Unit) && !validUnits.Contains(row.Unit))
                        rowErrors.Add($"Birim '{row.Unit}' geçersiz");

                    if (rowErrors.Any())
                    {
                        errors.Add(new ProductBulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            Name = row.Name,
                            Sku = row.Sku,
                            ErrorMessage = string.Join(" | ", rowErrors)
                        });
                        continue;
                    }

                    // ===== VALİD ÜRÜN OLUŞTUR =====
                    validProducts.Add(new CreateProductDto
                    {
                        Name = row.Name.Trim(),
                        Sku = string.IsNullOrEmpty(row.Sku) ? null : row.Sku.Trim(),
                        Barcode = string.IsNullOrEmpty(row.Barcode) ? null : row.Barcode.Trim(),
                        Description = row.Description?.Trim(),
                        Price = row.Price.Value,
                        Currency = string.IsNullOrEmpty(row.Currency) ? "TRY" : row.Currency.ToUpper(),
                        StockQuantity = row.StockQuantity ?? 0,
                        MinStockLevel = row.MinStockLevel,
                        MaxStockLevel = row.MaxStockLevel,
                        CategoryId = categoryId,
                        BrandId = brandId,
                        Unit = string.IsNullOrEmpty(row.Unit) ? "Adet" : row.Unit,
                        IsActive = isActive ?? true,
                        IsStockTrackable = isStockTrackable ?? true
                    });
                }

                result.Errors = errors;
                result.ErrorCount = errors.Count;

                // ===== KAYDET =====
                if (validProducts.Any())
                {
                    int totalValid = validProducts.Count;

                    progress?.Report(new ProductBulkUploadProgressDto
                    {
                        UploadId = uploadId,
                        CurrentRow = 0,
                        TotalRows = totalValid,
                        CurrentName = "Başlatılıyor...",
                        Status = "Processing",
                        Percentage = 0
                    });

                    for (int index = 0; index < validProducts.Count; index++)
                    {
                        var dto = validProducts[index];
                        var percent = (int)((index + 1) * 100.0 / totalValid);

                        progress?.Report(new ProductBulkUploadProgressDto
                        {
                            UploadId = uploadId,
                            CurrentRow = index + 1,
                            TotalRows = totalValid,
                            CurrentName = dto.Name ?? "İşleniyor...",
                            CurrentSku = dto.Sku,
                            Status = "Processing",
                            Percentage = percent
                        });

                        try
                        {
                            var product = _mapper.Map<Product>(dto);
                            product.CreatedAt = DateTime.UtcNow;

                            await _unitOfWork.AddAsync(product);
                            await _unitOfWork.CompleteAsync();

                            result.SuccessCount++;
                            result.CreatedProducts.Add(_mapper.Map<ProductDto>(product));
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add(new ProductBulkUploadErrorDto
                            {
                                RowNumber = index + 1,
                                Name = dto.Name,
                                Sku = dto.Sku,
                                ErrorMessage = $"Kayıt hatası: {ex.InnerException?.Message ?? ex.Message}"
                            });
                        }
                    }

                    progress?.Report(new ProductBulkUploadProgressDto
                    {
                        UploadId = uploadId,
                        CurrentRow = totalValid,
                        TotalRows = totalValid,
                        CurrentName = "Tamamlandı! 🎉",
                        Status = "Completed",
                        Percentage = 100
                    });
                }
            }
            catch (Exception ex)
            {
                progress?.Report(new ProductBulkUploadProgressDto
                {
                    UploadId = uploadId,
                    Status = "Error",
                    CurrentName = ex.Message
                });

                result.Errors.Add(new ProductBulkUploadErrorDto
                {
                    RowNumber = 0,
                    Name = "SİSTEM",
                    ErrorMessage = $"Sistem hatası: {ex.Message}"
                });
                result.ErrorCount = excelData.Count;
            }

            return result;
        }
    }
}