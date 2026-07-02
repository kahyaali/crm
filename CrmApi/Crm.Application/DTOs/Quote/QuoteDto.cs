using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Quote
{
    public class QuoteDto
    {
        public int Id { get; set; }
        public string QuoteNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? OpportunityId { get; set; }
        public string? OpportunityName { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
        public List<QuoteItemDto> Items { get; set; } = new();
    }
}
