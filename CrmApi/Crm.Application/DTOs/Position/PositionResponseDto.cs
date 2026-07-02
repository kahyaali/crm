using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Position
{
    public class PositionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int PersonelCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
