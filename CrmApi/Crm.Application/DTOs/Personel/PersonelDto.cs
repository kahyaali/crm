namespace Crm.Application.DTOs.Personel
{
    public class PersonelDto
    {
        public int Id { get; set; }
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
        public string? Currency { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }

        // Department
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        // Position
        public int? PositionId { get; set; }
        public string? PositionName { get; set; }

        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
