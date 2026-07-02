using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Dashboard
{
    public class DashboardChartDto
    {
        public string Title { get; set; }
        public string Type { get; set; } // "Pie", "Bar", "Line", "Funnel"
        public List<ChartDataPoint> Data { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public string? Color { get; set; }
        public decimal? Percentage { get; set; }
    }
}
