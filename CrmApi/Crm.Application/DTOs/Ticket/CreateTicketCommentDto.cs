using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Ticket
{
    public class CreateTicketCommentDto
    {
        public int TicketId { get; set; }
        public string Comment { get; set; }
        public bool IsInternal { get; set; }
        public bool IsSolution { get; set; }
    }
}
