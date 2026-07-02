

using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Xml;
using System.Text.Json;

namespace Crm.Infrastructure.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ExchangeRateService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ExchangeRateService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<ExchangeRateService> logger,
            IUnitOfWork unitOfWork)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }


  
        private async Task<ExchangeRateSetting> GetActiveProvider()
        {
            var cacheKey = "active_exchange_rate_provider";

            if (_cache.TryGetValue(cacheKey, out ExchangeRateSetting cachedSetting))
                return cachedSetting;

            var setting = await _unitOfWork.Query<ExchangeRateSetting>()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Priority)
                .FirstOrDefaultAsync();

            if (setting != null)
            {
                Console.WriteLine($"✅ Aktif Provider: {setting.Provider} - {setting.ApiUrl}");  
                _cache.Set(cacheKey, setting, TimeSpan.FromMinutes(5));
            }
            else
            {
                Console.WriteLine("⚠️ Aktif provider bulunamadı, varsayılan TCMB kullanılıyor");
            }

            return setting ?? new ExchangeRateSetting
            {
                Provider = "Tcmb",
                ApiUrl = "https://www.tcmb.gov.tr/kurlar/today.xml",
                CacheDurationMinutes = 60
            };
        }

     
        public async Task<decimal> GetRateAsync(string fromCurrency, string toCurrency)
        {
            if (fromCurrency == toCurrency) return 1;

            var fromRate = await GetRateToTry(fromCurrency);  // 1 USD = 35 TL
            var toRate = await GetRateToTry(toCurrency);      // 1 EUR = 38 TL

            var result = fromRate / toRate;  // 35 / 38 = 0.921

            Console.WriteLine($"💱 1 {fromCurrency} = {result} {toCurrency}");
            Console.WriteLine($"   {fromCurrency}->TRY: {fromRate}, {toCurrency}->TRY: {toRate}");

            return result;
        }



        private async Task<decimal> GetRateToTry(string currency)
        {
            if (currency == "TRY") return 1;

            var setting = await GetActiveProvider();

       
            var cacheKey = $"exchange_rate_{currency}_to_try_{setting.Provider?.ToLower()}";

            if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
                return cachedRate;

            try
            {
                decimal rate = 1;

                switch (setting.Provider?.ToLower())
                {
                    case "tcmb":
                        rate = await GetRateFromTcmb(currency);
                        break;
                    case "openexchangerates":
                        rate = await GetRateFromOpenExchange(currency, setting);
                        break;
                    case "fixer":
                        rate = await GetRateFromFixer(currency, setting);
                        break;
                    case "currencyapi":
                        rate = await GetRateFromCurrencyApi(currency, setting);
                        break;
                    case "exchangerateapi":
                        rate = await GetRateFromExchangeRateApi(currency, setting);
                        break;
                    default:
                        rate = GetFallbackRate(currency);
                        break;
                }

                var cacheDuration = setting.CacheDurationMinutes ?? 60;
                _cache.Set(cacheKey, rate, TimeSpan.FromMinutes(cacheDuration));
                return rate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kur alınamadı: {currency}");
                return GetFallbackRate(currency);
            }
        }

        // ========== TCMB (XML) ==========
        private async Task<decimal> GetRateFromTcmb(string currency)
        {
            var rates = await GetTcmbRates();
            return currency switch
            {
                "USD" => rates.USDRate,
                "EUR" => rates.EURRate,
                "GBP" => rates.GBPRate,
                _ => GetFallbackRate(currency)
            };
        }

      
        private async Task<TcmbRates> GetTcmbRates()
        {
            var setting = await GetActiveProvider();  

            var cacheKey = $"tcmb_rates_{setting.Provider}";
            if (_cache.TryGetValue(cacheKey, out TcmbRates cachedRates))
                return cachedRates;

           
            var url = setting.ApiUrl;
            Console.WriteLine($"🌐 TCMB API URL: {url}");  

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"📥 Gelen XML uzunluğu: {content.Length}");  

            var doc = new XmlDocument();
            doc.LoadXml(content);

            var usdNode = doc.SelectSingleNode("//Currency[@CurrencyCode='USD']");
            var eurNode = doc.SelectSingleNode("//Currency[@CurrencyCode='EUR']");
            var gbpNode = doc.SelectSingleNode("//Currency[@CurrencyCode='GBP']");

            var rates = new TcmbRates
            {
                USDRate = ParseRate(usdNode?.SelectSingleNode("ForexSelling")?.InnerText),
                EURRate = ParseRate(eurNode?.SelectSingleNode("ForexSelling")?.InnerText),
                GBPRate = ParseRate(gbpNode?.SelectSingleNode("ForexSelling")?.InnerText)
            };

            Console.WriteLine($"💰 KURLAR: USD={rates.USDRate}, EUR={rates.EURRate}, GBP={rates.GBPRate}");  

            _cache.Set(cacheKey, rates, TimeSpan.FromHours(1));
            return rates;
        }

        // ========== OpenExchangeRates (JSON) ==========
        private async Task<decimal> GetRateFromOpenExchange(string currency, ExchangeRateSetting setting)
        {
            var url = $"{setting.ApiUrl}/latest.json?app_id={setting.ApiKey}&base=USD";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var rates = root.GetProperty("rates");

            var usdToTry = rates.GetProperty("TRY").GetDecimal();

            return currency switch
            {
                "TRY" => 1,
                "USD" => usdToTry, 
                "EUR" => usdToTry / rates.GetProperty("EUR").GetDecimal(),
                "GBP" => usdToTry / rates.GetProperty("GBP").GetDecimal(),
                _ => usdToTry
            };
        }

        // ========== Fixer.io (JSON) ==========
        private async Task<decimal> GetRateFromFixer(string currency, ExchangeRateSetting setting)
        {
            var url = $"{setting.ApiUrl}?access_key={setting.ApiKey}&base=USD";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var rates = doc.RootElement.GetProperty("rates");

            var usdToTry = rates.GetProperty("TRY").GetDecimal();

            return currency switch
            {
                "TRY" => 1,
                "USD" => usdToTry, 
                "EUR" => usdToTry / rates.GetProperty("EUR").GetDecimal(),
                "GBP" => usdToTry / rates.GetProperty("GBP").GetDecimal(),
                _ => usdToTry
            };
        }

        // ========== CurrencyAPI (JSON) ==========
        private async Task<decimal> GetRateFromCurrencyApi(string currency, ExchangeRateSetting setting)
        {
            var url = $"{setting.ApiUrl}?apikey={setting.ApiKey}&base_currency=USD";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            var usdToTry = data.GetProperty("TRY").GetProperty("value").GetDecimal();

            return currency switch
            {
                "TRY" => 1,
                "USD" => usdToTry, 
                "EUR" => usdToTry / data.GetProperty("EUR").GetProperty("value").GetDecimal(),
                "GBP" => usdToTry / data.GetProperty("GBP").GetProperty("value").GetDecimal(),
                _ => usdToTry
            };
        }

        // ========== ExchangeRate-API (JSON) ==========
        private async Task<decimal> GetRateFromExchangeRateApi(string currency, ExchangeRateSetting setting)
        {
            var url = setting.ApiUrl.Replace("{API_KEY}", setting.ApiKey);
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var conversionRates = doc.RootElement.GetProperty("conversion_rates");

            var usdToTry = conversionRates.GetProperty("TRY").GetDecimal();

            return currency switch
            {
                "TRY" => 1,
                "USD" => usdToTry, 
                "EUR" => usdToTry / conversionRates.GetProperty("EUR").GetDecimal(),
                "GBP" => usdToTry / conversionRates.GetProperty("GBP").GetDecimal(),
                _ => usdToTry
            };
        }

        private async Task<decimal> GetRateFromTry(string currency)
        {
            if (currency == "TRY") return 1;
            var rate = await GetRateToTry(currency);
            return 1 / rate;
        }

        private decimal ParseRate(string rateStr)
        {
            if (string.IsNullOrEmpty(rateStr)) return 35.00m;
            rateStr = rateStr.Replace(".", ",");
            if (decimal.TryParse(rateStr, out var rate))
                return rate;
            return 35.00m;
        }

        private decimal GetFallbackRate(string currency)
        {
            return currency switch
            {
                "USD" => 35.00m,
                "EUR" => 38.00m,
                "GBP" => 44.00m,
                _ => 1
            };
        }

        public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
        {
            var rate = await GetRateAsync(fromCurrency, toCurrency);
            return amount * rate;
        }
    }

    public class TcmbRates
    {
        public decimal USDRate { get; set; }
        public decimal EURRate { get; set; }
        public decimal GBPRate { get; set; }
    }
}