using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Campaign
{
    public class UpdateCampaignDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Budget { get; set; }
        public decimal? ActualCost { get; set; }
        public int? TargetLeads { get; set; }
        public int? ConvertedLeads { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}
