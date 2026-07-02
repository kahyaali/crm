using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ErrorLogs
{
    public class ErrorLogPaginationDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? ErrorLevel { get; set; }
        public bool? IsResolved { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
    }
}
