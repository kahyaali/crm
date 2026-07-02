using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Ticket
{
    public class TicketStatsDto
    {
        public int Total { get; set; }
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int Closed { get; set; }
        public int Critical { get; set; }
        public double AverageResolutionTime { get; set; } // Saat cinsinden
    }
}
