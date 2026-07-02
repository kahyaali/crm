using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Lead
{
    public class ConvertLeadToCustomerDto
    {
        public int LeadId { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
    }
}
