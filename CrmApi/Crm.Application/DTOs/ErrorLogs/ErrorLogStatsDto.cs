using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ErrorLogs
{
    public class ErrorLogStatsDto
    {
        public int TotalErrors { get; set; }
        public int ResolvedErrors { get; set; }
        public int UnresolvedErrors { get; set; }
        public List<ErrorLevelCountDto> ErrorsByLevel { get; set; } = new();
        public List<ErrorMessageCountDto> MostCommonErrors { get; set; } = new();
        public List<DailyErrorCountDto> Last7DaysErrors { get; set; } = new();
    }
}
