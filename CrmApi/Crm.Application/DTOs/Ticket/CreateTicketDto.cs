namespace Crm.Application.DTOs.Ticket
{
    public class CreateTicketDto
    {
        public string Subject { get; set; }
        public string? Description { get; set; }
        public int CustomerId { get; set; }
        public string? Category { get; set; }
        public string? Priority { get; set; }
        public int? AssignedToPersonelId { get; set; }
    }
}
