using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Customer:BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? CustomerType { get; set; } // Bireysel, Kurumsal, Potansiyel
        public string? Notes { get; set; }
        public string? Status { get; set; }

        public bool IsActive { get; set; } = true;


        /// <summary>
        /// Cari Hesap Numarası - Benzersiz, boş geçilemez
        /// </summary>
        public string AccountNumber { get; set; }  //  Cari No - ZORUNLU

        /// <summary>
        /// Ödeme Tipi: Peşin, Vadeli, Kredili
        /// </summary>
        public string? PaymentType { get; set; }  // Cash, Credit, Deferred

        /// <summary>
        /// Kredi Limiti (Kurumsal müşteriler için)
        /// </summary>
        public decimal? CreditLimit { get; set; }

        /// <summary>
        /// Vade Gün Sayısı
        /// </summary>
        public int? PaymentTermDays { get; set; } // 30, 60, 90 gün

        /// <summary>
        /// İndirim Oranı (%)
        /// </summary>
        public decimal? DiscountRate { get; set; }

        /// <summary>
        /// Vergi Dairesi (Şirket için zaten var, bireysel için ek)
        /// </summary>
        public string? TaxAdministration { get; set; }

        /// <summary>
        /// Web Sitesi
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Farklı Teslimat Adresi
        /// </summary>
        public string? ShippingAddress { get; set; }

        /// <summary>
        /// Farklı Fatura Adresi
        /// </summary>
        public string? InvoiceAddress { get; set; }

        /// <summary>
        /// İlgili Kişi (Kurumsal müşterilerde)
        /// </summary>
        public string? ContactPerson { get; set; }

        /// <summary>
        /// İlgili Kişi Telefonu
        /// </summary>
        public string? ContactPersonPhone { get; set; }




        //================== Mevcut ilişkiler ==================

        // User ile ilişki (Müşteriye de kullanıcı açılabilir)
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        // Hangi personel ekledi/kaydetti
        public int? CreatedByPersonelId { get; set; }
        public virtual Personel? CreatedByPersonel { get; set; }

        public int? AssignedToPersonelId { get; set; }
        public virtual Personel? AssignedToPersonel { get; set; }

        //  Navigation
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
