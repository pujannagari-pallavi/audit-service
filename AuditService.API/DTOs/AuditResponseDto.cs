using AuditService.API.Enums;

namespace AuditService.API.DTOs
{
    public class AuditResponseDto
    {
        public int AuditId { get; set; }
        public string AuditCode { get; set; } = string.Empty;
        public string AuditName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string AuditType { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public int AuditManagerId { get; set; }  // Audit Manager who will approve the audit
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<int> AuditorUserIds { get; set; } = new();
    }
}
