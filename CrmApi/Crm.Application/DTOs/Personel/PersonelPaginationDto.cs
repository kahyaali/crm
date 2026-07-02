using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Personel
{
    public class PersonelPaginationDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "asc";
        public int? DepartmentId { get; set; }
        public int? PositionId { get; set; }
        public bool? IsActive { get; set; }
    }
}
