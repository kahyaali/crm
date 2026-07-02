using System.ComponentModel.DataAnnotations;

namespace Crm.Application.DTOs.Lead
{
    public class CreateLeadDto
    {
       
        public string CompanyName { get; set; }

        public string ContactName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string? Source { get; set; }
        public string? Status { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public decimal? PotentialRevenue { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? Notes { get; set; }

        public int? CampaignId { get; set; }
    }
}
