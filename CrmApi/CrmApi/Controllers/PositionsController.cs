
using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Position;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Validators.PositionValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PositionsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public PositionsController(IUnitOfWork unitOfWork, IMapper mapper,ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
        }

        [HttpGet("select-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetList()
        {
            var positions = await _unitOfWork.Query<Position>()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();
            return Ok(positions);
        }

        [HttpGet]
        [HasPermission("positions.view")]
        public async Task<IActionResult> GetAll([FromQuery] PositionPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Position>()
                    .Where(p => p.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(p => p.Name.Contains(pagination.Search));
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(p => p.Name)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var positionDtos = _mapper.Map<List<PositionResponseDto>>(items);

                foreach (var dto in positionDtos)
                {
                    dto.PersonelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.PositionId == dto.Id && !p.IsDeleted);
                }

                var response = new PositionPaginationResponse
                {
                    Data = positionDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Position",
                    AdditionalInfo = $"Aktif pozisyonlar listelendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/positions",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpGet("all")]
        [HasPermission("positions.viewall")]
        public async Task<IActionResult> GetAllIncludingInactive([FromQuery] PositionPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Position>().AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(p => p.Name.Contains(pagination.Search));
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(p => p.Name)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var positionDtos = _mapper.Map<List<PositionResponseDto>>(items);

                foreach (var dto in positionDtos)
                {
                    dto.PersonelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.PositionId == dto.Id && !p.IsDeleted);
                }

                var response = new PositionPaginationResponse
                {
                    Data = positionDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Position",
                    AdditionalInfo = $"Tüm pozisyonlar listelendi (aktif/pasif). Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/positions/all",
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
        [HasPermission("positions.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var position = await _unitOfWork.GetByIdAsync<Position>(id);
                if (position == null)
                    return NotFound(new { message = "Pozisyon bulunamadı" });

                var positionDto = _mapper.Map<PositionResponseDto>(position);
                positionDto.PersonelCount = await _unitOfWork.Query<Personel>()
                    .CountAsync(p => p.PositionId == id && !p.IsDeleted);

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Position",
                    EntityId = id,
                    AdditionalInfo = $"Pozisyon detayı görüntülendi: {position.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });


                return Ok(positionDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/positions/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // Pozisyon Create
        [HttpPost]
        [HasPermission("positions.create")]
        public async Task<IActionResult> Create([FromBody] CreatePositionDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Pozisyon bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new CreatePositionValidator();
                var positionEntity = new Position
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = true
                };

                var validationResult = await validator.ValidateAsync(positionEntity);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<Position>().AnyAsync(p => p.Name == request.Name))
                    return BadRequest(new { message = "Bu pozisyon adı zaten mevcut" });

                var position = _mapper.Map<Position>(request);
                position.CreatedAt = DateTime.UtcNow;
                position.IsActive = true;

                await _unitOfWork.AddAsync(position);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Position",
                    EntityId = position.Id,
                    AdditionalInfo = $"Yeni pozisyon oluşturuldu: {position.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<PositionResponseDto>(position));
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/positions",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // Pozisyon Update
        [HttpPut("{id}")]
        [HasPermission("positions.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePositionDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Pozisyon bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new UpdatePositionValidator();
                var positionEntity = new Position
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description
                };

                var validationResult = await validator.ValidateAsync(positionEntity);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var position = await _unitOfWork.GetByIdAsync<Position>(id);
                if (position == null)
                    return NotFound(new { message = "Pozisyon bulunamadı" });

                var oldName = position.Name;

                //  SADECE VERİTABANI KONTROLÜ
                if (await _unitOfWork.Query<Position>().AnyAsync(p => p.Name == request.Name && p.Id != id))
                    return BadRequest(new { message = "Bu pozisyon adı zaten mevcut" });

                _mapper.Map(request, position);
                position.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(position);
                await _unitOfWork.CompleteAsync();

                var responseDto = new PositionResponseDto
                {
                    Id = position.Id,
                    Name = position.Name,
                    Description = position.Description,
                    IsActive = position.IsActive,
                    CreatedAt = position.CreatedAt,
                    PersonelCount = await _unitOfWork.Query<Personel>()
                        .CountAsync(p => p.PositionId == id && !p.IsDeleted)
                };

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Position",
                    EntityId = position.Id,
                    AdditionalInfo = $"Pozisyon güncellendi: {oldName} -> {position.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(responseDto);
            }
            catch (Exception ex)
            {

                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/positions/{id}",
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
        [HasPermission("positions.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var position = await _unitOfWork.GetByIdAsync<Position>(id);
                if (position == null)
                    return NotFound(new { message = "Pozisyon bulunamadı" });

                var hasPersonel = await _unitOfWork.Query<Personel>()
                    .AnyAsync(p => p.PositionId == id && !p.IsDeleted);

                var positionName = position.Name;

                if (hasPersonel)
                {
                    // ACTION LOG - Silinememe
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "DELETE_FAILED",
                        EntityType = "Position",
                        EntityId = id,
                        AdditionalInfo = $"Pozisyon silinemedi: {positionName} (bağlı personel var)",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });
                    return BadRequest(new { message = "Bu pozisyona bağlı personeller var. Önce onları başka pozisyona taşımalısınız." });
                }

                _unitOfWork.Delete(position);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Position",
                    EntityId = id,
                    AdditionalInfo = $"Pozisyon tamamen silindi: {positionName}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Pozisyon tamamen silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/positions/{id}",
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
        [HasPermission("positions.deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {

            try
            {
                var position = await _unitOfWork.GetByIdAsync<Position>(id);
                if (position == null)
                    return NotFound(new { message = "Pozisyon bulunamadı" });

                var hasPersonel = await _unitOfWork.Query<Personel>()
                    .AnyAsync(p => p.PositionId == id && !p.IsDeleted);

                if (hasPersonel)
                    return BadRequest(new { message = "Bu pozisyona bağlı personeller var. Pasif yapmak için önce personelleri başka pozisyona taşımalısınız." });

                position.IsActive = false;
                position.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(position);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DEACTIVATE",
                    EntityType = "Position",
                    EntityId = id,
                    AdditionalInfo = $"Pozisyon pasif hale getirildi: {position.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Pozisyon pasif hale getirildi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/positions/{id}/deactivate",
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
        [HasPermission("positions.activate")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var position = await _unitOfWork.GetByIdAsync<Position>(id);
                if (position == null)
                    return NotFound(new { message = "Pozisyon bulunamadı" });

                position.IsActive = true;
                position.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(position);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Position",
                    EntityId = id,
                    AdditionalInfo = $"Pozisyon aktif hale getirildi: {position.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Pozisyon aktif hale getirildi" });
            }

            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/positions/{id}/activate",
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