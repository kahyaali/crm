using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public string PaymentNumber { get; set; }
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; } // Nakit, Kredi Kartı, Banka Havalesi, Çek
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        public int? ReceivedByPersonelId { get; set; }
        public virtual Personel? ReceivedByPersonel { get; set; }
    }
}
