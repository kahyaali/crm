using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Opportunity
{
    public class OpportunityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string? Stage { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public string? AssignedToPersonelName { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime? ActualCloseDate { get; set; }
        public string? Description { get; set; }
        public string? LostReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
    }
}
