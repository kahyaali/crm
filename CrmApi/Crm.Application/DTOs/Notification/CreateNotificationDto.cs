using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Notification
{
    public class CreateNotificationDto
    {
        public int? PersonelId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? Type { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }
}
