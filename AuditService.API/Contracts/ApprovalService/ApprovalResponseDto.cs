namespace AuditService.API.Contracts.ApprovalService
{
    /// <summary>
    /// Represents the expected response from the Approval Service for an approval request.
    /// <para><b>Source:</b> Approval Service — GET /api/approvals/{approvalId}</para>
    /// <para><b>Owner:</b> Approval Service Team</para>
    /// 
    /// <para><b>Status Values:</b> Pending, Approved, Rejected</para>
    /// 
    /// <para><b>Used by Audit Service to determine:</b></para>
    /// <list type="bullet">
    ///   <item>If Approved → Transition audit from PendingApproval to Scheduled (creation approval)</item>
    ///   <item>If Approved → Transition audit to Completed (closure approval)</item>
    ///   <item>If Rejected → Transition audit back to Draft with comments (creation approval)</item>
    /// </list>
    /// </summary>
    public class ApprovalResponseDto
    {
        /// <summary>
        /// Unique approval request ID.
        /// </summary>
        public int ApprovalId { get; set; }

        /// <summary>
        /// Entity type: "Audit", "Observation", or "Action".
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the entity (AuditId for Audit Service calls).
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// The type of approval: "AuditCreation", "AuditClosure", etc.
        /// </summary>
        public string ApprovalType { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the approval: "Pending", "Approved", or "Rejected".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The user who requested this approval.
        /// </summary>
        public int RequestedByUserId { get; set; }

        /// <summary>
        /// Optional comments from the approver (especially on rejection).
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// When the approval was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the approval was last updated (approved/rejected).
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
