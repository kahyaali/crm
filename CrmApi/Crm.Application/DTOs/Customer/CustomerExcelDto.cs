using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Customer
{
    public class CustomerExcelDto
    {
        // Zorunlu Alanlar
        public string AccountNumber { get; set; }      // Cari No - Benzersiz, ZORUNLU
        public string FirstName { get; set; }           // Ad - ZORUNLU
        public string LastName { get; set; }            // Soyad - ZORUNLU
        public string Email { get; set; }               // Email - ZORUNLU
        public string Phone { get; set; }               // Telefon - ZORUNLU

        // Müşteri Tipi (Bireysel/Kurumsal/Potansiyel) - ZORUNLU
        public string CustomerType { get; set; }

        // Kurumsal Bilgiler (CustomerType = Kurumsal ise ZORUNLU)
        public string? CompanyName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }

        // Opsiyonel Alanlar
        public string? TaxAdministration { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? Website { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPersonPhone { get; set; }
        public string? PaymentType { get; set; }        // Cash, Credit, Deferred
        public decimal? CreditLimit { get; set; }
        public int? PaymentTermDays { get; set; }
        public decimal? DiscountRate { get; set; }
        public string? ShippingAddress { get; set; }
        public string? InvoiceAddress { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
    }
}
