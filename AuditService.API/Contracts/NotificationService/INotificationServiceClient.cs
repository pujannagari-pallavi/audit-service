namespace AuditService.API.Contracts.NotificationService
{
    /// <summary>
    /// Contract for communicating with the Notification Service.
    /// The Audit Service needs to send notifications when audit status changes,
    /// approvals are needed, or auditors are assigned.
    /// 
    /// <para><b>Owner:</b> Notification Service Team</para>
    /// <para><b>Status:</b> NOT IMPLEMENTED — Contract only (no HTTP calls yet)</para>
    /// 
    /// <para><b>Required Endpoints from Notification Service:</b></para>
    /// <list type="bullet">
    ///   <item>POST /api/notifications — Send a notification to a user</item>
    ///   <item>POST /api/notifications/batch — Send notifications to multiple users</item>
    /// </list>
    /// 
    /// <para><b>Notifications Triggered by Audit Service:</b></para>
    /// <list type="bullet">
    ///   <item>Audit created → Notify Audit Manager for approval (BRD 4.1)</item>
    ///   <item>Audit approved/rejected → Notify Admin who created it (BRD 4.1)</item>
    ///   <item>Audit scheduled → Notify assigned Auditors (BRD 4.2)</item>
    ///   <item>Audit status changed → Notify relevant stakeholders</item>
    ///   <item>Audit closure requested → Notify Audit Manager (BRD 4.6)</item>
    /// </list>
    /// </summary>
    public interface INotificationServiceClient
    {
        /// <summary>
        /// Sends a notification to a single user.
        /// <para><b>When to call:</b> On audit creation (notify Audit Manager),
        /// on approval/rejection (notify creator), on status transitions.</para>
        /// <para><b>Expected endpoint:</b> POST /api/notifications</para>
        /// </summary>
        /// <param name="notification">The notification details.</param>
        /// <returns>True if the notification was accepted for delivery.</returns>
        Task<bool> SendNotificationAsync(SendNotificationDto notification);

        /// <summary>
        /// Sends notifications to multiple users at once.
        /// <para><b>When to call:</b> When audit is scheduled and all assigned auditors need to be notified.</para>
        /// <para><b>Expected endpoint:</b> POST /api/notifications/batch</para>
        /// </summary>
        /// <param name="notifications">List of notification details for each user.</param>
        /// <returns>True if all notifications were accepted for delivery.</returns>
        Task<bool> SendBatchNotificationsAsync(IEnumerable<SendNotificationDto> notifications);
    }
}
