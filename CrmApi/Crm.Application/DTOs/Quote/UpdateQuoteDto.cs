using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Quote
{
    public class UpdateQuoteDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? OpportunityId { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public decimal TaxRate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public List<UpdateQuoteItemDto> Items { get; set; } = new();
    }
}
