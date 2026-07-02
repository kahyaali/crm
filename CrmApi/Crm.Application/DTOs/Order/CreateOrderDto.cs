namespace Crm.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public int? QuoteId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? DeliveryDate { get; set; }
        public string? Status { get; set; } = "Pending";
        public string? PaymentStatus { get; set; } = "Pending";
        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }
        public string? Currency { get; set; } = "TRY";
        public decimal TaxRate { get; set; } = 0.20m;

        // Order Items (ürünler)
        public List<CreateOrderItemDto> Items { get; set; } = new List<CreateOrderItemDto>();
    }
}
