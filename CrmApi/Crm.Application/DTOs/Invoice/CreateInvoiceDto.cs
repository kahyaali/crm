using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Invoice
{
    public class CreateInvoiceDto
    {
        public int CustomerId { get; set; }
        public int? OrderId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TaxRate { get; set; } = 20;
        public string? Status { get; set; } = "Gönderildi";
        public string? Notes { get; set; }
        public List<CreateInvoiceItemDto> Items { get; set; } = new();
    }
}
