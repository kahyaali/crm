using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ActionLogs
{
    public class ActionLogStatsDto
    {
        public int TotalActions { get; set; }
        public List<ActionTypeCountDto> ActionsByType { get; set; } = new();
        public List<EntityTypeCountDto> ActionsByEntity { get; set; } = new();
        public List<UserActionCountDto> MostActiveUsers { get; set; } = new();
        public List<DailyActionCountDto> Last7DaysActions { get; set; } = new();
    }
}
