using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.ActionLogs
{
    public class ActionLogDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? PersonelId { get; set; }
        public string? PersonelName { get; set; }
        public string ActionType { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, LOGOUT, EXPORT, IMPORT
        public string EntityType { get; set; } = string.Empty;  // Customer, Personel, User, MailSetting
        public int? EntityId { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? AdditionalInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
