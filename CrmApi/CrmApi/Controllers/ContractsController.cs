using AutoMapper;
using Crm.API.Attributes;
using Crm.Application.DTOs.Contract;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using CrmApi.Hubs;
using CrmApi.Validators.ContractValidator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ContractsController(IUnitOfWork unitOfWork, IMapper mapper, ILogService logService, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logService = logService;
            _hubContext = hubContext;
        }

        // GET: api/contracts/customers
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

        // GET: api/contracts
        [HttpGet]
        [HasPermission("contract.view")]
        public async Task<IActionResult> GetAll([FromQuery] ContractPaginationDto pagination)
        {
            try
            {
                var validator = new ContractPaginationValidator();
                var validationResult = await validator.ValidateAsync(pagination);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                var query = _unitOfWork.Query<Contract>()
                    .Include(c => c.Customer)
                    .Include(c => c.CreatedByPersonel)
                    .Include(c => c.Quote)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(pagination.Search))
                {
                    query = query.Where(c =>
                        c.ContractNumber.Contains(pagination.Search) ||
                        c.Title.Contains(pagination.Search) ||
                        c.Customer.FirstName.Contains(pagination.Search) ||
                        c.Customer.LastName.Contains(pagination.Search));
                }

                if (!string.IsNullOrEmpty(pagination.Status))
                    query = query.Where(c => c.Status == pagination.Status);

                if (pagination.CustomerId.HasValue)
                    query = query.Where(c => c.CustomerId == pagination.CustomerId);

                if (pagination.StartDate.HasValue)
                    query = query.Where(c => c.StartDate >= pagination.StartDate);

                if (pagination.EndDate.HasValue)
                    query = query.Where(c => c.EndDate <= pagination.EndDate.Value.Date.AddDays(1));

                var totalCount = await query.CountAsync();
                var contracts = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var contractDtos = _mapper.Map<List<ContractDto>>(contracts);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Contract",
                    AdditionalInfo = $"Sözleşme listesi görüntülendi. Sayfa: {pagination.Page}, Toplam: {totalCount}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new
                {
                    Data = contractDtos,
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
                    RequestPath = "/api/contracts",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/contracts/{id}
        [HttpGet("{id}")]
        [HasPermission("contract.view")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var contract = await _unitOfWork.Query<Contract>()
                    .Include(c => c.Customer)
                    .Include(c => c.CreatedByPersonel)
                    .Include(c => c.Quote)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                    return NotFound(new { message = "Sözleşme bulunamadı" });

                var contractDto = _mapper.Map<ContractDto>(contract);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "VIEW",
                    EntityType = "Contract",
                    EntityId = id,
                    AdditionalInfo = $"Sözleşme detayı görüntülendi: {contract.ContractNumber}",
                    UserId = GetCurrentPersonelId() ?? 0,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(contractDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/contracts/{id}",
                    RequestMethod = "GET",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/contracts
        [HttpPost]
        [HasPermission("contract.create")]
        public async Task<IActionResult> Create([FromBody] CreateContractDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Sözleşme bilgileri eksik" });

                var validator = new CreateContractValidator();
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

                var contract = _mapper.Map<Contract>(request);

                var year = DateTime.UtcNow.Year;
                var lastContract = await _unitOfWork.Query<Contract>()
                    .Where(c => c.ContractNumber.StartsWith($"CON-{year}"))
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                int lastNumber = 0;
                if (lastContract != null)
                {
                    var lastNumberStr = lastContract.ContractNumber.Split('-')[2];
                    lastNumber = int.Parse(lastNumberStr);
                }
                contract.ContractNumber = $"CON-{year}-{(lastNumber + 1).ToString("D6")}";

                contract.CreatedAt = DateTime.UtcNow;
                contract.CreatedByPersonelId = currentPersonelId.Value;
                contract.IsSigned = false;

                await _unitOfWork.AddAsync(contract);
                await _unitOfWork.CompleteAsync();

                var createdContract = await _unitOfWork.Query<Contract>()
                    .Include(c => c.Customer)
                    .Include(c => c.CreatedByPersonel)
                    .Include(c => c.Quote)
                    .FirstOrDefaultAsync(c => c.Id == contract.Id);

                var contractDto = _mapper.Map<ContractDto>(createdContract);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "Contract",
                    EntityId = contract.Id,
                    AdditionalInfo = $"Yeni sözleşme oluşturuldu: {contract.ContractNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Yeni Sözleşme",
                    Message = $"{contract.ContractNumber} numaralı sözleşme oluşturuldu",
                    Type = "ContractCreated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshContracts");

                return Ok(contractDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/contracts",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // PUT: api/contracts/{id}
        [HttpPut("{id}")]
        [HasPermission("contract.edit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateContractDto request)
        {
            try
            {
                var validator = new UpdateContractValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
                    return BadRequest(new { message = "Doğrulama hatası", errors });
                }

                if (request == null)
                    return BadRequest(new { message = "Sözleşme bilgileri eksik" });

                if (id != request.Id)
                    return BadRequest(new { message = "ID uyuşmazlığı" });

                var contract = await _unitOfWork.Query<Contract>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                    return NotFound(new { message = "Sözleşme bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (contract.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece sözleşmeyi oluşturan kişi düzenleyebilir." });
                }

                var oldContractNumber = contract.ContractNumber;
                var oldStatus = contract.Status;

                _mapper.Map(request, contract);
                contract.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(contract);
                await _unitOfWork.CompleteAsync();

                var updatedContract = await _unitOfWork.Query<Contract>()
                    .Include(c => c.Customer)
                    .Include(c => c.CreatedByPersonel)
                    .Include(c => c.Quote)
                    .FirstOrDefaultAsync(c => c.Id == contract.Id);

                var contractDto = _mapper.Map<ContractDto>(updatedContract);

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "Contract",
                    EntityId = id,
                    AdditionalInfo = $"Sözleşme güncellendi: {oldContractNumber}, Durum: {oldStatus} -> {contract.Status}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Sözleşme Güncellendi",
                    Message = $"{contract.ContractNumber} numaralı sözleşme güncellendi",
                    Type = "ContractUpdated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshContracts");

                return Ok(contractDto);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/contracts/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // DELETE: api/contracts/{id}
        [HttpDelete("{id}")]
        [HasPermission("contract.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var contract = await _unitOfWork.Query<Contract>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                    return NotFound(new { message = "Sözleşme bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (contract.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece sözleşmeyi oluşturan kişi silebilir." });
                }

                if (contract.Status == "Aktif")
                {
                    return BadRequest(new { message = "Aktif sözleşme silinemez. Önce feshedin." });
                }

                _unitOfWork.Delete(contract);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "Contract",
                    EntityId = id,
                    AdditionalInfo = $"Sözleşme silindi: {contract.ContractNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Sözleşme Silindi",
                    Message = $"{contract.ContractNumber} numaralı sözleşme silindi",
                    Type = "ContractDeleted",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshContracts");

                return Ok(new { message = "Sözleşme başarıyla silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/contracts/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = GetCurrentPersonelId(),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // POST: api/contracts/{id}/sign
        [HttpPost("{id}/sign")]
        [HasPermission("contract.edit")]
        public async Task<IActionResult> Sign(int id, [FromBody] SignContractDto request)
        {
            try
            {
                var contract = await _unitOfWork.Query<Contract>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                    return NotFound(new { message = "Sözleşme bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (contract.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece sözleşmeyi oluşturan kişi imzalayabilir." });
                }

                if (contract.IsSigned)
                    return BadRequest(new { message = "Sözleşme zaten imzalanmış." });

                if (contract.Status != "Aktif")
                    return BadRequest(new { message = "Sadece aktif sözleşmeler imzalanabilir." });

                contract.IsSigned = true;
                contract.SignedDate = DateTime.UtcNow;
                contract.SignedBy = request.SignedBy ?? currentPersonelId.Value.ToString();
                contract.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(contract);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "SIGN",
                    EntityType = "Contract",
                    EntityId = id,
                    AdditionalInfo = $"Sözleşme imzalandı: {contract.ContractNumber}, İmzalayan: {contract.SignedBy}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Sözleşme İmzalandı",
                    Message = $"{contract.ContractNumber} numaralı sözleşme imzalandı",
                    Type = "ContractSigned",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshContracts");

                return Ok(new { message = "Sözleşme başarıyla imzalandı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/contracts/{id}/terminate
        [HttpPost("{id}/terminate")]
        [HasPermission("contract.edit")]
        public async Task<IActionResult> Terminate(int id)
        {
            try
            {
                var contract = await _unitOfWork.Query<Contract>()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                    return NotFound(new { message = "Sözleşme bulunamadı" });

                var currentPersonelId = GetCurrentPersonelId();
                if (!currentPersonelId.HasValue)
                    return Unauthorized();

                if (contract.CreatedByPersonelId != currentPersonelId.Value)
                {
                    return StatusCode(403, new { message = "Sadece sözleşmeyi oluşturan kişi feshedebilir." });
                }

                if (contract.Status == "Feshedildi")
                    return BadRequest(new { message = "Sözleşme zaten feshedilmiş." });

                if (contract.Status != "Aktif" && contract.Status != "Bekliyor")
                    return BadRequest(new { message = "Sadece aktif veya bekleyen sözleşmeler feshedilebilir." });

                contract.Status = "Feshedildi";
                contract.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(contract);
                await _unitOfWork.CompleteAsync();

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "TERMINATE",
                    EntityType = "Contract",
                    EntityId = id,
                    AdditionalInfo = $"Sözleşme feshedildi: {contract.ContractNumber}",
                    UserId = currentPersonelId.Value,
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    Title = "Sözleşme Feshedildi",
                    Message = $"{contract.ContractNumber} numaralı sözleşme feshedildi",
                    Type = "ContractTerminated",
                    Timestamp = DateTime.UtcNow
                });
                await _hubContext.Clients.All.SendAsync("RefreshContracts");

                return Ok(new { message = "Sözleşme başarıyla feshedildi" });
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