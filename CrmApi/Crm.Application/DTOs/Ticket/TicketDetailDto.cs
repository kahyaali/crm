using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Ticket
{
    public class TicketDetailDto:TicketDto
    {
        public int? CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
        public List<TicketCommentDto> Comments { get; set; } = new();
    }
}
