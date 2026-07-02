using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Campaign:BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; } // Email, SMS, Sosyal Medya, Radyo, TV
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Budget { get; set; }
        public decimal? ActualCost { get; set; }
        public int? TargetLeads { get; set; }
        public int? ConvertedLeads { get; set; }
        public string? Status { get; set; } // Taslak, Aktif, Tamamlandı, İptal
        public string? Notes { get; set; }

        // Oluşturan bilgisi
        public int CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }
    }
}
