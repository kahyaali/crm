using Crm.Application.DTOs.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Invoice
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
    }
}
