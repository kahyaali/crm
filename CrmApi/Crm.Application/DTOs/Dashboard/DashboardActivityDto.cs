using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Dashboard
{
    public class DashboardActivityDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } // "Ticket", "Lead", "Invoice"
        public string Action { get; set; }     // "Created", "Updated", "Resolved"
        public string Title { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
    }
}
