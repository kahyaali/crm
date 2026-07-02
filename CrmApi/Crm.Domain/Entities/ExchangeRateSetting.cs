using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class ExchangeRateSetting:BaseEntity
    {
        public string Provider { get; set; }        // Tcmb, OpenExchangeRates
        public string Name { get; set; }            // TCMB, OpenExchangeRates
        public string ApiUrl { get; set; }          // API URL
        public string ApiKey { get; set; }          // API Key (varsa)
        public bool IsActive { get; set; }          // Aktif mi?
        public int Priority { get; set; }           // Öncelik sırası
        public int? CacheDurationMinutes { get; set; } = 60; // Cache süresi
    }
}
