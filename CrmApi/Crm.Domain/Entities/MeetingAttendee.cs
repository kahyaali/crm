using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class MeetingAttendee : BaseEntity
    {
        public int MeetingId { get; set; }
        public virtual Meeting Meeting { get; set; }
        public int PersonelId { get; set; }
        public virtual Personel Personel { get; set; }
        public string? AttendanceStatus { get; set; } // Katıldı, Katılmadı, Beklemede
    }
}
