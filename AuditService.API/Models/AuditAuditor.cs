namespace AuditService.API.Models
{
    public class AuditAuditor
    {
        public int Id { get; set; }
        public int AuditId { get; set; }
        public int UserId { get; set; }

        public Audit Audit { get; set; } = null!;
    }
}
