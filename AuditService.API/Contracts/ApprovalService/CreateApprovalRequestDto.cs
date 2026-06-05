namespace AuditService.API.Contracts.ApprovalService
{
    /// <summary>
    /// Represents the request payload to create a new approval request in the Approval Service.
    /// <para><b>Target:</b> Approval Service — POST /api/approvals</para>
    /// <para><b>Owner:</b> Approval Service Team</para>
    /// 
    /// <para><b>Field Mapping (from Approval Service architecture doc):</b></para>
    /// <list type="bullet">
    ///   <item>EntityType — "Audit" (since Audit Service is the caller)</item>
    ///   <item>EntityId — The AuditId being submitted for approval</item>
    ///   <item>ApprovalType — "AuditCreation" or "AuditClosure"</item>
    ///   <item>RequestedByUserId — The user submitting the approval request</item>
    /// </list>
    /// </summary>
    public class CreateApprovalRequestDto
    {
        /// <summary>
        /// The type of entity being approved. Always "Audit" for Audit Service calls.
        /// </summary>
        public string EntityType { get; set; } = "Audit";

        /// <summary>
        /// The ID of the entity (AuditId) being submitted for approval.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// The type of approval being requested.
        /// Values: "AuditCreation" (BRD 4.1) or "AuditClosure" (BRD 4.6).
        /// </summary>
        public string ApprovalType { get; set; } = string.Empty;

        /// <summary>
        /// The user ID of the person requesting the approval.
        /// </summary>
        public int RequestedByUserId { get; set; }
    }
}
