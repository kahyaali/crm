using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Payment
{
    public class AddPaymentDto
    {
        public int InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
    }
}
