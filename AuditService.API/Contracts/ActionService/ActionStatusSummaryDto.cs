namespace AuditService.API.Contracts.ActionService
{
    /// <summary>
    /// Represents a summary of corrective action counts by status for a given audit.
    /// <para><b>Source:</b> Action Service — GET /api/actions/audit/{auditId}/status-summary</para>
    /// <para><b>Owner:</b> Action Service Team</para>
    /// 
    /// <para><b>Used by Audit Service to:</b></para>
    /// <list type="bullet">
    ///   <item>Determine if audit can be closed/completed (all actions must be Closed — BRD 4.6)</item>
    ///   <item>Show corrective action progress on audit detail views</item>
    /// </list>
    /// </summary>
    public class ActionStatusSummaryDto
    {
        /// <summary>
        /// The audit these actions are related to.
        /// </summary>
        public int AuditId { get; set; }

        /// <summary>
        /// Total number of corrective actions for this audit.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Number of actions in Open status.
        /// </summary>
        public int OpenCount { get; set; }

        /// <summary>
        /// Number of actions in InProgress status.
        /// </summary>
        public int InProgressCount { get; set; }

        /// <summary>
        /// Number of actions in ClosureRequested status.
        /// </summary>
        public int ClosureRequestedCount { get; set; }

        /// <summary>
        /// Number of actions in Closed status.
        /// </summary>
        public int ClosedCount { get; set; }
    }
}
