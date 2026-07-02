using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Task;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Validators.TaskValidator;
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
    public class TasksController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TasksController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _hubContext = hubContext;
        }

        // GET: api/tasks/personels
        [HttpGet("personels")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPersonels()
        {
            try
            {
                var personels = await _unitOfWork.Query<Personel>()
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.FirstName)
                    .Select(p => new { p.Id, p.FirstName, p.LastName })
                    .ToListAsync();

                return Ok(personels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/tasks/customers
        [HttpGet("customers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _unitOfWork.Query<Customer>()
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .OrderBy(c => c.FirstName)
                    .Select(c => new { c.Id, c.FirstName, c.LastName, c.CompanyName })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/tasks/leads
        [HttpGet("leads")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeads()
        {
            try
            {
                var leads = await _unitOfWork.Query<Lead>()
                    .Where(l => !l.IsDeleted)
                    .OrderBy(l => l.CompanyName)
                    .Select(l => new { l.Id, l.CompanyName })
                    .ToListAsync();

                return Ok(leads);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/tasks/opportunities
        [HttpGet("opportunities")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOpportunities()
        {
            try
            {
                var opportunities = await _unitOfWork.Query<Opportunity>()
                    .Where(o => !o.IsDeleted)
                    .OrderBy(o => o.Name)
                    .Select(o => new { o.Id, o.Name })
                    .ToListAsync();

                return Ok(opportunities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/tasks
        [HttpGet]
       // [HasPermission("task.view")]
        public async Task<IActionResult> GetAll([FromQuery] TaskPaginationDto pagination)
        {
            try
            {
                var validator = new TaskPaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var query = _unitOfWork.Query<DomainTask>()
                    .Include(t => t.AssignedToPersonel)
                    .Include(t => t.RelatedToCustomer)
                    .Include(t => t.RelatedToLead)
                    .Include(t => t.RelatedToOpportunity)
                    .Include(t => t.CreatedByPersonel)
                    .AsQueryable();

                // Arama
                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(t =>
                        t.Title.Contains(pagination.Search) ||
                        t.Description.Contains(pagination.Search));
                }

                // Filtreler
                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(t => t.Status == pagination.Status);

                if (!string.IsNullOrEmpty(pagination.Priority))
                    query = query.Where(t => t.Priority == pagination.Priority);

                if (pagination.AssignedToPersonelId.HasValue)
                    query = query.Where(t => t.AssignedToPersonelId == pagination.AssignedToPersonelId);

                if (pagination.RelatedToCustomerId.HasValue)
                    query = query.Where(t => t.RelatedToCustomerId == pagination.RelatedToCustomerId);

                if (pagination.StartDate.HasValue)
                    query = query.Where(t => t.DueDate >= pagination.StartDate);

                if (pagination.EndDate.HasValue)
                    query = query.Where(t => t.DueDate <= pagination.EndDate.Value.Date.AddDays(1));

                var totalCount = await query.CountAsync();
                var tasks = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var taskDtos = _mapper.Map<List<TaskDto>>(tasks);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Task",
                    AdditionalInfo = $"Görev listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    Data = taskDtos,
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
                    RequestPath = "/api/tasks",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
       // [HasPermission("task.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var task = await _unitOfWork.Query<DomainTask>()
                    .Include(t => t.AssignedToPersonel)
                    .Include(t => t.RelatedToCustomer)
                    .Include(t => t.RelatedToLead)
                    .Include(t => t.RelatedToOpportunity)
                    .Include(t => t.CreatedByPersonel)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { message = "Görev bulunamadı" });

                var taskDto = _mapper.Map<TaskDto>(task);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Task",
                    EntityId = id,
                    AdditionalInfo = $"Görev detayı görüntülendi: {task.Title}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tasks/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/tasks
        [HttpPost]
      //  [HasPermission("task.create")]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Görev bilgileri eksik" });

                var validator = new CreateTaskValidator();
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

                var task = _mapper.Map<DomainTask>(request);
                task.CreatedAt = DateTime.UtcNow;
                task.CreatedByPersonelId = currentPersonelId.Value;

                await _unitOfWork.AddAsync(task);
                await _unitOfWork.CompleteAsync();

                var createdTask = await _unitOfWork.Query<DomainTask>()
                    .Include(t => t.AssignedToPersonel)
                    .Include(t => t.RelatedToCustomer)
                    .Include(t => t.RelatedToLead)
                    .Include(t => t.RelatedToOpportunity)
                    .Include(t => t.CreatedByPersonel)
                    .FirstOrDefaultAsync(t => t.Id == task.Id);

                var taskDto = _mapper.Map<TaskDto>(createdTask);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Task",
                    EntityId = task.Id,
                    AdditionalInfo = $"Yeni görev oluşturuldu: {task.Title}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Görev",
                    Message = $"{task.Title} görevi oluşturuldu",
                    Type = "TaskCreated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshTasks");

                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/tasks",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
      //  [HasPermission("task.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto request)
        {
            try
            {
                var validator = new UpdateTaskValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                if (request == null)
                    return BadRequest(new { message = "Görev bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var task = await _unitOfWork.Query<DomainTask>()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { message = "Görev bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                // Sadece oluşturan kişi düzenleyebilir
                if (task.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece görevi oluşturan kişi düzenleyebilir." });
                }

                var oldTitle = task.Title;
                var oldStatus = task.Status;

                _mapper.Map(request, task);
                task.UpdatedAt = DateTime.UtcNow;

                // Status "Tamamlandı" ise CompletedAt'i güncelle
                if (task.Status == "Tamamlandı")
                {
                    task.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    task.CompletedAt = null;
                }

                _unitOfWork.Update(task);
                await _unitOfWork.CompleteAsync();

                var updatedTask = await _unitOfWork.Query<DomainTask>()
                    .Include(t => t.AssignedToPersonel)
                    .Include(t => t.RelatedToCustomer)
                    .Include(t => t.RelatedToLead)
                    .Include(t => t.RelatedToOpportunity)
                    .Include(t => t.CreatedByPersonel)
                    .FirstOrDefaultAsync(t => t.Id == task.Id);

                var taskDto = _mapper.Map<TaskDto>(updatedTask);

                // Action Log
                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Task",
                    EntityId = id,
                    AdditionalInfo = $"Görev güncellendi: {oldTitle} -> {task.Title}, Durum: {oldStatus} -> {task.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                // SignalR bildirimi
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Görev Güncellendi",
                    Message = $"{task.Title} görevi güncellendi",
                    Type = "TaskUpdated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshTasks");

                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tasks/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
      //  [HasPermission("task.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var task = await _unitOfWork.Query<DomainTask>()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return NotFound(new { message = "Görev bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (task.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece görevi oluşturan kişi silebilir." });
                }

                _unitOfWork.Delete(task);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Task",
                    EntityId = id,
                    AdditionalInfo = $"Görev silindi: {task.Title}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Görev Silindi",
                    Message = $"{task.Title} görevi silindi",
                    Type = "TaskDeleted",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshTasks");

                return Ok(new { message = "Görev başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/tasks/{id}",
                    RequestMethod = "DELETE",
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
