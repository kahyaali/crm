using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Meeting : BaseEntity
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? MeetingLink { get; set; } // Zoom, Teams, Google Meet
        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }
        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }
        public string? Status { get; set; } // Planlandı, Devam Ediyor, Tamamlandı, İptal
        public string? Notes { get; set; }

        public int CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }
        public virtual ICollection<MeetingAttendee> Attendees { get; set; }
    }
}
