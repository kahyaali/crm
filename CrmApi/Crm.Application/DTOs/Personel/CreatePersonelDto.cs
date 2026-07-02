namespace Crm.Application.DTOs.Personel
{
    public class CreatePersonelDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string? PersonnelNumber { get; set; }  
        public string? RegistrationNumber { get; set; } 

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public bool IsActive { get; set; } = true;


        public decimal? Salary { get; set; }
        public string? Currency { get; set; } = "TRY";
        public DateTime? HireDate { get; set; }
        public bool CreateUser { get; set; } = false;
        public string? Password { get; set; }
        public int? PositionId { get; set; }
        public int? DepartmentId { get; set; }

        public int? ManagerId { get; set; }
    }
}
