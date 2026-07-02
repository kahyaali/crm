using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Notification:BaseEntity
    {
        public int? PersonelId { get; set; }
        public virtual Personel Personel { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; } // Task, Meeting, Ticket, System
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
