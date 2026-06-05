namespace AuditService.API.DTOs
{
    public class AuditManagerDashboardDto
    {
        public List<AuditResponseDto> PendingApproval { get; set; } = new();
        public List<AuditResponseDto> InProgress { get; set; } = new();
        public List<AuditResponseDto> PendingClosure { get; set; } = new();
        public int TotalPendingApproval { get; set; }
        public int TotalInProgress { get; set; }
        public int TotalPendingClosure { get; set; }
    }
}
