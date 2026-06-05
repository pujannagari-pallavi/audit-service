using AuditService.API.Enums;

namespace AuditService.API.Models
{
    public class Audit
    {
        public int AuditId { get; set; }
        public string AuditCode { get; set; } = string.Empty;
        public string AuditName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public AuditType AuditType { get; set; }
        public int CreatedByUserId { get; set; }
        public int AuditManagerId { get; set; }  // Audit Manager who will approve the audit
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public AuditStatus Status { get; set; } = AuditStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<AuditAuditor> AuditAuditors { get; set; } = new List<AuditAuditor>();
        public ICollection<AuditHistory> AuditHistories { get; set; } = new List<AuditHistory>();
    }
}
