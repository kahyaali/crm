using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Meeting
{
    public class UpdateAttendanceStatusDto
    {
        public int MeetingId { get; set; }
        public int PersonelId { get; set; }
        public string AttendanceStatus { get; set; } // Katıldı, Katılmadı, Beklemede
    }
}
