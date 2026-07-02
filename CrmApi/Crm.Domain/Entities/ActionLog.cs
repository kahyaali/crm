using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class ActionLog:BaseEntity
    {
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        public int? PersonelId { get; set; }
        public virtual Personel? Personel { get; set; }

        public string ActionType { get; set; } // CREATE, UPDATE, DELETE, LOGIN, LOGOUT, EXPORT, IMPORT
        public string EntityType { get; set; }  // Customer, Personel, User, MailSetting
        public int? EntityId { get; set; }      // Hangi kayıt üzerinde işlem yapıldı
        public string? OldData { get; set; }     // JSON formatında eski veri
        public string? NewData { get; set; }     // JSON formatında yeni veri
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}
