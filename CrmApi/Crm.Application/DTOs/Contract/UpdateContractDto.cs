using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Contract
{
    public class UpdateContractDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal ContractValue { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public int? QuoteId { get; set; }
    }
}
