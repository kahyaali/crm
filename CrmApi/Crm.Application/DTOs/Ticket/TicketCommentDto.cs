using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Ticket
{
    public class TicketCommentDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Comment { get; set; }
        public int? PersonelId { get; set; }
        public string? PersonelName { get; set; }
        public bool IsInternal { get; set; }
        public bool IsSolution { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
