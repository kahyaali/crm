using Crm.Application.DTOs.Personel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Customer
{
    public class CustomerBulkUploadResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<CustomerBulkUploadErrorDto> Errors { get; set; } = new();
        public List<CustomerDto> CreatedCustomers { get; set; } = new();
    }

    public class CustomerBulkUploadErrorDto
    {
        public int RowNumber { get; set; }
        public string Email { get; set; }
        public string AccountNumber { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CustomerBulkUploadProgressDto
    {
        public string UploadId { get; set; }
        public int CurrentRow { get; set; }
        public int TotalRows { get; set; }
        public string CurrentEmail { get; set; }
        public string CurrentAccountNumber { get; set; } // Customer'a özel
        public string Status { get; set; } // Processing, Completed, Error
        public int Percentage { get; set; }
    }
}
