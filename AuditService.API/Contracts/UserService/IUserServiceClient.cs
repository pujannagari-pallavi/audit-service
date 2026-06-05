namespace AuditService.API.Contracts.UserService
{
    /// <summary>
    /// Contract for communicating with the User Service.
    /// This interface defines the API calls that the Audit Service
    /// will need from the User Service once it is available.
    /// 
    /// <para><b>Owner:</b> User Service Team</para>
    /// <para><b>Status:</b> NOT IMPLEMENTED — Contract only (no HTTP calls yet)</para>
    /// 
    /// <para><b>Required Endpoints from User Service:</b></para>
    /// <list type="bullet">
    ///   <item>GET /api/users/{userId} — Returns user details (name, email, role, department)</item>
    ///   <item>GET /api/users/{userId}/exists — Returns whether a user exists</item>
    ///   <item>GET /api/users/{userId}/role — Returns the role of a user</item>
    ///   <item>GET /api/departments/{departmentId} — Returns department details</item>
    ///   <item>GET /api/departments/{departmentId}/exists — Returns whether a department exists</item>
    /// </list>
    /// </summary>
    public interface IUserServiceClient
    {
        /// <summary>
        /// Validates that a user exists in the User Service.
        /// <para><b>When to call:</b> Before creating an audit (validate CreatedByUserId),
        /// and before assigning auditors (validate each UserId in AuditAuditor).</para>
        /// <para><b>Expected endpoint:</b> GET /api/users/{userId}/exists</para>
        /// </summary>
        /// <param name="userId">The user ID to validate.</param>
        /// <returns>True if the user exists and is not soft-deleted; otherwise false.</returns>
        Task<bool> UserExistsAsync(int userId);

        /// <summary>
        /// Gets the basic details of a user (name, email, role).
        /// <para><b>When to call:</b> When enriching audit responses to show creator name,
        /// auditor names instead of just IDs.</para>
        /// <para><b>Expected endpoint:</b> GET /api/users/{userId}</para>
        /// </summary>
        /// <param name="userId">The user ID to look up.</param>
        /// <returns>User details, or null if the user doesn't exist.</returns>
        Task<UserDto?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Gets multiple users by their IDs in a single call.
        /// <para><b>When to call:</b> When enriching audit responses that have multiple auditors,
        /// to avoid N+1 HTTP calls.</para>
        /// <para><b>Expected endpoint:</b> POST /api/users/batch or GET /api/users?ids=1,2,3</para>
        /// </summary>
        /// <param name="userIds">The list of user IDs to look up.</param>
        /// <returns>List of user details for the given IDs.</returns>
        Task<IEnumerable<UserDto>> GetUsersByIdsAsync(IEnumerable<int> userIds);

        /// <summary>
        /// Validates that the given user has the specified role.
        /// <para><b>When to call:</b> To enforce that only Admins can create audits,
        /// only Auditors can be assigned to audits, only Audit Managers can approve.</para>
        /// <para><b>Expected endpoint:</b> GET /api/users/{userId}/role</para>
        /// </summary>
        /// <param name="userId">The user ID to check.</param>
        /// <param name="roleName">Expected role name (e.g., "Admin", "Auditor", "AuditManager").</param>
        /// <returns>True if the user has the specified role.</returns>
        Task<bool> UserHasRoleAsync(int userId, string roleName);

        /// <summary>
        /// Validates that a department exists in the User Service.
        /// <para><b>When to call:</b> Before creating an audit (validate DepartmentId).</para>
        /// <para><b>Expected endpoint:</b> GET /api/departments/{departmentId}/exists</para>
        /// </summary>
        /// <param name="departmentId">The department ID to validate.</param>
        /// <returns>True if the department exists.</returns>
        Task<bool> DepartmentExistsAsync(int departmentId);

        /// <summary>
        /// Gets department details by ID.
        /// <para><b>When to call:</b> When enriching audit responses to show department name
        /// instead of just DepartmentId.</para>
        /// <para><b>Expected endpoint:</b> GET /api/departments/{departmentId}</para>
        /// </summary>
        /// <param name="departmentId">The department ID to look up.</param>
        /// <returns>Department details, or null if not found.</returns>
        Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId);
    }
}
