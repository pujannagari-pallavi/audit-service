using AuditService.API.Enums;

namespace AuditService.API.DTOs
{
    public class UpdateAuditStatusDto
    {
        public AuditStatus Status { get; set; }
        public int ChangedByUserId { get; set; }
        public string? Comments { get; set; }
    }
}
