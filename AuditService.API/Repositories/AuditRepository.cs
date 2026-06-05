using AuditService.API.Data;
using AuditService.API.Enums;
using AuditService.API.Models;
using AuditService.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuditService.API.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _context;

        public AuditRepository(AuditDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Audit>> GetAllAsync()
        {
            return await _context.Audits
                .Include(a => a.AuditAuditors)
                .ToListAsync();
        }

        public async Task<Audit?> GetByIdAsync(int auditId)
        {
            return await _context.Audits
                .Include(a => a.AuditAuditors)
                .FirstOrDefaultAsync(a => a.AuditId == auditId);
        }

        public async Task<Audit?> GetByCodeAsync(string auditCode)
        {
            return await _context.Audits
                .Include(a => a.AuditAuditors)
                .FirstOrDefaultAsync(a => a.AuditCode == auditCode);
        }

        public async Task<IEnumerable<Audit>> GetByStatusAsync(AuditStatus status)
        {
            return await _context.Audits
                .Include(a => a.AuditAuditors)
                .Where(a => a.Status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<Audit>> GetByDepartmentAsync(int departmentId)
        {
            return await _context.Audits
                .Include(a => a.AuditAuditors)
                .Where(a => a.DepartmentId == departmentId)
                .ToListAsync();
        }

        public async Task<Audit> CreateAsync(Audit audit)
        {
            try
            {
                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();
                
                // Reload to get navigation properties
                return await _context.Audits
                    .Include(a => a.AuditAuditors)
                    .FirstAsync(a => a.AuditId == audit.AuditId);
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException($"Database error creating audit: {innerMessage}. AuditCode: {audit.AuditCode}", ex);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException($"Failed to create audit: {innerMessage}", ex);
            }
        }

        public async Task<Audit> UpdateAsync(Audit audit)
        {
            audit.UpdatedAt = DateTime.UtcNow;
            _context.Audits.Update(audit);
            await _context.SaveChangesAsync();
            return audit;
        }

        public async Task<bool> DeleteAsync(int auditId)
        {
            var audit = await _context.Audits.FindAsync(auditId);
            if (audit == null) return false;
            audit.IsDeleted = true;
            audit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddAuditorsAsync(int auditId, List<int> userIds)
        {
            try
            {
                var auditors = userIds.Select(uid => new AuditAuditor
                {
                    AuditId = auditId,
                    UserId = uid
                }).ToList();
                
                _context.AuditAuditors.AddRange(auditors);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add auditors to audit {auditId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateAuditorsAsync(int auditId, List<int> userIds)
        {
            var existing = await _context.AuditAuditors
                .Where(aa => aa.AuditId == auditId)
                .ToListAsync();
            _context.AuditAuditors.RemoveRange(existing);
            await AddAuditorsAsync(auditId, userIds);
        }

        public async Task<IEnumerable<AuditHistory>> GetHistoryAsync(int auditId)
        {
            return await _context.AuditHistories
                .Where(h => h.AuditId == auditId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task AddHistoryAsync(AuditHistory history)
        {
            try
            {
                _context.AuditHistories.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add history for audit {history.AuditId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(int auditId)
        {
            return await _context.Audits
                .AnyAsync(a => a.AuditId == auditId && !a.IsDeleted);
        }

        public async Task<AuditStatus?> GetStatusAsync(int auditId)
        {
            var audit = await _context.Audits
                .Where(a => a.AuditId == auditId && !a.IsDeleted)
                .Select(a => (AuditStatus?)a.Status)
                .FirstOrDefaultAsync();
            return audit;
        }

        public async Task<IEnumerable<Audit>> GetByAuditManagerAndStatusAsync(int auditManagerId, AuditStatus status)
        {
            return await _context.Audits
                .Include(a => a.AuditAuditors)
                .Where(a => a.AuditManagerId == auditManagerId && a.Status == status && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
