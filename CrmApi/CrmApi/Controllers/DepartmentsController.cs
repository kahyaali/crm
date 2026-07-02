
using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Department;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Validators.DepartmentValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public DepartmentsController(IUnitOfWork unitOfWork, IMapper mapper,ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
        }

        [HttpGet("select-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetList()
        {
            var departments = await _unitOfWork.Query<Department>()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();
            return Ok(departments);
        }

        [HttpGet("all")]
        [HasPermission("department.viewall")]
        public async Task<IActionResult> GetAllIncludingInactive([FromQuery] DepartmentPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Department>().AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(d => d.Name.Contains(pagination.Search));
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(d => d.Name)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var departmentDtos = _mapper.Map<List<DepartmentDto>>(items);

                foreach (var dto in departmentDtos)
                {
                    dto.PersonelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.DepartmentId == dto.Id && !p.IsDeleted);
                }

                var response = new DepartmentPaginationResponse
                {
                    Data = departmentDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Department",
                    AdditionalInfo = $"Tüm departmanlar listelendi (aktif/pasif). Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/departments/all",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpGet]
        [HasPermission("department.view")]
        public async Task<IActionResult> GetAll([FromQuery] DepartmentPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Department>()
                    .Where(d => d.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(d => d.Name.Contains(pagination.Search));
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(d => d.Name)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var departmentDtos = _mapper.Map<List<DepartmentDto>>(items);

                foreach (var dto in departmentDtos)
                {
                    dto.PersonelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.DepartmentId == dto.Id && !p.IsDeleted);
                }

                var response = new DepartmentPaginationResponse
                {
                    Data = departmentDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Department",
                    AdditionalInfo = $"Aktif departmanlar listelendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/departments",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        [HasPermission("department.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var department = await _unitOfWork.GetByIdAsync<Department>(id);
                if (department == null)
                    return NotFound(new { message = $"Departman bulunamadı (ID: {id})" });

                var departmentDto = _mapper.Map<DepartmentDto>(department);
                departmentDto.PersonelCount = await _unitOfWork.Query<Personel>()
                    .CountAsync(p => p.DepartmentId == id && !p.IsDeleted);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Department",
                    EntityId = id,
                    AdditionalInfo = $"Departman detayı görüntülendi: {department.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(departmentDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/departments/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // Departman Create
        [HttpPost]
        [HasPermission("department.create")]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Departman bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new CreateDepartmentValidator();
                var departmentEntity = new Department
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = true
                };

                var validationResult = await validator.ValidateAsync(departmentEntity);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<Department>().AnyAsync(d => d.Name == request.Name))
                    return BadRequest(new { message = $"'{request.Name}' departman adı zaten mevcut" });

                var department = _mapper.Map<Department>(request);
                department.CreatedAt = DateTime.UtcNow;
                department.IsActive = true;

                await _unitOfWork.AddAsync(department);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Department",
                    EntityId = department.Id,
                    AdditionalInfo = $"Yeni departman oluşturuldu: {department.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<DepartmentDto>(department));
            }
            catch (Exception ex)
            {

                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/departments",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // Departman Update
        [HttpPut("{id}")]
        [HasPermission("department.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Departman bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new UpdateDepartmentValidator();
                var departmentEntity = new Department
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description
                };

                var validationResult = await validator.ValidateAsync(departmentEntity);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var department = await _unitOfWork.GetByIdAsync<Department>(id);
                if (department == null)
                    return NotFound(new { message = $"Departman bulunamadı (ID: {id})" });

                var oldName = department.Name;

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<Department>().AnyAsync(d => d.Name == request.Name && d.Id != id))
                    return BadRequest(new { message = $"'{request.Name}' departman adı zaten başka bir departman tarafından kullanılıyor" });

                _mapper.Map(request, department);
                department.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(department);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Department",
                    EntityId = department.Id,
                    AdditionalInfo = $"Departman güncellendi: {oldName} -> {department.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<DepartmentDto>(department));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/departments/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        [HasPermission("department.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var department = await _unitOfWork.GetByIdAsync<Department>(id);
                if (department == null)
                    return NotFound(new { message = $"Departman bulunamadı (ID: {id})" });

                var hasPersonel = await _unitOfWork.Query<Personel>()
                    .AnyAsync(p => p.DepartmentId == id && !p.IsDeleted);

                var departmentName = department.Name;

                if (hasPersonel)
                {
                    var personelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.DepartmentId == id && !p.IsDeleted);

                    // ACTION LOG - Silinememe
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "DELETE_FAILED",
                        EntityType = "Department",
                        EntityId = id,
                        AdditionalInfo = $"Departman silinemedi: {departmentName} ({personelCount} personel bağlı olduğu için)",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });

                    return BadRequest(new
                    {
                        message = $"Bu departmana bağlı {personelCount} personel var. Önce personelleri başka departmana taşımalısınız.",
                        personelCount = personelCount
                    });
                }

                _unitOfWork.Delete(department);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Department",
                    EntityId = id,
                    AdditionalInfo = $"Departman tamamen silindi: {departmentName}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Departman tamamen silindi", id = id });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/departments/{id}",
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
        [HasPermission("department.deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var department = await _unitOfWork.GetByIdAsync<Department>(id);
                if (department == null)
                    return NotFound(new { message = $"Departman bulunamadı (ID: {id})" });

                var hasPersonel = await _unitOfWork.Query<Personel>()
                    .AnyAsync(p => p.DepartmentId == id && !p.IsDeleted);

                if (hasPersonel)
                {
                    var personelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.DepartmentId == id && !p.IsDeleted);
                    return BadRequest(new
                    {
                        message = $"Bu departmana bağlı {personelCount} personel var. Pasif yapmak için önce personelleri başka departmana taşımalısınız.",
                        personelCount = personelCount
                    });
                }

                if (!department.IsActive)
                    return BadRequest(new { message = "Departman zaten pasif durumda" });

                department.IsActive = false;
                department.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(department);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DEACTIVATE",
                    EntityType = "Department",
                    EntityId = id,
                    AdditionalInfo = $"Departman pasif hale getirildi: {department.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Departman pasif hale getirildi", id = id, isActive = false });
            }
            catch (Exception ex)
            {

                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/departments/{id}/deactivate",
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
        [HasPermission("department.activate")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var department = await _unitOfWork.GetByIdAsync<Department>(id);
                if (department == null)
                    return NotFound(new { message = $"Departman bulunamadı (ID: {id})" });

                if (department.IsActive)
                    return BadRequest(new { message = "Departman zaten aktif durumda" });

                department.IsActive = true;
                department.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(department);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Department",
                    EntityId = id,
                    AdditionalInfo = $"Departman aktif hale getirildi: {department.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Departman aktif hale getirildi", id = id, isActive = true });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/departments/{id}/activate",
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