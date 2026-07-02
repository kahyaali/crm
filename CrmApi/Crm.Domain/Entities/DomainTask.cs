using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class DomainTask:BaseEntity
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public virtual Personel? AssignedToPersonel { get; set; }
        public int? RelatedToCustomerId { get; set; }
        public virtual Customer? RelatedToCustomer { get; set; }
        public int? RelatedToLeadId { get; set; }
        public virtual Lead? RelatedToLead { get; set; }
        public int? RelatedToOpportunityId { get; set; }
        public virtual Opportunity? RelatedToOpportunity { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }
    }
}
