using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Quote
{
    public class CreateQuoteDto
    {
        public int CustomerId { get; set; }
        public int? OpportunityId { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public decimal TaxRate { get; set; } = 20;
        public string? Status { get; set; } = "Taslak";
        public string? Notes { get; set; }
        public List<CreateQuoteItemDto> Items { get; set; } = new();
    }
}
