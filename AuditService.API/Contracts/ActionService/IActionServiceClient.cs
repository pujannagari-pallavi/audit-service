namespace AuditService.API.Contracts.ActionService
{
    /// <summary>
    /// Contract for communicating with the Action Service.
    /// The Audit Service needs to check corrective action statuses
    /// to enforce the final audit closure rule: "All corrective actions
    /// must be closed before the audit can be completed."
    /// 
    /// <para><b>Owner:</b> Action Service Team</para>
    /// <para><b>Status:</b> NOT IMPLEMENTED — Contract only (no HTTP calls yet)</para>
    /// 
    /// <para><b>Required Endpoints from Action Service:</b></para>
    /// <list type="bullet">
    ///   <item>GET /api/actions/audit/{auditId}/all-closed — Are all actions for this audit closed?</item>
    ///   <item>GET /api/actions/audit/{auditId}/status-summary — Action counts by status</item>
    /// </list>
    /// 
    /// <para><b>Business Rules Dependent on This Service:</b></para>
    /// <list type="bullet">
    ///   <item>Audit cannot move to "Completed" unless all corrective actions are Closed (BRD 4.6).</item>
    ///   <item>Actions are linked to Observations (via ObservationId), not directly to Audits.
    ///         The Action Service should support querying by AuditId through the observation chain.</item>
    /// </list>
    /// 
    /// <para><b>Note:</b> Actions reference ObservationIds, which in turn reference AuditIds.
    /// The Action Service needs to resolve this chain internally or expose an audit-level endpoint.</para>
    /// </summary>
    public interface IActionServiceClient
    {
        /// <summary>
        /// Checks whether all corrective actions for observations under a given audit are Closed.
        /// <para><b>When to call:</b> Before transitioning audit to Completed status (BRD 4.6).</para>
        /// <para><b>Expected endpoint:</b> GET /api/actions/audit/{auditId}/all-closed</para>
        /// </summary>
        /// <param name="auditId">The audit ID to check corrective actions for.</param>
        /// <returns>True if every corrective action linked to this audit's observations is Closed.</returns>
        Task<bool> AreAllActionsClosedForAuditAsync(int auditId);

        /// <summary>
        /// Gets a count of corrective actions grouped by status for a specific audit.
        /// <para><b>When to call:</b> For audit summary/dashboard views.</para>
        /// <para><b>Expected endpoint:</b> GET /api/actions/audit/{auditId}/status-summary</para>
        /// </summary>
        /// <param name="auditId">The audit ID to get action summary for.</param>
        /// <returns>Summary with counts per status (Open, InProgress, ClosureRequested, Closed).</returns>
        Task<ActionStatusSummaryDto> GetActionStatusSummaryForAuditAsync(int auditId);

        /// <summary>
        /// Checks whether any corrective actions exist for the observations under a given audit.
        /// <para><b>When to call:</b> To determine if the audit has entered the corrective action phase.</para>
        /// <para><b>Expected endpoint:</b> GET /api/actions/audit/{auditId} (check count > 0)</para>
        /// </summary>
        /// <param name="auditId">The audit ID to check.</param>
        /// <returns>True if at least one corrective action exists for this audit's observations.</returns>
        Task<bool> HasActionsForAuditAsync(int auditId);
    }
}
