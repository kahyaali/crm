using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Opportunity
{
    public class CreateOpportunityDto
    {
        public string Name { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string? Stage { get; set; } = "Prospekt";
        public int? AssignedToPersonelId { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public string? Description { get; set; }
    }
}
