using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class TicketComment:BaseEntity
    {
        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }
        public string Comment { get; set; }
        public int? PersonelId { get; set; }
        public virtual Personel? Personel { get; set; }
        public bool IsInternal { get; set; } // Sadece personel görsün mü?
        public bool IsSolution { get; set; } // Çözüm notu mu?
    }
}
