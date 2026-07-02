using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Customer
{
    public class CustomerPaginationDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? AssignedToPersonelId { get; set; }
        public string? Status { get; set; }

        public string? PaymentType { get; set; }  // Cash, Credit, Deferred
        public decimal? MinCreditLimit { get; set; }
        public decimal? MaxCreditLimit { get; set; }
        public string? CustomerType { get; set; }  // Bireysel, Kurumsal, Potansiyel
    }
}
