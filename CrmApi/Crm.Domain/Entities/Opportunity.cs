using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Opportunity:BaseEntity
    {
        public string Name { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public decimal Amount { get; set; }
        public string? Stage { get; set; } // Prospekt, Teklif, Pazarlık, Kapandı-Kazandı, Kapandı-Kaybetti
        public string? Status { get; set; } // Açık, Kapandı, İptal 
        public int? AssignedToPersonelId { get; set; }
        public virtual Personel? AssignedToPersonel { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime? ActualCloseDate { get; set; }
        public string? Description { get; set; }
        public string? LostReason { get; set; }

        //  Oluşturan bilgisi
        public int CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }

        public virtual ICollection<Quote> Quotes { get; set; }
    }
}
