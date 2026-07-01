using AuditService.API.DTOs;
using AuditService.API.Enums;

namespace AuditService.API.Services.Interfaces
{
    public interface IAuditService
    {
        Task<IEnumerable<AuditResponseDto>> GetAllAuditsAsync();
        Task<AuditResponseDto?> GetAuditByIdAsync(int auditId);
        Task<AuditResponseDto?> GetAuditByCodeAsync(string auditCode);
        Task<IEnumerable<AuditResponseDto>> GetAuditsByStatusAsync(AuditStatus status);
        Task<IEnumerable<AuditResponseDto>> GetAuditsByDepartmentAsync(int departmentId);
        Task<AuditResponseDto> CreateAuditAsync(CreateAuditDto dto);
        Task<AuditResponseDto?> UpdateAuditAsync(int auditId, UpdateAuditDto dto, int changedByUserId);
        Task<bool> DeleteAuditAsync(int auditId);
        Task<AuditResponseDto?> UpdateStatusAsync(int auditId, UpdateAuditStatusDto dto);
        Task<bool> AuditExistsAsync(int auditId);
        Task<string?> GetAuditStatusAsync(int auditId);
        Task<AuditManagerDashboardDto> GetAuditManagerDashboardAsync(int auditManagerId);
    }
}
