using Crm.API.Attributes;
using Crm.Application.DTOs.Exchange;
using Crm.Domain.Entities;
using Crm.Infrastructure.Helpers;
using Crm.Infrastructure.Repositories;
using Crm.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    //[Route("api/ExchangeRate")]
    [ApiController]
    [Authorize]
    public class ExchangeRateSettingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly IMemoryCache _cache;
        private readonly IExchangeRateService _exchangeRateService;

        public ExchangeRateSettingsController(IUnitOfWork unitOfWork, ILogService logService, IMemoryCache cache, IExchangeRateService exchangeRateService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
            _cache = cache;
            _exchangeRateService = exchangeRateService;
        }

        // ========== GET ALL ==========
        [HttpGet]
        [HasPermission("system.admin")]
        public async Task<IActionResult> GetAll()
        {
            var settings = await _unitOfWork.Query<ExchangeRateSetting>()
                .OrderBy(x => x.Priority)
                .ToListAsync();

            return Ok(settings);
        }

        // ========== GET ACTIVE ==========
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var active = await _unitOfWork.Query<ExchangeRateSetting>()
                .FirstOrDefaultAsync(x => x.IsActive);

            return Ok(active);
        }

        // ========== GET BY ID ==========
        [HttpGet("{id}")]
        [HasPermission("system.admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var setting = await _unitOfWork.GetByIdAsync<ExchangeRateSetting>(id);
            if (setting == null)
                return NotFound(new { message = "Provider bulunamadı" });

            return Ok(setting);
        }

        // ========== CREATE ==========
        [HttpPost]
        [HasPermission("system.admin")]
        public async Task<IActionResult> Create([FromBody] CreateExchangeRateSettingDto request)
        {
            try
            {
                // Aynı provider var mı kontrol et
                var exists = await _unitOfWork.Query<ExchangeRateSetting>()
                    .AnyAsync(x => x.Provider == request.Provider);

                if (exists)
                    return BadRequest(new { message = "Bu provider zaten kayıtlı" });

                var setting = new ExchangeRateSetting
                {
                    Provider = request.Provider,
                    Name = request.Name,
                    ApiUrl = request.ApiUrl,
                    ApiKey = request.ApiKey,
                    IsActive = false,
                    Priority = request.Priority,
                    CacheDurationMinutes = request.CacheDurationMinutes ?? 60,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AddAsync(setting);
                await _unitOfWork.CompleteAsync();

                _cache.Remove("active_exchange_rate_provider");

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "CREATE",
                    EntityType = "ExchangeRateSetting",
                    EntityId = setting.Id,
                    AdditionalInfo = $"Yeni kur servisi eklendi: {setting.Name} ({setting.Provider})",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(setting);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/exchangeratesettings",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== UPDATE ==========
        [HttpPut("{id}")]
        [HasPermission("system.admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExchangeRateSettingDto request)
        {
            try
            {
                var setting = await _unitOfWork.GetByIdAsync<ExchangeRateSetting>(id);
                if (setting == null)
                    return NotFound(new { message = "Provider bulunamadı" });

                if (!string.IsNullOrEmpty(request.Name))
                    setting.Name = request.Name;

                if (!string.IsNullOrEmpty(request.ApiUrl))
                    setting.ApiUrl = request.ApiUrl;

                if (request.ApiKey != null)
                    setting.ApiKey = request.ApiKey;

                if (request.Priority.HasValue)
                    setting.Priority = request.Priority.Value;

                if (request.CacheDurationMinutes.HasValue)
                    setting.CacheDurationMinutes = request.CacheDurationMinutes.Value;

                setting.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Update(setting);
                await _unitOfWork.CompleteAsync();

                _cache.Remove("active_exchange_rate_provider");

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "UPDATE",
                    EntityType = "ExchangeRateSetting",
                    EntityId = id,
                    AdditionalInfo = $"Kur servisi güncellendi: {setting.Name}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(setting);
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/exchangeratesettings/{id}",
                    RequestMethod = "PUT",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // ========== DELETE ==========
        [HttpDelete("{id}")]
        [HasPermission("system.admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var setting = await _unitOfWork.GetByIdAsync<ExchangeRateSetting>(id);
                if (setting == null)
                    return NotFound(new { message = "Provider bulunamadı" });

                if (setting.IsActive)
                    return BadRequest(new { message = "Aktif olan bir kur servisi silinemez. Önce başka bir servisi aktif edin." });

                var settingName = setting.Name;

                _unitOfWork.Delete(setting);
                await _unitOfWork.CompleteAsync();

                _cache.Remove("active_exchange_rate_provider");

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "DELETE",
                    EntityType = "ExchangeRateSetting",
                    EntityId = id,
                    AdditionalInfo = $"Kur servisi silindi: {settingName}",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Kur servisi silindi" });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = $"/api/exchangeratesettings/{id}",
                    RequestMethod = "DELETE",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }


        // ========== SWITCH PROVIDER ==========
        [HttpPost("switch/{id}")]
        [HasPermission("system.admin")]
        public async Task<IActionResult> SwitchProvider(int id)
        {
            try
            {
                var allSettings = await _unitOfWork.Query<ExchangeRateSetting>().ToListAsync();
                foreach (var setting in allSettings)
                {
                    setting.IsActive = false;
                    _unitOfWork.Update(setting);
                }

                var selected = await _unitOfWork.GetByIdAsync<ExchangeRateSetting>(id);
                if (selected == null)
                    return NotFound(new { message = "Provider bulunamadı" });

                selected.IsActive = true;
                selected.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Update(selected);

                await _unitOfWork.CompleteAsync();

                // 1. Aktif sağlayıcı önbelleğini temizle
                _cache.Remove("active_exchange_rate_provider");

              
                _cache.Remove("exchange_rate_USD_to_TRY");
                _cache.Remove("exchange_rate_EUR_to_TRY");
                _cache.Remove("exchange_rate_GBP_to_TRY");

                // Küçük harf veya farklı kombinasyon ihtimallerine karşı garantiye alma
                _cache.Remove("exchange_rate_USD_TRY");
                _cache.Remove("exchange_rate_EUR_TRY");
                _cache.Remove("exchange_rate_GBP_TRY");

                // Eğer altyapıda TCMB'nin ham XML verisi cache'leniyorsa  temizle
                _cache.Remove("tcmb_rates");

                await _logService.LogActionAsync(new ActionLog
                {
                    ActionType = "SWITCH_PROVIDER",
                    EntityType = "ExchangeRateSetting",
                    EntityId = id,
                    AdditionalInfo = $"Kur servisi değiştirildi ve eski cache'ler temizlendi: {selected.Name} ({selected.Provider})",
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(new { message = $"Kur servisi başarıyla değiştirildi ve önbellek temizlendi", provider = selected });
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/exchangeratesettings/switch",
                    RequestMethod = "POST",
                    IpAddress = HttpContextHelper.GetClientIp(HttpContext),
                    UserId = HttpContextHelper.GetCurrentUserId(HttpContext),
                    ErrorLevel = "Error",
                    CreatedAt = DateTime.UtcNow
                });
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

       
        // GET: api/exchangeratesettings/rate/USD/TRY
        [HttpGet("rate/{fromCurrency}/{toCurrency}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRate(string fromCurrency, string toCurrency)
        {
            try
            {
                // 1. Servisten kuru çekiyoruz (Eğer API patlarsa servis arkada default 35, 38, 44 dönecek)
                var rate = await _exchangeRateService.GetRateAsync(fromCurrency, toCurrency);

                // 2. Gelen değerin sistemin default (fallback) değeri olup olmadığını kontrol ediyoruz
                bool isDefaultValue = (fromCurrency == "USD" && rate == 35.00m) ||
                                      (fromCurrency == "EUR" && rate == 38.00m) ||
                                      (fromCurrency == "GBP" && rate == 44.00m);

                // 3. Kullanıcıya hem kuru hem de durumu bildiren net bir obje dönüyoruz
                return Ok(new
                {
                    rate = rate,
                    isDefault = isDefaultValue,
                    message = isDefaultValue
                        ? $"Seçili API servisinde hata oluştu. Sistemde kayıtlı varsayılan (default) değer ({rate}) getirildi."
                        : "Canlı kur verisi başarıyla çekildi."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        // GET: api/exchangerate/convert
        [HttpGet("convert")]
        [AllowAnonymous]
        public async Task<IActionResult> Convert([FromQuery] decimal amount, [FromQuery] string from, [FromQuery] string to)
        {
            try
            {
                var result = await _exchangeRateService.ConvertAsync(amount, from, to);
                return Ok(new { amount, from, to, result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ========== GET ALL LIVE RATES ==========
        // GET: api/exchangeratesettings/all-rates
        [HttpGet("all-rates")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetAllRates()
        {
            try
            {
                
                var ratesResult = new Dictionary<string, decimal>
                {
                    { "TRY", 1.00m } // Baz para birimi TRY her zaman 1'dir.
                };

             
                var usdRate = await _exchangeRateService.GetRateAsync("USD", "TRY");
                var eurRate = await _exchangeRateService.GetRateAsync("EUR", "TRY");
                var gbpRate = await _exchangeRateService.GetRateAsync("GBP", "TRY");

                ratesResult.Add("USD", usdRate);
                ratesResult.Add("EUR", eurRate);
                ratesResult.Add("GBP", gbpRate);

                return Ok(ratesResult);
            }
            catch (Exception ex)
            {
             
                var fallbackRates = new Dictionary<string, decimal>
                {
                    { "TRY", 1.00m },
                    { "USD", 35.00m },
                    { "EUR", 38.00m },
                    { "GBP", 44.00m }
                };

              
                await _logService.LogErrorAsync(new ErrorLog
                {
                    ErrorMessage = $"Toplu kur çekme hatası: {ex.Message}",
                    StackTrace = ex.StackTrace,
                    RequestPath = "/api/exchangeratesettings/all-rates",
                    RequestMethod = "GET",
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(fallbackRates);
            }
        }
    }
}