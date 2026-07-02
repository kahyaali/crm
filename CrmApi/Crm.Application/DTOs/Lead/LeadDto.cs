namespace Crm.Application.DTOs.Lead
{
    public class LeadDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? Source { get; set; }
        public string? Status { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public string? AssignedToPersonelName { get; set; }
        public decimal? PotentialRevenue { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? Notes { get; set; }
        public int? ConvertedToCustomerId { get; set; }
        public string? ConvertedToCustomerName { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? CampaignId { get; set; }
        public string? CampaignName { get; set; }
    }
}
