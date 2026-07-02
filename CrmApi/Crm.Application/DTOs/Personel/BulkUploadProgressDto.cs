using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Personel
{
    public class BulkUploadProgressDto
    {
        public string UploadId { get; set; }
        public int CurrentRow { get; set; }
        public int TotalRows { get; set; }
        public string CurrentEmail { get; set; }
        public string Status { get; set; } // Processing, Completed, Error
        public int Percentage { get; set; }
    }
}
