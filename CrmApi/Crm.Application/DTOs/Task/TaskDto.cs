using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Task
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public string? AssignedToPersonelName { get; set; }
        public int? RelatedToCustomerId { get; set; }
        public string? RelatedToCustomerName { get; set; }
        public int? RelatedToLeadId { get; set; }
        public string? RelatedToLeadName { get; set; }
        public int? RelatedToOpportunityId { get; set; }
        public string? RelatedToOpportunityName { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
    }
}
