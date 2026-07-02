using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Brand;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Validators.BrandValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BrandsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public BrandsController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
        }

        // GET: api/brands (Pagination + Filtre)
        [HttpGet]
        [HasPermission("product.view")]
        public async Task<IActionResult> GetAll([FromQuery] BrandPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Brand>()
                    .Where(b => !b.IsDeleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(b => b.Name.Contains(pagination.Search));
                }

                if (pagination.IsActive.HasValue)
                {
                    query = query.Where(b => b.IsActive == pagination.IsActive);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(b => b.Name)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var brandDtos = _mapper.Map<List<BrandDto>>(items);

                foreach (var dto in brandDtos)
                {
                    dto.ProductCount = await _unitOfWork.Query<Product>()
                        .CountAsync(p => p.BrandId == dto.Id && !p.IsDeleted);
                }

                var response = new BrandPaginationResponse
                {
                    Data = brandDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Brand",
                    AdditionalInfo = $"Marka listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/brands",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/brands/select-list
        [HttpGet("select-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSelectList()
        {
            var brands = await _unitOfWork.Query<Brand>()
                .Where(b => !b.IsDeleted && b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();
            return Ok(brands);
        }

        // POST: api/brands
        [HttpPost]
        [HasPermission("product.create")]
        public async Task<IActionResult> Create([FromBody] CreateBrandDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Marka bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new CreateBrandDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<Brand>().AnyAsync(b => b.Name == request.Name))
                    return BadRequest(new { message = "Bu marka adı zaten mevcut" });

                var brand = _mapper.Map<Brand>(request);
                brand.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.AddAsync(brand);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Brand",
                    EntityId = brand.Id,
                    AdditionalInfo = $"Yeni marka oluşturuldu: {brand.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<BrandDto>(brand));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/brands",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/brands/{id}
        [HttpPut("{id}")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBrandDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Marka bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "Geçersiz ID" });

                //  FluentValidation ile validasyon
                var validator = new UpdateBrandDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var brand = await _unitOfWork.GetByIdAsync<Brand>(id);
                if (brand == null)
                    return NotFound(new { message = "Marka bulunamadı" });
                var oldName = brand.Name;

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<Brand>().AnyAsync(b => b.Name == request.Name && b.Id != id))
                    return BadRequest(new { message = "Bu marka adı zaten mevcut" });

                _mapper.Map(request, brand);
                brand.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(brand);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Brand",
                    EntityId = brand.Id,
                    AdditionalInfo = $"Marka güncellendi: {oldName} -> {brand.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<BrandDto>(brand));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/brands/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }
        // DELETE: api/brands/{id}
        [HttpDelete("{id}")]
        [HasPermission("product.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _unitOfWork.GetByIdAsync<Brand>(id);
                if (brand == null)
                    return NotFound(new { message = "Marka bulunamadı" });

                var hasProducts = await _unitOfWork.Query<Product>()
                    .AnyAsync(p => p.BrandId == id && !p.IsDeleted);

                var brandName = brand.Name;

                if (hasProducts)
                {
                    // İLİŞKİ VAR: SoftDelete yap (sadece pasifleştir)
                    var productCount = await _unitOfWork.Query<Product>()
                        .CountAsync(p => p.BrandId == id && !p.IsDeleted);

                    _unitOfWork.SoftDelete(brand);
                    await _unitOfWork.CompleteAsync();

                    // ACTION LOG - Soft Delete
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "SOFT_DELETE",
                        EntityType = "Brand",
                        EntityId = id,
                        AdditionalInfo = $"Marka pasif hale getirildi: {brandName} ({productCount} ürün bağlı olduğu için)",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });

                    return Ok(new
                    {
                        message = $"Marka pasif hale getirildi. ({productCount} ürün bağlı olduğu için tamamen silinemedi)",
                        isHardDeleted = false,
                        isSoftDeleted = true,
                        productCount = productCount
                    });
                }
                else
                {
                    // İLİŞKİ YOK: Tamamen veritabanından sil
                    _unitOfWork.Delete(brand);  
                    await _unitOfWork.CompleteAsync();

                    return Ok(new
                    {
                        message = "Marka tamamen silindi",
                        isHardDeleted = true,
                        isSoftDeleted = false
                    });
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/brands/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/brands/{id}/activate
        [HttpPost("{id}/activate")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var brand = await _unitOfWork.GetByIdAsync<Brand>(id);
                if (brand == null)
                    return NotFound(new { message = "Marka bulunamadı" });

                brand.IsActive = true;
                brand.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(brand);
                await _unitOfWork.CompleteAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Brand",
                    EntityId = id,
                    AdditionalInfo = $"Marka aktif hale getirildi: {brand.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Marka aktif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/brands/{id}/activate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/brands/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var brand = await _unitOfWork.GetByIdAsync<Brand>(id);
                if (brand == null)
                    return NotFound(new { message = "Marka bulunamadı" });

                brand.IsActive = false;
                brand.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(brand);
                await _unitOfWork.CompleteAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Brand",
                    EntityId = id,
                    AdditionalInfo = $"Marka aktif hale getirildi: {brand.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Marka pasif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/brands/{id}/deactivate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // ==========Marka Resim Yükleme ==========
        // POST: api/brands/{id}/logo
        [HttpPost("{id}/logo")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> UploadLogo(int id, IFormFile file)
        {
            try
            {
                var brand = await _unitOfWork.GetByIdAsync<Brand>(id);
                if (brand == null)
                    return NotFound(new { message = "Marka bulunamadı" });

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Dosya seçilmedi" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Sadece resim dosyaları yüklenebilir" });

                if (file.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Dosya boyutu 2MB'dan küçük olmalıdır" });

            
                var fileName = $"brand_{brand.Id}_{DateTime.Now.Ticks}{extension}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Brands");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                //  URL YOLU - wwwroot/Brands klasörüne göre
                var logoUrl = $"/Brands/{fileName}";

                // Eski logoyu sil
                if (!string.IsNullOrEmpty(brand.LogoUrl))
                {
                    try
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", brand.LogoUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Eski logo silinemedi: {ex.Message}");
                    }
                }

                brand.LogoUrl = logoUrl;
                brand.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(brand);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPLOAD_LOGO",
                    EntityType = "Brand",
                    EntityId = brand.Id,
                    AdditionalInfo = $"{brand.Name} markasının logosu güncellendi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { logoUrl = logoUrl, message = "Logo başarıyla yüklendi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/brands/{id}/logo",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Dosya yüklenirken hata oluştu: {ex.Message}" });
            }
        }

        // ========== Marka Resim Silme ==========
        // DELETE: api/brands/{id}/logo
        [HttpDelete("{id}/logo")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> DeleteLogo(int id)
        {
            try
            {
                var brand = await _unitOfWork.GetByIdAsync<Brand>(id);
                if (brand == null)
                    return NotFound(new { message = "Marka bulunamadı" });

                if (string.IsNullOrEmpty(brand.LogoUrl))
                    return BadRequest(new { message = "Silinecek logo bulunamadı" });

         
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", brand.LogoUrl.TrimStart('/'));

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                var oldLogoUrl = brand.LogoUrl;
                brand.LogoUrl = null;
                brand.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(brand);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE_LOGO",
                    EntityType = "Brand",
                    EntityId = brand.Id,
                    AdditionalInfo = $"{brand.Name} markasının logosu silindi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Logo başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/brands/{id}/logo",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Logo silinirken hata oluştu: {ex.Message}" });
            }
        }
    }
}
