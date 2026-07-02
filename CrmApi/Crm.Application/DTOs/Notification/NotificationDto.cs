using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int? PersonelId { get; set; }
        public string? PersonelName { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; } // Task, Meeting, Ticket, System, Lead, Order
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
