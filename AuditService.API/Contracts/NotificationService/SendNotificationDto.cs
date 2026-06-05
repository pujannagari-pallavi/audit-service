namespace AuditService.API.Contracts.NotificationService
{
    /// <summary>
    /// Represents the request payload to send a notification via the Notification Service.
    /// <para><b>Target:</b> Notification Service — POST /api/notification</para>
    /// <para><b>Owner:</b> Notification Service Team</para>
    /// </summary>
    public class SendNotificationDto
    {
        /// <summary>
        /// The user ID of the notification recipient.
        /// </summary>
        public int RecipientUserId { get; set; }

        /// <summary>
        /// The notification title.
        /// Example: "Audit Submitted for Approval"
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notification type (maps to NotificationType enum in NotificationService):
        /// 0 = ApprovalRequested, 1 = ApprovalApproved, 2 = ApprovalRejected, 3 = AuditAssigned, 4 = ActionAssigned, 5 = DueDateReminder
        /// </summary>
public int Type { get; set; }

        /// <summary>
        /// Entity type (maps to EntityType enum in NotificationService):
        /// 0 = Audit, 1 = Observation, 2 = Action
        /// </summary>
        public int EntityType { get; set; }

        /// <summary>
        /// The entity (audit) ID this notification relates to.
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// The notification message content.
        /// Example: "Audit AUD-2026-001 has been submitted for your approval."
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
