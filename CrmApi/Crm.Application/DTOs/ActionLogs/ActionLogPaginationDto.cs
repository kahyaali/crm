using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ActionLogs
{
    public class ActionLogPaginationDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? UserId { get; set; }
        public int? PersonelId { get; set; }
        public string? ActionType { get; set; }
        public string? EntityType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
    }
}
