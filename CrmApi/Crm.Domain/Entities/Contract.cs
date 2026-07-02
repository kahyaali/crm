using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Contract : BaseEntity
    {
        public string ContractNumber { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal ContractValue { get; set; }
        public string? Status { get; set; } // Taslak, Bekliyor, Aktif, Süresi Doldu, Feshedildi
        public byte[]? DocumentContent { get; set; }
        public string? DocumentFileName { get; set; }
        public string? Notes { get; set; }

        //  Oluşturan bilgisi
        public int CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }

        //  İlişkili Teklif (Opsiyonel)
        public int? QuoteId { get; set; }
        public virtual Quote? Quote { get; set; }

        //  İmza bilgileri (Opsiyonel)
        public DateTime? SignedDate { get; set; }
        public string? SignedBy { get; set; }
        public bool IsSigned { get; set; }
    }
}
