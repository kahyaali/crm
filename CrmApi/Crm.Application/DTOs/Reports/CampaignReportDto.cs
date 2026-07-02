using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class CampaignReportDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Budget { get; set; }
        public decimal? ActualCost { get; set; }
        public int? TargetLeads { get; set; }
        public int? ConvertedLeads { get; set; }
        public decimal? ConversionRate { get; set; }
        public string? Status { get; set; }
        public string? CreatedByPersonel { get; set; }
        public decimal? ROI { get; set; } // Yatırım Getirisi
    }
}
