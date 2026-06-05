using AuditService.API.Enums;

namespace AuditService.API.DTOs
{
    public class UpdateAuditDto
    {
        public string AuditName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public AuditType AuditType { get; set; }
        public int AuditManagerId { get; set; }  // Audit Manager who will approve the audit
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public List<int> AuditorUserIds { get; set; } = new();
    }
}
