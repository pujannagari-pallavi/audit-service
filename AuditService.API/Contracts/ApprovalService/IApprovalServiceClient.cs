namespace AuditService.API.Contracts.ApprovalService
{
    /// <summary>
    /// Contract for communicating with the Approval Service.
    /// The Audit Service needs to create and query approval requests
    /// for two approval gates in the audit lifecycle:
    ///   1. Audit Creation Approval (Draft → PendingApproval → Scheduled)
    ///   2. Final Audit Closure Approval (before marking Completed)
    /// 
    /// <para><b>Owner:</b> Approval Service Team</para>
    /// <para><b>Status:</b> NOT IMPLEMENTED — Contract only (no HTTP calls yet)</para>
    /// 
    /// <para><b>Required Endpoints from Approval Service:</b></para>
    /// <list type="bullet">
    ///   <item>POST /api/approvals — Create a new approval request</item>
    ///   <item>GET /api/approvals/{approvalId} — Get approval status</item>
    ///   <item>GET /api/approvals/entity/{entityType}/{entityId} — Get approval for a specific entity</item>
    /// </list>
    /// 
    /// <para><b>Approval Types Used by Audit Service:</b></para>
    /// <list type="bullet">
    ///   <item><b>AuditCreation</b> — Admin submits audit for Audit Manager approval (BRD 4.1)</item>
    ///   <item><b>AuditClosure</b> — Final sign-off by Audit Manager (BRD 4.6)</item>
    /// </list>
    /// 
    /// <para><b>Expected Flow:</b></para>
    /// <list type="number">
    ///   <item>Audit Service calls CreateApprovalRequest when audit is submitted for approval.</item>
    ///   <item>Approval Service notifies the approver (via Notification Service).</item>
    ///   <item>When approved/rejected, Approval Service calls back or Audit Service polls for status.</item>
    ///   <item>Audit Service transitions audit status based on the result.</item>
    /// </list>
    /// </summary>
    public interface IApprovalServiceClient
    {
        /// <summary>
        /// Creates a new approval request for an audit.
        /// <para><b>When to call:</b></para>
        /// <list type="bullet">
        ///   <item>When admin submits a Draft audit for approval (AuditCreation type).</item>
        ///   <item>When Audit Manager initiates final audit closure (AuditClosure type).</item>
        /// </list>
        /// <para><b>Expected endpoint:</b> POST /api/approvals</para>
        /// </summary>
        /// <param name="request">The approval request details.</param>
        /// <returns>The created approval request with its ID and initial Pending status.</returns>
        Task<ApprovalResponseDto> CreateApprovalRequestAsync(CreateApprovalRequestDto request);

        /// <summary>
        /// Gets the current status of an approval request.
        /// <para><b>When to call:</b> To check if an audit's approval has been decided.</para>
        /// <para><b>Expected endpoint:</b> GET /api/approvals/{approvalId}</para>
        /// </summary>
        /// <param name="approvalId">The approval request ID.</param>
        /// <returns>Current approval details including status (Pending/Approved/Rejected).</returns>
        Task<ApprovalResponseDto?> GetApprovalByIdAsync(int approvalId);

        /// <summary>
        /// Gets the latest approval request for a specific audit.
        /// <para><b>When to call:</b> To check approval status without knowing the approval ID.</para>
        /// <para><b>Expected endpoint:</b> GET /api/approvals/entity/Audit/{auditId}?type={approvalType}</para>
        /// </summary>
        /// <param name="auditId">The audit entity ID.</param>
        /// <param name="approvalType">The type of approval (e.g., "AuditCreation" or "AuditClosure").</param>
        /// <returns>The latest approval for this audit and type, or null if none exists.</returns>
        Task<ApprovalResponseDto?> GetApprovalForAuditAsync(int auditId, string approvalType);
    }
}
