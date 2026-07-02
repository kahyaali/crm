using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Domain.Entities
{
    public class Personel:BaseEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string? PersonnelNumber { get; set; }  // Personel No - Benzersiz, boş geçilebilir
        public string? RegistrationNumber { get; set; } // Sicil No - Benzersiz, boş geçilebilir

        public string? AvatarUrl { get; set; }

        public decimal? Salary { get; set; }
        public string? Currency { get; set; } = "TRY";
        public DateTime? HireDate { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PostalCode { get; set; }

        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        public int? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        public int? PositionId { get; set; }
        public virtual Position? Position { get; set; }

        // Hiyerarşi - Kendisine bağlı personeller (Manager)
        public int? ManagerId { get; set; }
        public virtual Personel? Manager { get; set; }
        public virtual ICollection<Personel> Subordinates { get; set; } = new List<Personel>();

        // Atanmış kayıtlar
        public virtual ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
        public virtual ICollection<DomainTask> AssignedTasks { get; set; } = new List<DomainTask>();
        public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
        public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    }
}
