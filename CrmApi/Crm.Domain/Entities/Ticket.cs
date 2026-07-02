using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public string TicketNumber { get; set; }
        public string Subject { get; set; }
        public string? Description { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public string? Category { get; set; } // Şikayet, Talep, Bilgi, Teknik Destek
        public string? Priority { get; set; } // Düşük, Orta, Yüksek, Acil
        public string? Status { get; set; } // Açık, İşlemde, Beklemede, Çözüldü, Kapandı
        public int? AssignedToPersonelId { get; set; }
        public virtual Personel? AssignedToPersonel { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int? CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }
        public virtual ICollection<TicketComment> Comments { get; set; }
    }
}
