using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public string InvoiceNumber { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxRate { get; set; } //  KDV oranı (%20 gibi)
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Status { get; set; } // Gönderildi, Kısmen Ödendi, Ödendi, Gecikmiş, İptal
        public string? Notes { get; set; }
        public byte[]? PdfContent { get; set; }

        //  Oluşturan bilgisi
        public int CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }

        //  Fatura kalemleri
        public virtual ICollection<InvoiceItem> Items { get; set; }

        //  Ödemeler
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
