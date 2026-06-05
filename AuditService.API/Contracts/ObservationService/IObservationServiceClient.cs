namespace AuditService.API.Contracts.ObservationService
{
    /// <summary>
    /// Contract for communicating with the Observation Service.
    /// The Audit Service needs to query observation statuses to enforce
    /// audit lifecycle transitions (e.g., all observations must be approved
    /// before audit can move to FindingsApproved or Closed).
    /// 
    /// <para><b>Owner:</b> Your team (Observation Service)</para>
    /// <para><b>Status:</b> Endpoints EXIST in ObservationService.API — HTTP client not wired yet</para>
    /// 
    /// <para><b>Available Endpoints (already built):</b></para>
    /// <list type="bullet">
    ///   <item>GET /api/observations/audit/{auditId} — Returns all observations for an audit</item>
    ///   <item>GET /api/observations/audit/{auditId}/status-summary — Returns count by status</item>
    ///   <item>GET /api/observations/audit/{auditId}/all-approved — Returns whether all are approved</item>
    /// </list>
    /// 
    /// <para><b>Business Rules Dependent on This Service:</b></para>
    /// <list type="bullet">
    ///   <item>Audit cannot move to "FindingsApproved" unless all observations are Approved.</item>
    ///   <item>Audit cannot be "Closed/Completed" unless all observations are Approved.</item>
    ///   <item>When audit moves to "ObservationsSubmitted", all draft observations should be submitted.</item>
    /// </list>
    /// </summary>
    public interface IObservationServiceClient
    {
        /// <summary>
        /// Checks whether all observations for a given audit are in Approved status.
        /// <para><b>When to call:</b> Before transitioning audit to FindingsApproved or Closed status.</para>
        /// <para><b>Expected endpoint:</b> GET /api/observations/audit/{auditId}/all-approved</para>
        /// </summary>
        /// <param name="auditId">The audit ID to check observations for.</param>
        /// <returns>True if every observation for this audit has status = Approved.</returns>
        Task<bool> AreAllObservationsApprovedAsync(int auditId);

        /// <summary>
        /// Gets a count of observations grouped by status for a specific audit.
        /// <para><b>When to call:</b> For audit summary/dashboard or before status transitions.</para>
        /// <para><b>Expected endpoint:</b> GET /api/observations/audit/{auditId}/status-summary</para>
        /// </summary>
        /// <param name="auditId">The audit ID to get observation summary for.</param>
        /// <returns>Summary with counts per status (Draft, Submitted, Approved, Rejected).</returns>
        Task<ObservationStatusSummaryDto> GetObservationStatusSummaryAsync(int auditId);

        /// <summary>
        /// Checks whether an audit has at least one observation.
        /// <para><b>When to call:</b> Before allowing audit to move from InProgress to ObservationsSubmitted.</para>
        /// <para><b>Expected endpoint:</b> GET /api/observations/audit/{auditId} (check count > 0)</para>
        /// </summary>
        /// <param name="auditId">The audit ID to check.</param>
        /// <returns>True if at least one observation exists for this audit.</returns>
        Task<bool> HasObservationsAsync(int auditId);
    }
}
