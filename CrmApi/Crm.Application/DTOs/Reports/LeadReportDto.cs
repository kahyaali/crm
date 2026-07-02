using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class LeadReportDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? Source { get; set; }
        public string? Status { get; set; }
        public string? AssignedPersonel { get; set; }
        public decimal? PotentialRevenue { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public string? CampaignName { get; set; }
        public bool IsConverted { get; set; }
    }
}
