namespace Crm.Application.DTOs.Order
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? QuoteId { get; set; }
        public string? QuoteNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? Currency { get; set; } = "TRY";

        // Navigation
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
