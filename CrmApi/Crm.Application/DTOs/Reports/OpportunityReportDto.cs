using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class OpportunityReportDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string? Stage { get; set; }
        public string? Status { get; set; }
        public string? AssignedPersonel { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime? ActualCloseDate { get; set; }
        public string? LostReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int QuoteCount { get; set; }
        public bool IsWon { get; set; }
    }
}
