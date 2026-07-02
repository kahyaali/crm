using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Exchange
{
    public class UpdateExchangeRateSettingDto
    {
        public string? Name { get; set; }
        public string? ApiUrl { get; set; }
        public string? ApiKey { get; set; }
        public int? Priority { get; set; }
        public int? CacheDurationMinutes { get; set; }
    }
}
