using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Invoice
{
    public class UpdateInvoiceDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? OrderId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TaxRate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public List<UpdateInvoiceItemDto> Items { get; set; } = new();
    }
}
