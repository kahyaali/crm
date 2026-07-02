using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Task
{
    public class UpdateTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public int? RelatedToCustomerId { get; set; }
        public int? RelatedToLeadId { get; set; }
        public int? RelatedToOpportunityId { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
