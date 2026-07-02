using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    // Potansiyel Müşteri
    public class Lead:BaseEntity
    {
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? Source { get; set; } // Web, Referans, Reklam, Fuar
        public string? Status { get; set; } // Yeni, İletişime Geçildi, Teklif Sunuldu, Müşteri Oldu, Kaybedildi
        public int? AssignedToPersonelId { get; set; }
        public virtual Personel? AssignedToPersonel { get; set; }
        public decimal? PotentialRevenue { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? Notes { get; set; }
        public int? ConvertedToCustomerId { get; set; }
        public virtual Customer? ConvertedToCustomer { get; set; }

        public int? CampaignId { get; set; }
        public virtual Campaign? Campaign { get; set; }
    }
}
