using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Product;
using Crm.Application.DTOs.ProductCategory;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Validators.ProductCategoryValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public ProductCategoriesController(IUnitOfWork unitOfWork, IMapper mapper,ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
        }

        // GET: api/productcategories (Pagination + Filtre)
        [HttpGet]
        [HasPermission("product.view")]
        public async Task<IActionResult> GetAll([FromQuery] ProductCategoryPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<ProductCategory>()
                    .Where(c => !c.IsDeleted)
                    .AsQueryable();

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(c => c.Name.Contains(pagination.Search));
                }

                // Aktif/Pasif filtresi
                if (pagination.IsActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == pagination.IsActive);
                }

                // Üst kategori filtresi
                if (pagination.ParentCategoryId.HasValue)
                {
                    query = query.Where(c => c.ParentCategoryId == pagination.ParentCategoryId);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Include(c => c.ParentCategory)
                    .OrderBy(c => c.Name)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var categoryDtos = _mapper.Map<List<ProductCategoryDto>>(items);

                // Ürün sayılarını hesapla
                foreach (var dto in categoryDtos)
                {
                    dto.ProductCount = await _unitOfWork.Query<Product>()
                        .CountAsync(p => p.CategoryId == dto.Id && !p.IsDeleted);
                }

                var response = new ProductCategoryPaginationResponse
                {
                    Data = categoryDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "ProductCategory",
                    AdditionalInfo = $"Ürün kategorileri listelendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/productcategories",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/productcategories/select-list
        [HttpGet("select-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSelectList()
        {
            var categories = await _unitOfWork.Query<ProductCategory>()
                .Where(c => !c.IsDeleted && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return Ok(categories);
        }

        // GET: api/productcategories/{id}
        [HttpGet("{id}")]
        [HasPermission("product.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var category = await _unitOfWork.GetByIdAsync<ProductCategory>(id);
                if (category == null)
                    return NotFound(new { message = "Kategori bulunamadı" });

                var categoryDto = _mapper.Map<ProductCategoryDto>(category);
                categoryDto.ProductCount = await _unitOfWork.Query<Product>()
                    .CountAsync(p => p.CategoryId == id && !p.IsDeleted);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "ProductCategory",
                    EntityId = id,
                    AdditionalInfo = $"Ürün kategorisi detayı görüntülendi: {category.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/productcategories/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/productcategories
        [HttpPost]
        [HasPermission("product.create")]
        public async Task<IActionResult> Create([FromBody] CreateProductCategoryDto request)
        {
            try
            {
                //  FluentValidation ile validasyon
                var validator = new CreateProductCategoryDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<ProductCategory>().AnyAsync(c => c.Name == request.Name))
                    return BadRequest(new { message = "Bu kategori adı zaten mevcut" });

                var category = _mapper.Map<ProductCategory>(request);
                category.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.AddAsync(category);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "ProductCategory",
                    EntityId = category.Id,
                    AdditionalInfo = $"Yeni ürün kategorisi oluşturuldu: {category.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });


                return Ok(_mapper.Map<ProductCategoryDto>(category));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/productcategories",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/productcategories/{id}
        [HttpPut("{id}")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCategoryDto request)
        {
            try
            {
                if (id != request.Id)
                    return BadRequest(new { message = "Geçersiz ID" });

                //  FluentValidation ile validasyon
                var validator = new UpdateProductCategoryDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var category = await _unitOfWork.GetByIdAsync<ProductCategory>(id);
                if (category == null)
                    return NotFound(new { message = "Kategori bulunamadı" });

                var oldName = category.Name;

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<ProductCategory>().AnyAsync(c => c.Name == request.Name && c.Id != id))
                    return BadRequest(new { message = "Bu kategori adı zaten mevcut" });

                _mapper.Map(request, category);
                category.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(category);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "ProductCategory",
                    EntityId = category.Id,
                    AdditionalInfo = $"Ürün kategorisi güncellendi: {oldName} -> {category.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<ProductCategoryDto>(category));
            }
            catch (Exception ex)
            {

                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/productcategories/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/productcategories/{id}
        [HttpDelete("{id}")]
        [HasPermission("product.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _unitOfWork.GetByIdAsync<ProductCategory>(id);
                if (category == null)
                    return NotFound(new { message = "Kategori bulunamadı" });

                var hasProducts = await _unitOfWork.Query<Product>()
                    .AnyAsync(p => p.CategoryId == id && !p.IsDeleted);

                var categoryName = category.Name;
                var productCount = await _unitOfWork.Query<Product>()
          .CountAsync(p => p.CategoryId == id && !p.IsDeleted);

                if (hasProducts)
                {
                    _unitOfWork.SoftDelete(category);
                    await _unitOfWork.CompleteAsync();

                    // ACTION LOG - Soft Delete
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "SOFT_DELETE",
                        EntityType = "ProductCategory",
                        EntityId = id,
                        AdditionalInfo = $"Kategori pasif hale getirildi: {categoryName} ({productCount} ürün bağlı olduğu için)",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                    return Ok(new { message = "Kategori pasif hale getirildi (ilişkili ürünler nedeniyle)" });
                }
                else
                {
                    // Delete metodu zaten hard delete yapıyor
                    _unitOfWork.Delete(category); 
                    await _unitOfWork.CompleteAsync();
                    // ACTION LOG - Hard Delete
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "HARD_DELETE",
                        EntityType = "ProductCategory",
                        EntityId = id,
                        AdditionalInfo = $"Kategori tamamen silindi: {categoryName}",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                    return Ok(new { message = "Kategori tamamen silindi" });
                }
            }
            catch (Exception ex)
            {

                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/productcategories/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/productcategories/{id}/activate
        [HttpPost("{id}/activate")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {

                var category = await _unitOfWork.GetByIdAsync<ProductCategory>(id);
                if (category == null)
                    return NotFound(new { message = "Kategori bulunamadı" });

                category.IsActive = true;
                category.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(category);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "ProductCategory",
                    EntityId = id,
                    AdditionalInfo = $"Kategori aktif hale getirildi: {category.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Kategori aktif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/productcategories/{id}/activate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/productcategories/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        [HasPermission("product.edit")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var category = await _unitOfWork.GetByIdAsync<ProductCategory>(id);
                if (category == null)
                    return NotFound(new { message = "Kategori bulunamadı" });

                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(category);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DEACTIVATE",
                    EntityType = "ProductCategory",
                    EntityId = id,
                    AdditionalInfo = $"Kategori pasif hale getirildi: {category.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Kategori pasif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/productcategories/{id}/deactivate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }
    }
}