using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Personel
{
    public class BulkUploadResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<BulkUploadErrorDto> Errors { get; set; } = new();
        public List<PersonelDto> CreatedPersonels { get; set; } = new();
    }

    public class BulkUploadErrorDto
    {
        public int RowNumber { get; set; }
        public string Email { get; set; }
        public string ErrorMessage { get; set; }
    }
}
