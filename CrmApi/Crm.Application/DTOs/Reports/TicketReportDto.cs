using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class TicketReportDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public string CustomerName { get; set; }
        public string? AssignedPersonel { get; set; }
        public string? CreatedByPersonel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int CommentCount { get; set; }
        public TimeSpan? ResolutionTime { get; set; } // Çözüm süresi
    }
}
