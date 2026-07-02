using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Order
{
    public class UpdateOrderDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? QuoteId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }
        public string? Currency { get; set; } = "TRY";
        public decimal TaxRate { get; set; } = 0.20m;

        // Order Items
        public List<UpdateOrderItemDto> Items { get; set; } = new List<UpdateOrderItemDto>();
    }
}
