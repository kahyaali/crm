using AutoMapper;
using ClosedXML.Excel;
using Crm.API.Attributes;
using Crm.Application.DTOs.Personel;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Helpers;
using CrmApi.Services;
using CrmApi.Validators.PersonelValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PersonelsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDataFilterService _dataFilterService;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly INotificationService _notificationService;


        public PersonelsController(IUnitOfWork unitOfWork, IDataFilterService dataFilterService, IMapper mapper, ILogService logService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _dataFilterService = dataFilterService;
            _mapper = mapper;
            _logService = logService;
            _notificationService= notificationService;

        }

        // GET: api/personels
        [HttpGet]
        [HasPermission("mypersonel.view")]
        public async Task<IActionResult> GetAll([FromQuery] PersonelPaginationDto pagination)
        {
            try
            {
                var query = _unitOfWork.Query<Personel>()
                    .Include(p => p.User)
                    .Include(p => p.Department)
                    .Include(p => p.Position)
                    .AsQueryable();

                query = await _dataFilterService.FilterPersonelsByRole(query);

                //  IsActive filtresi
                if (pagination.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == pagination.IsActive);
                }

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(p =>
                        p.FirstName.Contains(pagination.Search) ||
                        p.LastName.Contains(pagination.Search) ||
                        p.Email.Contains(pagination.Search));
                }

                if (pagination.DepartmentId.HasValue)
                {
                    query = query.Where(p => p.DepartmentId == pagination.DepartmentId);
                }

                if (pagination.PositionId.HasValue)
                {
                    query = query.Where(p => p.PositionId == pagination.PositionId);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var personelDtos = _mapper.Map<List<PersonelDto>>(items);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                foreach (var dto in personelDtos)
                {
                    if (!string.IsNullOrEmpty(dto.AvatarUrl) && !dto.AvatarUrl.StartsWith("http"))
                    {
                        dto.AvatarUrl = $"{baseUrl}{dto.AvatarUrl}";
                    }
                }

                var response = new PersonelPaginationResponse
                {
                    Data = personelDtos,
                    TotalCount = totalCount,
                    Page = pagination.Page,
                    PageSize = pagination.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
                };

                // ActionLog
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Personel",
                    AdditionalInfo = $"Personel listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
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
                    RequestPath = "/api/personels",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/personels/{id}
        [HttpGet("{id}")]
       // [HasPermission("personel.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var personel = await _unitOfWork.Query<Personel>()
                               .Include(p => p.Department)
                               .Include(p => p.Position)
                               .Include(p => p.Manager)
                               .FirstOrDefaultAsync(p => p.Id == id);

                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });

                // ActionLog
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Personel",
                    EntityId = id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} personel detayı görüntülendi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<PersonelDto>(personel));
            }
            catch (Exception ex)
            {
                //ErrorLog
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/personels
        [HttpPost]
        [HasPermission("personel.create")]
        public async Task<IActionResult> Create([FromBody] CreatePersonelDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Personel bilgileri eksik" });

                var validator = new CreatePersonelDtoValidator();
                var validationResult = await validator.ValidateAsync(request);



                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }


                var duplicateNumberPersonel = await _unitOfWork.Query<Personel>()
            .FirstOrDefaultAsync(p => (!string.IsNullOrEmpty(request.PersonnelNumber) && p.PersonnelNumber == request.PersonnelNumber)
                                   || (!string.IsNullOrEmpty(request.RegistrationNumber) && p.RegistrationNumber == request.RegistrationNumber));

                if (duplicateNumberPersonel != null)
                {
                    var dbErrors = new Dictionary<string, string[]>();

                    if (!string.IsNullOrEmpty(request.PersonnelNumber) && duplicateNumberPersonel.PersonnelNumber == request.PersonnelNumber)
                        dbErrors.Add(nameof(request.PersonnelNumber), new[] { "Bu Personel Numarası zaten aktif bir kullanıcı tarafından kullanılıyor." });

                    if (!string.IsNullOrEmpty(request.RegistrationNumber) && duplicateNumberPersonel.RegistrationNumber == request.RegistrationNumber)
                        dbErrors.Add(nameof(request.RegistrationNumber), new[] { "Bu Sicil Numarası zaten aktif bir kullanıcı tarafından kullanılıyor." });

                    return BadRequest(new { message = "Doğrulama hatası", errors = dbErrors });
                }

                //  SADECE VERİTABANI KONTROLLERİ (manuel)
                var existingPersonel = await _unitOfWork.Query<Personel>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Email == request.Email);

                if (existingPersonel != null)
                {
                    if (!existingPersonel.IsDeleted)
                        return BadRequest(new { message = "Bu email adresi zaten kayıtlı" });

                    // Soft delete edilmiş personeli geri getir
                    existingPersonel.IsDeleted = false;
                    _mapper.Map(request, existingPersonel);
                    existingPersonel.UpdatedAt = DateTime.UtcNow;

                    if (request.CreateUser && !string.IsNullOrEmpty(request.Password))
                    {
                        if (existingPersonel.UserId == null)
                        {
                            var user = await CreateUserAsync(request);
                            existingPersonel.UserId = user.Id;
                        }
                    }

                    _unitOfWork.Update(existingPersonel);
                    await _unitOfWork.CompleteAsync();

                    // ActionLog
                    await _logService.LogActionAsync(new ActionLog
                    {
                        ActionType = "RESTORE",
                        EntityType = "Personel",
                        EntityId = existingPersonel.Id,
                        AdditionalInfo = $"{existingPersonel.FirstName} {existingPersonel.LastName} personeli geri getirildi | " +
                        $"Personel No: {existingPersonel.PersonnelNumber} | " +
                        $"Sicil No: {existingPersonel.RegistrationNumber}",
                        UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                        IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                        CreatedAt = DateTime.UtcNow
                    });

                    return Ok(_mapper.Map<PersonelDto>(existingPersonel));
                }

                // Yeni personel oluştur
                var personel = _mapper.Map<Personel>(request);
                personel.CreatedAt = DateTime.UtcNow;

                if (request.CreateUser && !string.IsNullOrEmpty(request.Password))
                {
                    var user = await CreateUserAsync(request);
                    personel.UserId = user.Id;
                }

                await _unitOfWork.AddAsync(personel);
                await _unitOfWork.CompleteAsync();

                // ActionLog
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"Yeni personel oluşturuldu: {personel.FirstName} {personel.LastName} | " +
                    $"Email: {personel.Email} | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<PersonelDto>(personel));
            }
            catch (DbUpdateException ex)
            {
                // ErrorLog
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/personels",
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
                // ErrorLog
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/personels",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // PUT: api/personels/{id}
        [HttpPut("{id}")]
        [HasPermission("personel.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePersonelDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Personel bilgileri eksik" });

                //  FluentValidation ile validasyon
                var validator = new UpdatePersonelDtoValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });


                var duplicatePersonel = await _unitOfWork.Query<Personel>()
            .FirstOrDefaultAsync(p => p.Id != id && (
                                   p.Email == request.Email
                                   || (!string.IsNullOrEmpty(request.PersonnelNumber) && p.PersonnelNumber == request.PersonnelNumber)
                                   || (!string.IsNullOrEmpty(request.RegistrationNumber) && p.RegistrationNumber == request.RegistrationNumber)));

                if (duplicatePersonel != null)
                {
                    var dbErrors = new Dictionary<string, string[]>();

                    if (duplicatePersonel.Email == request.Email)
                        dbErrors.Add(nameof(request.Email), new[] { "Bu email adresi başka bir personel tarafından kullanılıyor." });

                    if (!string.IsNullOrEmpty(request.PersonnelNumber) && duplicatePersonel.PersonnelNumber == request.PersonnelNumber)
                        dbErrors.Add(nameof(request.PersonnelNumber), new[] { "Bu Personel Numarası başka bir personele ait." });

                    if (!string.IsNullOrEmpty(request.RegistrationNumber) && duplicatePersonel.RegistrationNumber == request.RegistrationNumber)
                        dbErrors.Add(nameof(request.RegistrationNumber), new[] { "Bu Sicil Numarası başka bir personele ait." });

                    return BadRequest(new { message = "Doğrulama hatası", errors = dbErrors });
                }


                var oldName = $"{personel.FirstName} {personel.LastName}";
                var oldEmail = personel.Email;

                //  SADECE VERİTABANI KONTROLÜ (manuel)
                if (personel.Email != request.Email && await _unitOfWork.AnyAsync<Personel>(p => p.Email == request.Email))
                    return BadRequest(new { message = "Bu email adresi başka bir personel tarafından kullanılıyor" });

                _mapper.Map(request, personel);
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG EKLENDI
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"Personel güncellendi: {oldName} -> {personel.FirstName} {personel.LastName} | " +
                    $"Email: {oldEmail} -> {personel.Email} | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(_mapper.Map<PersonelDto>(personel));
            }
            catch (DbUpdateException ex)
            {
                // ERROR LOG 
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}",
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
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/personels/{id}
        [HttpDelete("{id}")]
        [HasPermission("personel.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });

                // İlişki kontrolü
                var hasTickets = await _unitOfWork.Query<Ticket>().AnyAsync(t => t.AssignedToPersonelId == id && !t.IsDeleted);
                var hasTasks = await _unitOfWork.Query<DomainTask>().AnyAsync(t => t.AssignedToPersonelId == id && !t.IsDeleted);

                if (hasTickets || hasTasks)
                {
                    return BadRequest(new
                    {
                        message = "Bu personele atanmış ticket veya task var. Önce onları başka personele atamalısınız."
                    });
                }

               
                var personelName = $"{personel.FirstName} {personel.LastName}";

                // Kullanıcısı varsa sil
                if (personel.UserId != null)
                {
                    var user = await _unitOfWork.GetByIdAsync<User>(personel.UserId.Value);
                    if (user != null)
                    {
                        _unitOfWork.Delete(user);
                    }
                }

                _unitOfWork.Delete(personel);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Personel",
                    EntityId = id,
                    AdditionalInfo = $"Personel ve bağlı kullanıcı silindi: {personelName} | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Personel ve bağlı kullanıcı tamamen silindi" });
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/personels/{id}/create-user
        [HttpPost("{id}/create-user")]
        [HasPermission("personel.createuser")]
        public async Task<IActionResult> CreateUserForPersonel(int id, [FromBody] CreateUserForPersonelRequest request)
        {
            try
            {
                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });

                if (personel.UserId != null)
                    return BadRequest(new { message = "Bu personele zaten kullanıcı açılmış" });

                var user = await CreateUserAsync(personel.FirstName, personel.LastName, personel.Email, request.Password);
                personel.UserId = user.Id;
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE_USER",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} personeli için kullanıcı hesabı oluşturuldu | " +
                    $"Email: {personel.Email} | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Kullanıcı başarıyla oluşturuldu", email = user.Email });
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}/create-user",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // Personel resim yükleme
        [HttpPost("{id}/avatar")]
        //[HasPermission("personel.edit")]
        public async Task<IActionResult> UploadAvatar(int id, IFormFile file)
        {

            try
            {
                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound();

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Dosya seçilmedi" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Sadece resim dosyaları yüklenebilir" });

                if (file.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Dosya boyutu 2MB'dan küçük olmalıdır" });

                var fileName = $"avatar_{personel.Id}_{DateTime.Now.Ticks}{extension}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var avatarUrl = $"{baseUrl}/avatars/{fileName}";

                // Eski avatarı sil
                if (!string.IsNullOrEmpty(personel.AvatarUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", personel.AvatarUrl.Replace(baseUrl, "").TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                personel.AvatarUrl = avatarUrl;
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG 
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPLOAD_AVATAR",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} personelinin avatarı güncellendi | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { avatarUrl = avatarUrl });
            }
            catch (Exception ex)
            {
                // ERROR LOG 
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}/avatar",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // Personel kendi avatarını güncelleme
        [HttpPut("my-avatar")]
       // [HasPermission("personel.edit")]
        public async Task<IActionResult> UpdateMyAvatar(IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Kullanıcı bulunamadı" });

                var userId = int.Parse(userIdClaim);
                var personel = await _unitOfWork.Query<Personel>().FirstOrDefaultAsync(p => p.UserId == userId);

                if (personel == null)
                    return NotFound(new { message = "Personel kaydınız bulunamadı" });

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Dosya seçilmedi" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Sadece resim dosyaları yüklenebilir" });

                if (file.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Dosya boyutu 2MB'dan küçük olmalıdır" });

                var fileName = $"avatar_{personel.Id}_{DateTime.Now.Ticks}{extension}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var oldAvatarUrl = personel.AvatarUrl;

                if (!string.IsNullOrEmpty(personel.AvatarUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", personel.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                personel.AvatarUrl = $"/avatars/{fileName}";
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE_MY_AVATAR",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} kendi avatarını güncelledi | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber} | " +
                    $"Eski avatar: {oldAvatarUrl ?? "Yok"} | Yeni avatar: {personel.AvatarUrl}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { avatarUrl = personel.AvatarUrl, message = "Avatar başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/personels/my-avatar",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // DELETE: api/personels/my-avatar
        [HttpDelete("{id}/avatar")]
        [HasPermission("personel.edit")]
        public async Task<IActionResult> DeleteAvatar(int id)
        {
            try
            {
                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });

                if (string.IsNullOrEmpty(personel.AvatarUrl))
                    return BadRequest(new { message = "Silinecek avatar bulunamadı" });

                var oldAvatarUrl = personel.AvatarUrl;

                // Fiziksel dosyayı sil
                var fileName = Path.GetFileName(personel.AvatarUrl);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars", fileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                // Database'den avatar URL'ini temizle
                personel.AvatarUrl = null;
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE_PERSONEL_AVATAR_BY_ADMIN",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"Yetkili kullanıcı, {personel.FirstName} {personel.LastName} isimli personelin logosunu sildi. Eski logo: {oldAvatarUrl}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Avatar başarıyla silindi", avatarUrl = (string?)null });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpDelete("my-avatar")]
        public async Task<IActionResult> DeleteMyAvatar()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized();

                var userId = int.Parse(userIdClaim);
                var personel = await _unitOfWork.Query<Personel>()
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (personel == null)
                    return NotFound();

                if (string.IsNullOrEmpty(personel.AvatarUrl))
                    return BadRequest();

                var oldAvatarUrl = personel.AvatarUrl;

                var fileName = Path.GetFileName(personel.AvatarUrl);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars", fileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                personel.AvatarUrl = null;
                personel.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE_MY_AVATAR",
                    EntityType = "Personel",
                    EntityId = personel.Id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} kendi logosunu sildi. Eski logo: {oldAvatarUrl}",
                    UserId = userId,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Logonuz başarıyla silindi", avatarUrl = (string?)null });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        // Personel kendisine bağlı olan personelleri getirir
        // GET: api/personels/my-team
        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            try
            {
                var currentPersonel = await _dataFilterService.GetCurrentPersonel();
                if (currentPersonel == null)
                    return NotFound(new { message = "Personel kaydınız bulunamadı" });

                
                var query = _unitOfWork.Query<Personel>()
                    .AsNoTracking()
                    .Include(p => p.Department)
                    .Include(p => p.Position)
                    .AsQueryable();

                query = await _dataFilterService.FilterPersonelsByRole(query);

                query = query.Where(p => p.Id != currentPersonel.Id && p.ManagerId == currentPersonel.Id);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.FirstName.Contains(search) ||
                        p.LastName.Contains(search) ||
                        p.Email.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var teamMembers = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var teamMemberDtos = _mapper.Map<List<PersonelDto>>(teamMembers);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                foreach (var dto in teamMemberDtos)
                {
                    if (!string.IsNullOrEmpty(dto.AvatarUrl) && !dto.AvatarUrl.StartsWith("http"))
                    {
                        dto.AvatarUrl = $"{baseUrl}{dto.AvatarUrl}";
                    }
                }

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW_MY_TEAM",
                    EntityType = "Personel",
                    AdditionalInfo = $"{currentPersonel.FirstName} {currentPersonel.LastName} kendi ekibini görüntüledi. Toplam {totalCount} ekip üyesi",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    currentPersonel = _mapper.Map<PersonelDto>(currentPersonel),
                    teamMembers = teamMemberDtos,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/personels/my-team",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // Helper Methods
        private async Task<User> CreateUserAsync(CreatePersonelDto request)
        {
            return await CreateUserAsync(request.FirstName, request.LastName, request.Email, request.Password);
        }

        private async Task<User> CreateUserAsync(string firstName, string lastName, string email, string password)
        {

            try
            {
                var defaultRole = await _unitOfWork.Query<Role>().FirstOrDefaultAsync(r => r.Name == "User");
                Role role = defaultRole;
                if (role == null)
                {
                    role = new Role { Name = "User", Description = "Standart Kullanıcı", CreatedAt = DateTime.UtcNow };
                    await _unitOfWork.AddAsync(role);
                    await _unitOfWork.CompleteAsync();
                }

                PasswordHelper.CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                var user = new User
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Email = email.Trim(),
                    PasswordHash = Convert.ToBase64String(passwordHash),
                    PasswordSalt = Convert.ToBase64String(passwordSalt),
                    RoleId = role.Id,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AddAsync(user);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE_USER",
                    EntityType = "User",
                    EntityId = user.Id,
                    AdditionalInfo = $"Yeni kullanıcı oluşturuldu: {firstName} {lastName} (Email: {email})",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return user;
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/personels/create-user",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                throw; // Hatayı yukarı fırlat
            }
        }

        [HttpPost("{id}/activate")]
        [HasPermission("personel.edit")]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });

                personel.IsActive = true;
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "ACTIVATE",
                    EntityType = "Personel",
                    EntityId = id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} personeli aktif hale getirildi | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Personel aktif hale getirildi" });
            }
            catch (Exception ex)
            {
                // ERROR LOG
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}/activate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }



        [HttpPost("{id}/deactivate")]
        [HasPermission("personel.edit")]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var personel = await _unitOfWork.GetByIdAsync<Personel>(id);
                if (personel == null)
                    return NotFound(new { message = "Personel bulunamadı" });

                // İlişki kontrolü - atanmış müşteri var mı?
                var hasCustomers = await _unitOfWork.Query<Customer>().AnyAsync(c => c.AssignedToPersonelId == id && !c.IsDeleted);
                if (hasCustomers)
                {
                    return BadRequest(new { message = "Bu personele bağlı müşteriler var. Önce onları başka personele atamalısınız." });
                }

                personel.IsActive = false;
                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();
                // ACTION LOG
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DEACTIVATE",
                    EntityType = "Personel",
                    EntityId = id,
                    AdditionalInfo = $"{personel.FirstName} {personel.LastName} personeli pasif hale getirildi | " +
                    $"Personel No: {personel.PersonnelNumber} | " +
                    $"Sicil No: {personel.RegistrationNumber}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Personel pasif hale getirildi" });
            }
            catch (Exception ex)
            {
                // ERROR LOG 
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/personels/{id}/deactivate",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // Personel profil sayfasında kendi bilgilerini görür
        // GET: api/personels/my-info
        [HttpGet("my-info")]
       // [HasPermission("personel.view")]
        public async Task<IActionResult> GetMyInfo()
        {
            try
            {
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Giriş yapmalısınız" });

                var userId = int.Parse(userIdClaim);

                var personel = await _unitOfWork.Query<Personel>()
                    .Include(p => p.Department)
                    .Include(p => p.Position)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (personel == null)
                    return NotFound(new { message = "Personel kaydınız bulunamadı" });

                var personelDto = new PersonelDto
                {
                    Id = personel.Id,
                    FirstName = personel.FirstName,
                    LastName = personel.LastName,
                    Email = personel.Email,
                    Phone = personel.Phone,
                    Address = personel.Address,
                    City = personel.City,
                    District = personel.District,
                    PostalCode = personel.PostalCode,
                    AvatarUrl = personel.AvatarUrl,
                    CreatedAt = personel.CreatedAt,
                    IsActive = personel.IsActive,
                    DepartmentName = personel.Department?.Name,
                    PositionName = personel.Position?.Name
                };

                if (!string.IsNullOrEmpty(personelDto.AvatarUrl) && !personelDto.AvatarUrl.StartsWith("http"))
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    personelDto.AvatarUrl = $"{baseUrl}{personelDto.AvatarUrl}";
                }

                return Ok(personelDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // Personel kendi bilgilerini günceller
        // PUT: api/personels/my-profile
        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized();

                var userId = int.Parse(userIdClaim);
                var personel = await _unitOfWork.Query<Personel>()
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (personel == null)
                    return NotFound();

                // Sadece gelen alanları güncelle
                if (!string.IsNullOrEmpty(request.FirstName))
                    personel.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    personel.LastName = request.LastName;
                if (!string.IsNullOrEmpty(request.Phone))
                    personel.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Address))
                    personel.Address = request.Address;
                if (!string.IsNullOrEmpty(request.City))
                    personel.City = request.City;
                if (!string.IsNullOrEmpty(request.District))
                    personel.District = request.District;
                if (!string.IsNullOrEmpty(request.PostalCode))
                    personel.PostalCode = request.PostalCode;

                personel.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(personel);
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "Profil güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        // ========== 1. EXCEL ŞABLON İNDİRME ==========
        [HttpGet("download-template")]
        // [HasPermission("personel.create")]
        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Personel Şablonu");

                    // ===== BAŞLIK =====
                    worksheet.Cell(1, 1).Value = "📋 PERSONEL TOPLU YÜKLEME ŞABLONU";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(0, 51, 102);
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // ===== AÇIKLAMA =====
                    worksheet.Cell(2, 1).Value = "⚠️ Zorunlu alanlar: Ad, Soyad, Email, Telefon";
                    worksheet.Cell(2, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Red;
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(3, 1).Value = "💡 Departman ve Pozisyon isimleri sistemdeki isimlerle BİREBİR aynı olmalıdır";
                    worksheet.Cell(3, 1).Style.Font.FontSize = 10;
                    worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromArgb(255, 128, 0);
                    worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(4, 1).Value = "📌 Sistemdeki departman ve pozisyon listesini görmek için '📋 Listeler' butonuna tıklayın";
                    worksheet.Cell(4, 1).Style.Font.FontSize = 9;
                    worksheet.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;
                    worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Row(4).Height = 15;

                    // ===== HEADER SATIRI =====
                    int headerRow = 5;
                    var headers = new string[]
                    {
                "Ad*", "Soyad*", "Email*", "Telefon*",
                "Personel No", "Sicil No", "Departman", "Pozisyon",
                "Maaş", "Para Birimi", "İşe Başlama", "Yönetici Email"
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
                "Ahmet", "Yılmaz", "ahmet.yilmaz@example.com", "0532 123 45 67",
                "P001", "S001", "İK", "Uzman", "45000", "TRY",
                "01.01.2020", "ali.veli@example.com"
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
                "• Ad, Soyad, Email, Telefon ZORUNLUDUR",
                "• Personel No ve Sicil No boş geçilebilir, benzersiz olmalıdır",
                "• Departman ve Pozisyon sistemdeki isimlerle birebir aynı olmalı",
                "• Maaş sayısal değer olmalıdır (örn: 45000)",
                "• Para Birimi: TRY, USD, EUR, GBP",
                "• Tarih formatı: GG.AA.YYYY (örn: 01.01.2020)",
                "• Yönetici Email sistemde kayıtlı personele ait olmalı"
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

                    // =====  SADECE VERİ SATIRLARINI KİLİTSİZ YAP =====
                    for (int row = dataRow; row <= 1000; row++)
                    {
                        for (int cellCol = 1; cellCol <= 12; cellCol++)
                        {
                            worksheet.Cell(row, cellCol).Style.Protection.Locked = false;
                        }
                    }

                    // =====  KORUMA =====
                    worksheet.Protect("TemplateProtection");

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var bytes = stream.ToArray();
                        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Personel_Toplu_Yukleme_Sablonu.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/personels/download-template",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Şablon oluşturulamadı: {ex.Message}" });
            }
        }


        // ========== 2. EXCEL'DEN TOPLU PERSONEL YÜKLEME ==========
        [HttpPost("upload-excel")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadExcel(IFormFile file, [FromQuery] string uploadId)
        {

           
            Console.WriteLine($" TEST BAŞLADI - uploadId: {uploadId}");

            var testProgress = new BulkUploadProgressDto
            {
                UploadId = uploadId ?? "test-123",
                CurrentRow = 3,
                TotalRows = 10,
                CurrentEmail = "TEST@TEST.COM",
                Status = "Processing",
                Percentage = 30
            };


            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Dosya seçilmedi" });

                var excelData = await ReadExcelFileAsync(file);

                if (excelData == null || excelData.Count == 0)
                    return BadRequest(new { message = "Excel'de veri bulunamadı" });

                int totalRows = excelData.Count;
                Console.WriteLine($"📊 TOPLAM SATIR: {totalRows}");

                // ===== PROGRESS =====
                var progress = new Progress<BulkUploadProgressDto>(report =>
                {
                    report.UploadId = uploadId;
                    report.TotalRows = totalRows;
                    Console.WriteLine($"📊 Progress: {report.CurrentRow}/{report.TotalRows} - %{report.Percentage}");
                    _notificationService.SendUploadProgressAsync(report).Wait();
                });

                // ===== uploadId'yi gönder =====
                var result = await ProcessBulkPersonelImportAsync(excelData, uploadId, progress);

                // ===== TAMAMLANDI =====
                await _notificationService.SendUploadProgressAsync(new BulkUploadProgressDto
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

                await _notificationService.SendUploadProgressAsync(new BulkUploadProgressDto
                {
                    UploadId = uploadId,
                    Status = "Error",
                    CurrentEmail = ex.Message
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


        // ========== 3. YARDIMCI METOTLAR ==========

        /// <summary>
        /// Excel dosyasını okur ve PersonelExcelDto listesine dönüştürür
        /// </summary>
        private async Task<List<PersonelExcelDto>> ReadExcelFileAsync(IFormFile file)
        {
            var result = new List<PersonelExcelDto>();

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

                    // ===== HEADER SATIRI (5. satır) =====
                    var headerRow = rows[4]; // Index 4 = 5. satır

                    // ===== KOLON İNDEKSLERİNİ BUL =====
                    int colAd = 1, colSoyad = 2, colEmail = 3, colTelefon = 4;
                    int colPersonelNo = 5, colSicilNo = 6, colDepartman = 7, colPozisyon = 8;
                    int colMaas = 9, colParaBirimi = 10, colIseBaslama = 11, colYoneticiEmail = 12;

                    for (int i = 1; i <= 12; i++)
                    {
                        var cellValue = headerRow.Cell(i).GetString();
                        if (string.IsNullOrEmpty(cellValue)) continue;

                        if (cellValue.Contains("Ad") && cellValue.Contains("*")) colAd = i;
                        else if (cellValue.Contains("Soyad") && cellValue.Contains("*")) colSoyad = i;
                        else if (cellValue.Contains("Email") && cellValue.Contains("*")) colEmail = i;
                        else if (cellValue.Contains("Telefon") && cellValue.Contains("*")) colTelefon = i;
                        else if (cellValue.Contains("Personel No")) colPersonelNo = i;
                        else if (cellValue.Contains("Sicil No")) colSicilNo = i;
                        else if (cellValue.Contains("Departman")) colDepartman = i;
                        else if (cellValue.Contains("Pozisyon")) colPozisyon = i;
                        else if (cellValue.Contains("Maaş")) colMaas = i;
                        else if (cellValue.Contains("Para Birimi")) colParaBirimi = i;
                        else if (cellValue.Contains("İşe Başlama")) colIseBaslama = i;
                        else if (cellValue.Contains("Yönetici Email")) colYoneticiEmail = i;
                    }

                    // ===== VERİ SATIRLARINI OKU (6. satırdan itibaren) =====
                    for (int i = 5; i < rows.Count; i++) // Index 5 = 6. satır
                    {
                        var row = rows[i];
                        var firstCell = row.Cell(1).GetString();

                        // Boş satır kontrolü
                        if (string.IsNullOrWhiteSpace(firstCell))
                            continue;

                        var dto = new PersonelExcelDto
                        {
                            FirstName = row.Cell(colAd).GetString().Trim(),
                            LastName = row.Cell(colSoyad).GetString().Trim(),
                            Email = row.Cell(colEmail).GetString().Trim(),
                            Phone = row.Cell(colTelefon).GetString().Trim(),
                            PersonnelNumber = row.Cell(colPersonelNo).GetString().Trim(),
                            RegistrationNumber = row.Cell(colSicilNo).GetString().Trim(),
                            DepartmentName = row.Cell(colDepartman).GetString().Trim(),
                            PositionName = row.Cell(colPozisyon).GetString().Trim(),
                            Currency = row.Cell(colParaBirimi).GetString().Trim(),
                            ManagerEmail = row.Cell(colYoneticiEmail).GetString().Trim()
                        };

                        // ===== MAAŞ (sayısal) =====
                        var salaryStr = row.Cell(colMaas).GetString().Trim();
                        if (!string.IsNullOrEmpty(salaryStr) && decimal.TryParse(salaryStr, out decimal salary))
                        {
                            dto.Salary = salary;
                        }

                        // ===== İŞE BAŞLAMA (tarih) =====
                        var hireDateStr = row.Cell(colIseBaslama).GetString().Trim();
                        if (!string.IsNullOrEmpty(hireDateStr))
                        {
                            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
                            if (DateTime.TryParseExact(hireDateStr, formats, null, System.Globalization.DateTimeStyles.None, out DateTime hireDate))
                            {
                                dto.HireDate = hireDate;
                            }
                        }

                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        private string GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string key)
        {
            if (columnMap.TryGetValue(key, out int col))
            {
                return row.Cell(col).GetString().Trim();
            }
            return null;
        }


        /// <summary>
        /// Excel'den okunan verileri veritabanına toplu olarak ekler
        /// </summary>
        // ===== PROGRESS İLE TOPLU YÜKLEME =====
        private async Task<BulkUploadResultDto> ProcessBulkPersonelImportAsync(List<PersonelExcelDto> excelData,string uploadId,  IProgress<BulkUploadProgressDto> progress = null)
        {
            var result = new BulkUploadResultDto
            {
                TotalRows = excelData.Count
            };

            try
            {
                // ===== MEVCUT VERİLER =====
                var departments = await _unitOfWork.Query<Department>().ToListAsync();
                var positions = await _unitOfWork.Query<Position>().ToListAsync();
                var existingPersonels = await _unitOfWork.Query<Personel>().IgnoreQueryFilters().ToListAsync();

                var existingEmails = existingPersonels.Select(p => p.Email.ToLowerInvariant()).ToHashSet();
                var existingPersonnelNumbers = existingPersonels.Where(p => !string.IsNullOrEmpty(p.PersonnelNumber)).Select(p => p.PersonnelNumber.ToLowerInvariant()).ToHashSet();
                var existingRegistrationNumbers = existingPersonels.Where(p => !string.IsNullOrEmpty(p.RegistrationNumber)).Select(p => p.RegistrationNumber.ToLowerInvariant()).ToHashSet();
                var personelByEmail = existingPersonels.Where(p => !string.IsNullOrEmpty(p.Email)).ToDictionary(p => p.Email.ToLowerInvariant(), p => p.Id);
                var departmentDict = departments.GroupBy(d => d.Name.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First().Id);
                var positionDict = positions.GroupBy(p => p.Name.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First().Id);

                // ===== VALIDASYON =====
                var validPersonels = new List<CreatePersonelDto>();
                var errors = new List<BulkUploadErrorDto>();
                var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processedPersonnelNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processedRegistrationNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < excelData.Count; i++)
                {
                    var row = excelData[i];
                    var rowNumber = i + 6;
                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(row.FirstName)) rowErrors.Add("Ad zorunludur");
                    if (string.IsNullOrWhiteSpace(row.LastName)) rowErrors.Add("Soyad zorunludur");
                    if (string.IsNullOrWhiteSpace(row.Email)) rowErrors.Add("Email zorunludur");
                    else if (!IsValidEmail(row.Email)) rowErrors.Add("Geçersiz email formatı");
                    if (string.IsNullOrWhiteSpace(row.Phone)) rowErrors.Add("Telefon zorunludur");

                    if (!string.IsNullOrEmpty(row.Email))
                    {
                        var emailKey = row.Email.ToLowerInvariant();
                        if (existingEmails.Contains(emailKey)) rowErrors.Add($"Email '{row.Email}' sistemde zaten kayıtlı");
                        if (processedEmails.Contains(emailKey)) rowErrors.Add($"Email '{row.Email}' bu dosyada tekrar ediyor");
                        else processedEmails.Add(emailKey);
                    }

                    if (!string.IsNullOrEmpty(row.PersonnelNumber))
                    {
                        var pNoKey = row.PersonnelNumber.ToLowerInvariant();
                        if (existingPersonnelNumbers.Contains(pNoKey)) rowErrors.Add($"Personel No '{row.PersonnelNumber}' sistemde zaten kayıtlı");
                        if (processedPersonnelNumbers.Contains(pNoKey)) rowErrors.Add($"Personel No '{row.PersonnelNumber}' bu dosyada tekrar ediyor");
                        else processedPersonnelNumbers.Add(pNoKey);
                    }

                    if (!string.IsNullOrEmpty(row.RegistrationNumber))
                    {
                        var rNoKey = row.RegistrationNumber.ToLowerInvariant();
                        if (existingRegistrationNumbers.Contains(rNoKey)) rowErrors.Add($"Sicil No '{row.RegistrationNumber}' sistemde zaten kayıtlı");
                        if (processedRegistrationNumbers.Contains(rNoKey)) rowErrors.Add($"Sicil No '{row.RegistrationNumber}' bu dosyada tekrar ediyor");
                        else processedRegistrationNumbers.Add(rNoKey);
                    }

                    int? departmentId = null;
                    if (!string.IsNullOrEmpty(row.DepartmentName))
                    {
                        var deptKey = row.DepartmentName.Trim().ToLowerInvariant();
                        if (departmentDict.TryGetValue(deptKey, out int deptId)) departmentId = deptId;
                        else rowErrors.Add($"Departman '{row.DepartmentName}' sistemde bulunamadı");
                    }

                    int? positionId = null;
                    if (!string.IsNullOrEmpty(row.PositionName))
                    {
                        var posKey = row.PositionName.Trim().ToLowerInvariant();
                        if (positionDict.TryGetValue(posKey, out int posId)) positionId = posId;
                        else rowErrors.Add($"Pozisyon '{row.PositionName}' sistemde bulunamadı");
                    }

                    if (!string.IsNullOrEmpty(row.Currency))
                    {
                        var validCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };
                        if (!validCurrencies.Contains(row.Currency.ToUpper())) rowErrors.Add($"Para birimi '{row.Currency}' geçersiz");
                    }

                    int? managerId = null;
                    if (!string.IsNullOrEmpty(row.ManagerEmail))
                    {
                        var managerKey = row.ManagerEmail.ToLowerInvariant();
                        if (personelByEmail.TryGetValue(managerKey, out int mId)) managerId = mId;
                        else rowErrors.Add($"Yönetici Email '{row.ManagerEmail}' sistemde bulunamadı");
                    }

                    if (row.HireDate.HasValue && row.HireDate > DateTime.Now) rowErrors.Add("İşe başlama tarihi gelecek tarih olamaz");

                    if (rowErrors.Any())
                    {
                        errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            Email = row.Email,
                            ErrorMessage = string.Join(" | ", rowErrors)
                        });
                        continue;
                    }

                    validPersonels.Add(new CreatePersonelDto
                    {
                        FirstName = row.FirstName.Trim(),
                        LastName = row.LastName.Trim(),
                        Email = row.Email.Trim().ToLower(),
                        Phone = row.Phone.Trim(),
                        PersonnelNumber = string.IsNullOrEmpty(row.PersonnelNumber) ? null : row.PersonnelNumber.Trim(),
                        RegistrationNumber = string.IsNullOrEmpty(row.RegistrationNumber) ? null : row.RegistrationNumber.Trim(),
                        DepartmentId = departmentId,
                        PositionId = positionId,
                        Salary = row.Salary,
                        Currency = string.IsNullOrEmpty(row.Currency) ? "TRY" : row.Currency.ToUpper(),
                        HireDate = row.HireDate,
                        ManagerId = managerId,
                        CreateUser = false
                    });
                }

                result.Errors = errors;
                result.ErrorCount = errors.Count;

                // ===== KAYDET =====
                if (validPersonels.Any())
                {
                    int totalValid = validPersonels.Count;

                    // =====  BAŞLANGIÇ PROGRESS =====
                    progress?.Report(new BulkUploadProgressDto
                    {
                        UploadId = uploadId,  // ← EKLENDI
                        CurrentRow = 0,
                        TotalRows = totalValid,
                        CurrentEmail = "Başlatılıyor...",
                        Status = "Processing",
                        Percentage = 0
                    });

                    for (int index = 0; index < validPersonels.Count; index++)
                    {
                        var dto = validPersonels[index];
                        var percent = (int)((index + 1) * 100.0 / totalValid);

                        // =====  HER SATIR PROGRESS =====
                        progress?.Report(new BulkUploadProgressDto
                        {
                            UploadId = uploadId,  // ← EKLENDI
                            CurrentRow = index + 1,
                            TotalRows = totalValid,
                            CurrentEmail = dto.Email ?? "İşleniyor...",
                            Status = "Processing",
                            Percentage = percent
                        });

                        try
                        {
                            var personel = new Personel
                            {
                                FirstName = dto.FirstName ?? "Bilinmeyen",
                                LastName = dto.LastName ?? "Bilinmeyen",
                                Email = dto.Email ?? "unknown@example.com",
                                Phone = dto.Phone ?? "000-000-0000",
                                PersonnelNumber = dto.PersonnelNumber,
                                RegistrationNumber = dto.RegistrationNumber,
                                DepartmentId = dto.DepartmentId,
                                PositionId = dto.PositionId,
                                Salary = dto.Salary,
                                Currency = dto.Currency,
                                HireDate = dto.HireDate,
                                ManagerId = dto.ManagerId,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            await _unitOfWork.AddAsync(personel);
                            await _unitOfWork.CompleteAsync();

                            result.SuccessCount++;
                            result.CreatedPersonels.Add(_mapper.Map<PersonelDto>(personel));
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            result.Errors.Add(new BulkUploadErrorDto
                            {
                                RowNumber = index + 1,
                                Email = dto.Email,
                                ErrorMessage = $"Kayıt hatası: {ex.InnerException?.Message ?? ex.Message}"
                            });
                        }
                    }

                    // =====  TAMAMLANDI PROGRESS =====
                    progress?.Report(new BulkUploadProgressDto
                    {
                        UploadId = uploadId,  // ← EKLENDI
                        CurrentRow = totalValid,
                        TotalRows = totalValid,
                        CurrentEmail = "Tamamlandı! 🎉",
                        Status = "Completed",
                        Percentage = 100
                    });
                }
                else
                {
                    progress?.Report(new BulkUploadProgressDto
                    {
                        UploadId = uploadId,  // ← EKLENDI
                        CurrentRow = 0,
                        TotalRows = 0,
                        CurrentEmail = "Geçerli personel yok",
                        Status = "Completed",
                        Percentage = 100
                    });
                }
            }
            catch (Exception ex)
            {
                // =====  HATA PROGRESS =====
                progress?.Report(new BulkUploadProgressDto
                {
                    UploadId = uploadId,  
                    Status = "Error",
                    CurrentEmail = ex.Message
                });

                result.Errors.Add(new BulkUploadErrorDto
                {
                    RowNumber = 0,
                    Email = "SİSTEM",
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