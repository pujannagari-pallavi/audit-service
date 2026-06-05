using AuditService.API.Enums;
using AuditService.API.Models;

namespace AuditService.API.Repositories.Interfaces
{
    public interface IAuditRepository
    {
        Task<IEnumerable<Audit>> GetAllAsync();
        Task<Audit?> GetByIdAsync(int auditId);
        Task<Audit?> GetByCodeAsync(string auditCode);
        Task<IEnumerable<Audit>> GetByStatusAsync(AuditStatus status);
        Task<IEnumerable<Audit>> GetByDepartmentAsync(int departmentId);
        Task<Audit> CreateAsync(Audit audit);
        Task<Audit> UpdateAsync(Audit audit);
        Task<bool> DeleteAsync(int auditId);
        Task AddAuditorsAsync(int auditId, List<int> userIds);
        Task UpdateAuditorsAsync(int auditId, List<int> userIds);
        Task<IEnumerable<AuditHistory>> GetHistoryAsync(int auditId);
        Task AddHistoryAsync(AuditHistory history);
        Task<bool> ExistsAsync(int auditId);
        Task<AuditStatus?> GetStatusAsync(int auditId);
        Task<IEnumerable<Audit>> GetByAuditManagerAndStatusAsync(int auditManagerId, AuditStatus status);
    }
}
