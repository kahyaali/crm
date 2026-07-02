using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Meeting
{
    public class MeetingDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? MeetingLink { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public string? Status { get; set; } // Planlandı, Devam Ediyor, Tamamlandı, İptal
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public int CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
        public List<MeetingAttendeeDto> Attendees { get; set; } = new();
    }

    public class MeetingAttendeeDto
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public int PersonelId { get; set; }
        public string? PersonelName { get; set; }
        public string? AttendanceStatus { get; set; } // Katıldı, Katılmadı, Beklemede
    }
}
