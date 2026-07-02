using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        public int? ReceivedByPersonelId { get; set; }
        public string? ReceivedByPersonelName { get; set; }
    }
}
