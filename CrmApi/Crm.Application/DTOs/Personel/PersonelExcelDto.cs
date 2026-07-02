using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Personel
{
    public class PersonelExcelDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? PersonnelNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public decimal? Salary { get; set; }
        public string? Currency { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }
        public string? ManagerEmail { get; set; }
    }
}
