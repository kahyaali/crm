using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class PersonnelPerformanceReportDto
    {
        public int PersonelId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }

        // Ticket Performansı
        public int TotalAssignedTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public double ResolutionRate { get; set; } // Yüzde
        public double AverageResolutionTime { get; set; } // Saat

        // Lead Performansı
        public int TotalAssignedLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public double ConversionRate { get; set; } // Yüzde

        // Diğer
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double TaskCompletionRate { get; set; } // Yüzde
    }
}
