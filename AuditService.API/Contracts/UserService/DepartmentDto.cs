namespace AuditService.API.Contracts.UserService
{
    /// <summary>
    /// Represents the expected response from the User Service for a department.
    /// <para><b>Source:</b> User Service — GET /api/departments/{departmentId}</para>
    /// <para><b>Owner:</b> User Service Team</para>
    /// 
    /// <para><b>Maps from User Service model fields:</b></para>
    /// <list type="bullet">
    ///   <item>DepartmentId (int) — Unique identifier</item>
    ///   <item>DepartmentName (string) — Name of the department</item>
    /// </list>
    /// </summary>
    public class DepartmentDto
    {
        /// <summary>
        /// Unique department identifier from User Service.
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Name of the department.
        /// </summary>
        public string DepartmentName { get; set; } = string.Empty;
    }
}
