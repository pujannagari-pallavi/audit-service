namespace AuditService.API.Models
{
    public class AuditHistory
    {
        public int Id { get; set; }
        public int AuditId { get; set; }
        public string FieldChanged { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public int ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public Audit Audit { get; set; } = null!;
    }
}
