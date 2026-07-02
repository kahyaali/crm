using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Meeting
{
    public class UpdateMeetingDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? MeetingLink { get; set; }
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public List<int> AttendeePersonelIds { get; set; } = new();
    }
}
