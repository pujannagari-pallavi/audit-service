using AuditService.API.Data;
using AuditService.API.Enums;
using AuditService.API.Models;
using AuditService.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AuditService.API.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _context;
        private readonly ILogger<AuditRepository> _logger;

        public AuditRepository(AuditDbContext context, ILogger<AuditRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Audit>> GetAllAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetAllAsync started");
            var audits = await _context.Audits
                .Include(a => a.AuditAuditors)
                .ToListAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.GetAllAsync completed in {ElapsedMs} ms with {Count} records", stopwatch.ElapsedMilliseconds, audits.Count);
            return audits;
        }

        public async Task<Audit?> GetByIdAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetByIdAsync started for AuditId {AuditId}", auditId);
            var audit = await _context.Audits
                .Include(a => a.AuditAuditors)
                .FirstOrDefaultAsync(a => a.AuditId == auditId);
            stopwatch.Stop();
            if (audit == null)
            {
                _logger.LogWarning("AuditRepository.GetByIdAsync found no record for AuditId {AuditId}", auditId);
            }
            else
            {
                _logger.LogInformation("AuditRepository.GetByIdAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, auditId);
            }
            return audit;
        }

        public async Task<Audit?> GetByCodeAsync(string auditCode)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetByCodeAsync started for AuditCode {AuditCode}", auditCode);
            var audit = await _context.Audits
                .Include(a => a.AuditAuditors)
                .FirstOrDefaultAsync(a => a.AuditCode == auditCode);
            stopwatch.Stop();
            if (audit == null)
            {
                _logger.LogWarning("AuditRepository.GetByCodeAsync found no record for AuditCode {AuditCode}", auditCode);
            }
            else
            {
                _logger.LogInformation("AuditRepository.GetByCodeAsync completed in {ElapsedMs} ms for AuditCode {AuditCode}", stopwatch.ElapsedMilliseconds, auditCode);
            }
            return audit;
        }

        public async Task<IEnumerable<Audit>> GetByStatusAsync(AuditStatus status)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetByStatusAsync started for Status {Status}", status);
            var audits = await _context.Audits
                .Include(a => a.AuditAuditors)
                .Where(a => a.Status == status)
                .ToListAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.GetByStatusAsync completed in {ElapsedMs} ms with {Count} records for Status {Status}", stopwatch.ElapsedMilliseconds, audits.Count, status);
            return audits;
        }

        public async Task<IEnumerable<Audit>> GetByDepartmentAsync(int departmentId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetByDepartmentAsync started for DepartmentId {DepartmentId}", departmentId);
            var audits = await _context.Audits
                .Include(a => a.AuditAuditors)
                .Where(a => a.DepartmentId == departmentId)
                .ToListAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.GetByDepartmentAsync completed in {ElapsedMs} ms with {Count} records for DepartmentId {DepartmentId}", stopwatch.ElapsedMilliseconds, audits.Count, departmentId);
            return audits;
        }

        public async Task<Audit> CreateAsync(Audit audit)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.CreateAsync started for AuditCode {AuditCode}", audit.AuditCode);
            try
            {
                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();
                
                // Reload to get navigation properties
                var createdAudit = await _context.Audits
                    .Include(a => a.AuditAuditors)
                    .FirstAsync(a => a.AuditId == audit.AuditId);
                stopwatch.Stop();
                _logger.LogInformation("AuditRepository.CreateAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, createdAudit.AuditId);
                return createdAudit;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "AuditRepository.CreateAsync database error for AuditCode {AuditCode}", audit.AuditCode);
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException($"Database error creating audit: {innerMessage}. AuditCode: {audit.AuditCode}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditRepository.CreateAsync failed for AuditCode {AuditCode}", audit.AuditCode);
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException($"Failed to create audit: {innerMessage}", ex);
            }
        }

        public async Task<Audit> UpdateAsync(Audit audit)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.UpdateAsync started for AuditId {AuditId}", audit.AuditId);
            audit.UpdatedAt = DateTime.UtcNow;
            _context.Audits.Update(audit);
            await _context.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.UpdateAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, audit.AuditId);
            return audit;
        }

        public async Task<bool> DeleteAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.DeleteAsync started for AuditId {AuditId}", auditId);
            var audit = await _context.Audits.FindAsync(auditId);
            if (audit == null)
            {
                _logger.LogWarning("AuditRepository.DeleteAsync found no record for AuditId {AuditId}", auditId);
                return false;
            }
            audit.IsDeleted = true;
            audit.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.DeleteAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, auditId);
            return true;
        }

        public async Task AddAuditorsAsync(int auditId, List<int> userIds)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.AddAuditorsAsync started for AuditId {AuditId} with {Count} users", auditId, userIds.Count);
            try
            {
                var auditors = userIds.Select(uid => new AuditAuditor
                {
                    AuditId = auditId,
                    UserId = uid
                }).ToList();
                
                _context.AuditAuditors.AddRange(auditors);
                await _context.SaveChangesAsync();
                stopwatch.Stop();
                _logger.LogInformation("AuditRepository.AddAuditorsAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, auditId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditRepository.AddAuditorsAsync failed for AuditId {AuditId}", auditId);
                throw new InvalidOperationException($"Failed to add auditors to audit {auditId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateAuditorsAsync(int auditId, List<int> userIds)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.UpdateAuditorsAsync started for AuditId {AuditId} with {Count} users", auditId, userIds.Count);
            var existing = await _context.AuditAuditors
                .Where(aa => aa.AuditId == auditId)
                .ToListAsync();
            _context.AuditAuditors.RemoveRange(existing);
            await AddAuditorsAsync(auditId, userIds);
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.UpdateAuditorsAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, auditId);
        }

        public async Task<IEnumerable<AuditHistory>> GetHistoryAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetHistoryAsync started for AuditId {AuditId}", auditId);
            var history = await _context.AuditHistories
                .Where(h => h.AuditId == auditId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.GetHistoryAsync completed in {ElapsedMs} ms with {Count} records for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, history.Count, auditId);
            return history;
        }

        public async Task AddHistoryAsync(AuditHistory history)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.AddHistoryAsync started for AuditId {AuditId}", history.AuditId);
            try
            {
                _context.AuditHistories.Add(history);
                await _context.SaveChangesAsync();
                stopwatch.Stop();
                _logger.LogInformation("AuditRepository.AddHistoryAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, history.AuditId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditRepository.AddHistoryAsync failed for AuditId {AuditId}", history.AuditId);
                throw new InvalidOperationException($"Failed to add history for audit {history.AuditId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            var exists = await _context.Audits
                .AnyAsync(a => a.AuditId == auditId && !a.IsDeleted);
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.ExistsAsync completed in {ElapsedMs} ms for AuditId {AuditId} with Result {Exists}", stopwatch.ElapsedMilliseconds, auditId, exists);
            return exists;
        }

        public async Task<AuditStatus?> GetStatusAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetStatusAsync started for AuditId {AuditId}", auditId);
            var audit = await _context.Audits
                .Where(a => a.AuditId == auditId && !a.IsDeleted)
                .Select(a => (AuditStatus?)a.Status)
                .FirstOrDefaultAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.GetStatusAsync completed in {ElapsedMs} ms for AuditId {AuditId} with Status {Status}", stopwatch.ElapsedMilliseconds, auditId, audit);
            return audit;
        }

        public async Task<IEnumerable<Audit>> GetByAuditManagerAndStatusAsync(int auditManagerId, AuditStatus status)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditRepository.GetByAuditManagerAndStatusAsync started for AuditManagerId {AuditManagerId} and Status {Status}", auditManagerId, status);
            var audits = await _context.Audits
                .Include(a => a.AuditAuditors)
                .Where(a => a.AuditManagerId == auditManagerId && a.Status == status && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            stopwatch.Stop();
            _logger.LogInformation("AuditRepository.GetByAuditManagerAndStatusAsync completed in {ElapsedMs} ms with {Count} records", stopwatch.ElapsedMilliseconds, audits.Count);
            return audits;
        }
    }
}
