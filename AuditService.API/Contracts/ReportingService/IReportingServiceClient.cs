namespace AuditService.API.Contracts.ReportingService
{
    /// <summary>
    /// Client for communicating with ReportingService
    /// </summary>
    public interface IReportingServiceClient
    {
        /// <summary>
        /// Syncs audit data to ReportingService for dashboard and reports
        /// </summary>
        Task SyncAuditAsync(AuditSyncDto auditSync);
    }

    public class AuditSyncDto
    {
        public int AuditId { get; set; }
        public string AuditCode { get; set; } = string.Empty;
        public string AuditName { get; set; } = string.Empty;
        public string AuditType { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string AssignedAuditorUserIds { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int TotalObservations { get; set; }
        public int TotalActions { get; set; }
        public int ClosedActions { get; set; }
        public bool IsDeleted { get; set; }
    }
}
