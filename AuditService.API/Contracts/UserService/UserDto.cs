namespace AuditService.API.Contracts.UserService
{
    /// <summary>
    /// Represents the expected response from the User Service for a single user.
    /// <para><b>Source:</b> User Service — GET /api/users/{userId}</para>
    /// <para><b>Owner:</b> User Service Team</para>
    /// 
    /// <para><b>Maps from User Service model fields:</b></para>
    /// <list type="bullet">
    ///   <item>UserId (int) — Unique identifier</item>
    ///   <item>Name (string) — Full name</item>
    ///   <item>Email (string) — Unique email</item>
    ///   <item>RoleName (string) — Role (Admin / Auditor / AuditManager / Employee / DepartmentHead)</item>
    ///   <item>DepartmentId (int?) — Department the user belongs to</item>
    ///   <item>Expertise (string?) — Skill/area of the user</item>
    /// </list>
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Unique user identifier from User Service.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Full name of the user.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Role name: Admin, Auditor, AuditManager, Employee, DepartmentHead.
        /// </summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Department the user belongs to (nullable for cross-department users).
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Skill or area of expertise (relevant for auditor assignment).
        /// </summary>
        public string? Expertise { get; set; }
    }
}
