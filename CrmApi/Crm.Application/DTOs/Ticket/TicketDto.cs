using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Ticket
{
    public class TicketDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public string? AssignedToPersonelName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int CommentCount { get; set; }
    }
}
