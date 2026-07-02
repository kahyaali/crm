using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Order : BaseEntity
    {
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        public int? QuoteId { get; set; }
        public virtual Quote? Quote { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Status { get; set; } // Beklemede, Onaylandı, Hazırlanıyor, Kargolandı, Teslim Edildi, İptal Edildi
        public string? PaymentStatus { get; set; } // Beklemede, Kısmen Ödendi, Ödendi, İptal
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }
        public string? Currency { get; set; } = "TRY";
        public virtual ICollection<OrderItem> Items { get; set; }
        public virtual ICollection<Invoice> Invoices { get; set; }
    }
}
