using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Product
{
    public class ProductBulkUploadProgressDto
    {
        public string UploadId { get; set; }
        public int CurrentRow { get; set; }
        public int TotalRows { get; set; }
        public string CurrentName { get; set; }
        public string CurrentSku { get; set; }
        public string Status { get; set; }
        public int Percentage { get; set; }
    }
}
