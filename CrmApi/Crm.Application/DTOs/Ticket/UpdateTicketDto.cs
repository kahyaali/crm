using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Ticket
{
    public class UpdateTicketDto
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public string? Status { get; set; }
        public int? AssignedToPersonelId { get; set; }
    }
}
