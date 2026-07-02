namespace Crm.Application.DTOs.Customer
{
    public class CreateCustomerDto
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
        public string? CustomerType { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; } = "Pending";
        public int? AssignedToPersonelId { get; set; }

        public bool IsActive { get; set; } = true;

        public string AccountNumber { get; set; }         
        public string? PaymentType { get; set; }
        public decimal? CreditLimit { get; set; }
        public int? PaymentTermDays { get; set; }
        public decimal? DiscountRate { get; set; }
        public string? TaxAdministration { get; set; }
        public string? Website { get; set; }
        public string? ShippingAddress { get; set; }
        public string? InvoiceAddress { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPersonPhone { get; set; }
    }
}
