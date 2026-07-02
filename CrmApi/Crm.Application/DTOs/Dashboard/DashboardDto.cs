using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Dashboard
{
    public class DashboardDto
    {
        public DashboardKpiDto Kpi { get; set; }
        public List<DashboardChartDto> Charts { get; set; }
        public List<DashboardActivityDto> RecentActivities { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
