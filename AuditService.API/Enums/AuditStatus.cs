namespace AuditService.API.Enums
{
    public enum AuditStatus
    {
        Draft = 1,
        PendingApproval = 2,
        Scheduled = 3,
        InProgress = 4,
        ObservationsSubmitted = 5,
        FindingsApproved = 6,
        PendingClosure = 7,  // All actions closed by Dept Head, waiting for Audit Manager to close
        Closed = 8,
        Completed = 9
    }
}
