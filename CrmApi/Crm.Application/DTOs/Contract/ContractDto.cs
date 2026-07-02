using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Contract
{
    public class ContractDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal ContractValue { get; set; }
        public string? Status { get; set; }
        public string? DocumentFileName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedByPersonelId { get; set; }
        public string? CreatedByPersonelName { get; set; }
        public int? QuoteId { get; set; }
        public string? QuoteNumber { get; set; }
        public DateTime? SignedDate { get; set; }
        public string? SignedBy { get; set; }
        public bool IsSigned { get; set; }
    }
}
