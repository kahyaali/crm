using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Notification
{
    public class NotificationPaginationDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? IsRead { get; set; }
        public string? Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
