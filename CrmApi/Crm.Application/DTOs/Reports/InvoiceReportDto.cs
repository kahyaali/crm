using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Reports
{
    public class InvoiceReportDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string? Status { get; set; }
        public string? CreatedByPersonel { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
