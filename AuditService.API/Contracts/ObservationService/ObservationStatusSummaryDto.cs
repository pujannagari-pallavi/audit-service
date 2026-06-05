namespace AuditService.API.Contracts.ObservationService
{
    /// <summary>
    /// Represents a summary of observation counts by status for a given audit.
    /// <para><b>Source:</b> Observation Service — GET /api/observations/audit/{auditId}/status-summary</para>
    /// <para><b>Owner:</b> Observation Service Team</para>
    /// 
    /// <para><b>Used by Audit Service to:</b></para>
    /// <list type="bullet">
    ///   <item>Determine if audit can transition to FindingsApproved (all must be Approved)</item>
    ///   <item>Show observation progress on audit detail views</item>
    /// </list>
    /// </summary>
    public class ObservationStatusSummaryDto
    {
        /// <summary>
        /// The audit these observations belong to.
        /// </summary>
        public int AuditId { get; set; }

        /// <summary>
        /// Total number of observations for this audit.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Number of observations in Draft status.
        /// </summary>
        public int DraftCount { get; set; }

        /// <summary>
        /// Number of observations in Submitted status.
        /// </summary>
        public int SubmittedCount { get; set; }

        /// <summary>
        /// Number of observations in Approved status.
        /// </summary>
        public int ApprovedCount { get; set; }

        /// <summary>
        /// Number of observations in Rejected status.
        /// </summary>
        public int RejectedCount { get; set; }
    }
}
