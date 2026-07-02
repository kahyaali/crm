using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Quote : BaseEntity
    {
        public string QuoteNumber { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public int? OpportunityId { get; set; }
        public virtual Opportunity? Opportunity { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; } // Taslak, Gönderildi, Onaylandı, Reddedildi
        public string? Notes { get; set; }
        public byte[]? PdfContent { get; set; }

        public int CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }
        public virtual ICollection<QuoteItem> Items { get; set; }
    }
}
