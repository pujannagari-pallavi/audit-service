using AutoMapper;
using AuditService.API.Contracts.NotificationService;
using AuditService.API.Contracts.ObservationService;
using AuditService.API.Contracts.ReportingService;
using AuditService.API.DTOs;
using AuditService.API.Enums;
using AuditService.API.Models;
using AuditService.API.Repositories.Interfaces;
using AuditService.API.Services.Interfaces;

namespace AuditService.API.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _repository;
        private readonly IObservationServiceClient _observationClient;
        private readonly IReportingServiceClient _reportingClient;
        private readonly INotificationServiceClient _notificationClient;
        private readonly ILogger<AuditService> _logger;
        private readonly IMapper _mapper;

        public AuditService(
            IAuditRepository repository, 
            IObservationServiceClient observationClient,
            IReportingServiceClient reportingClient,
            INotificationServiceClient notificationClient,
            ILogger<AuditService> logger, 
            IMapper mapper)
        {
            _repository = repository;
            _observationClient = observationClient;
            _reportingClient = reportingClient;
            _notificationClient = notificationClient;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AuditResponseDto>> GetAllAuditsAsync()
        {
            _logger.LogInformation("Retrieving all audits");
            var audits = await _repository.GetAllAsync();
            _logger.LogInformation("Retrieved {Count} audits", audits.Count());
            return _mapper.Map<IEnumerable<AuditResponseDto>>(audits);
        }

        public async Task<AuditResponseDto?> GetAuditByIdAsync(int auditId)
        {
            _logger.LogInformation("Retrieving audit with ID {AuditId}", auditId);
            var audit = await _repository.GetByIdAsync(auditId);
            if (audit == null)
                _logger.LogWarning("Audit with ID {AuditId} not found", auditId);
            return audit == null ? null : _mapper.Map<AuditResponseDto>(audit);
        }

        public async Task<AuditResponseDto?> GetAuditByCodeAsync(string auditCode)
        {
            _logger.LogInformation("Retrieving audit with code {AuditCode}", auditCode);
            var audit = await _repository.GetByCodeAsync(auditCode);
            if (audit == null)
                _logger.LogWarning("Audit with code {AuditCode} not found", auditCode);
            return audit == null ? null : _mapper.Map<AuditResponseDto>(audit);
        }

        public async Task<IEnumerable<AuditResponseDto>> GetAuditsByStatusAsync(AuditStatus status)
        {
            var audits = await _repository.GetByStatusAsync(status);
            return _mapper.Map<IEnumerable<AuditResponseDto>>(audits);
        }

        public async Task<IEnumerable<AuditResponseDto>> GetAuditsByDepartmentAsync(int departmentId)
        {
            var audits = await _repository.GetByDepartmentAsync(departmentId);
            return _mapper.Map<IEnumerable<AuditResponseDto>>(audits);
        }

        public async Task<AuditResponseDto> CreateAuditAsync(CreateAuditDto dto)
        {
            try
            {
                _logger.LogInformation("Creating audit | Code: {Code} | Name: {Name} | Type: {Type} | Department: {Dept} | CreatedBy: {User} | StartDate: {Start} | EndDate: {End} | Auditors: {Auditors}", 
                    dto.AuditCode, dto.AuditName, dto.AuditType, dto.DepartmentId, dto.CreatedByUserId, dto.StartDate, dto.EndDate, string.Join(",", dto.AuditorUserIds));

                // Check if AuditCode already exists
                var existingAudit = await _repository.GetByCodeAsync(dto.AuditCode);
                if (existingAudit != null)
                {
                    _logger.LogWarning("Audit code {Code} already exists", dto.AuditCode);
                    throw new InvalidOperationException($"Audit code '{dto.AuditCode}' already exists. Please use a unique code.");
                }

                var audit = _mapper.Map<Audit>(dto);
                audit.Status = AuditStatus.Draft; // New audits start in Draft status
                
                // Ensure collections are initialized
                audit.AuditAuditors = new List<AuditAuditor>();
                audit.AuditHistories = new List<AuditHistory>();

                _logger.LogInformation("Mapped audit model | AuditCode: {Code} | StartDate: {Start} | EndDate: {End}", 
                    audit.AuditCode, audit.StartDate, audit.EndDate);

                _logger.LogInformation("Saving audit to database...");
                var created = await _repository.CreateAsync(audit);
                _logger.LogInformation("Audit created with ID {AuditId} and code {AuditCode}", created.AuditId, created.AuditCode);

                if (dto.AuditorUserIds.Any())
                {
                    _logger.LogInformation("Adding {Count} auditors to audit {AuditId}", dto.AuditorUserIds.Count, created.AuditId);
                    await _repository.AddAuditorsAsync(created.AuditId, dto.AuditorUserIds);
                    
                    // Reload audit to get updated auditors for response and sync
                    created = await _repository.GetByIdAsync(created.AuditId) ?? created;
                    _logger.LogInformation("Reloaded audit with {Count} auditors", created.AuditAuditors?.Count ?? 0);
                }

                _logger.LogInformation("Adding history entry for audit {AuditId}", created.AuditId);
                await _repository.AddHistoryAsync(new AuditHistory
                {
                    AuditId = created.AuditId,
                    FieldChanged = "Status",
                    OldValue = string.Empty,
                    NewValue = AuditStatus.Draft.ToString(),
                    ChangedByUserId = dto.CreatedByUserId
                });

                // Sync to ReportingService for dashboard and reports
                var auditResponse = _mapper.Map<AuditResponseDto>(created);
                _logger.LogInformation("Syncing audit {AuditId} to ReportingService with {Count} auditors", 
                    auditResponse.AuditId, auditResponse.AuditorUserIds.Count);
                await SyncToReportingServiceAsync(auditResponse);

                // BRD 4.1: Notify Audit Managers that a new audit has been created and needs approval
                await NotifyAuditCreatedAsync(auditResponse);

                return auditResponse;
            }
            catch (InvalidOperationException)
            {
                // Re-throw business logic exceptions without wrapping
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit | Code: {Code} | Department: {Dept} | Error: {Message}", 
                    dto.AuditCode, dto.DepartmentId, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {InnerMessage}", ex.InnerException.Message);
                    throw new InvalidOperationException($"Error creating audit: {ex.InnerException.Message}", ex);
                }
                throw new InvalidOperationException($"Error creating audit: {ex.Message}", ex);
            }
        }

        public async Task<AuditResponseDto?> UpdateAuditAsync(int auditId, UpdateAuditDto dto, int changedByUserId)
        {
            _logger.LogInformation("Updating audit with ID {AuditId}", auditId);
            var audit = await _repository.GetByIdAsync(auditId);
            if (audit == null)
            {
                _logger.LogWarning("Audit with ID {AuditId} not found for update", auditId);
                return null;
            }

            _mapper.Map(dto, audit);

            await _repository.UpdateAsync(audit);
            await _repository.UpdateAuditorsAsync(auditId, dto.AuditorUserIds);

            // Sync to ReportingService for dashboard and reports
            var auditResponse = _mapper.Map<AuditResponseDto>(audit);
            await SyncToReportingServiceAsync(auditResponse);

            return auditResponse;
        }

        public async Task<bool> DeleteAuditAsync(int auditId)
        {
            _logger.LogInformation("Deleting audit with ID {AuditId}", auditId);
            
            // Get audit before deletion to sync to reporting service
            var audit = await _repository.GetByIdAsync(auditId);
            if (audit == null)
            {
                _logger.LogWarning("Audit with ID {AuditId} not found for deletion", auditId);
                return false;
            }
            
            var result = await _repository.DeleteAsync(auditId);
            if (result)
            {
                // Sync deletion to ReportingService
                var auditResponse = _mapper.Map<AuditResponseDto>(audit);
                await SyncToReportingServiceAsync(auditResponse, isDeleted: true);
                _logger.LogInformation("Audit {AuditId} deletion synced to ReportingService", auditId);
            }
            
            return result;
        }

        public async Task<AuditResponseDto?> UpdateStatusAsync(int auditId, UpdateAuditStatusDto dto)
        {
            _logger.LogInformation("Updating status of audit {AuditId} to {NewStatus}", auditId, dto.Status);
            var audit = await _repository.GetByIdAsync(auditId);
            if (audit == null)
            {
                _logger.LogWarning("Audit with ID {AuditId} not found for status update", auditId);
                return null;
            }

            // Validate status transitions that depend on Observation Service
            await ValidateStatusTransitionAsync(audit, dto.Status);

            var oldStatus = audit.Status.ToString();
            audit.Status = dto.Status;
            await _repository.UpdateAsync(audit);
            _logger.LogInformation("Audit {AuditId} status changed from {OldStatus} to {NewStatus}", auditId, oldStatus, dto.Status);

            await _repository.AddHistoryAsync(new AuditHistory
            {
                AuditId = auditId,
                FieldChanged = "Status",
                OldValue = oldStatus,
                NewValue = dto.Status.ToString(),
                ChangedByUserId = dto.ChangedByUserId
            });

            // Sync updated status to ReportingService
            var updatedAuditResponse = _mapper.Map<AuditResponseDto>(audit);
            await SyncToReportingServiceAsync(updatedAuditResponse);

            // Send notifications based on the new status
            await NotifyOnStatusChangeAsync(updatedAuditResponse, oldStatus, dto.ChangedByUserId);

            return updatedAuditResponse;
        }

        private async Task ValidateStatusTransitionAsync(Audit audit, AuditStatus newStatus)
        {
            // BRD 4.2: Cannot move to ObservationsSubmitted unless observations exist
            if (newStatus == AuditStatus.ObservationsSubmitted)
            {
                _logger.LogInformation("Validating observations exist for audit {AuditId}", audit.AuditId);
                var hasObservations = await _observationClient.HasObservationsAsync(audit.AuditId);
                if (!hasObservations)
                {
                    _logger.LogWarning("Status transition blocked: no observations found for audit {AuditId}", audit.AuditId);
                    throw new InvalidOperationException("Cannot submit observations: no observations found for this audit.");
                }
            }

            // BRD 4.3: Cannot move to FindingsApproved unless all observations are approved
            if (newStatus == AuditStatus.FindingsApproved)
            {
                _logger.LogInformation("Validating all observations approved for audit {AuditId}", audit.AuditId);
                var allApproved = await _observationClient.AreAllObservationsApprovedAsync(audit.AuditId);
                if (!allApproved)
                {
                    _logger.LogWarning("Status transition blocked: not all observations approved for audit {AuditId}", audit.AuditId);
                    throw new InvalidOperationException("Cannot approve findings: not all observations are approved.");
                }
            }

            // BRD 4.6: Cannot complete/close audit unless all observations are approved
            if (newStatus == AuditStatus.Completed)
            {
                _logger.LogInformation("Validating all observations approved for audit completion {AuditId}", audit.AuditId);
                var allApproved = await _observationClient.AreAllObservationsApprovedAsync(audit.AuditId);
                if (!allApproved)
                {
                    _logger.LogWarning("Status transition blocked: cannot complete audit {AuditId}, not all observations approved", audit.AuditId);
                    throw new InvalidOperationException("Cannot complete audit: not all observations are approved.");
                }
            }
        }

        public async Task<bool> AuditExistsAsync(int auditId)
        {
            return await _repository.ExistsAsync(auditId);
        }

        public async Task<string?> GetAuditStatusAsync(int auditId)
        {
            var status = await _repository.GetStatusAsync(auditId);
            return status?.ToString();
        }

        /// <summary>
        /// BRD 4.1: When an audit is created (Draft), notify Audit Managers for approval.
        /// </summary>
        private async Task NotifyAuditCreatedAsync(AuditResponseDto audit)
        {
            try
            {
                // Notify Audit Manager (userId may need to come from UserService in future)
                // For now, we send a notification that any AuditManager can see
                _logger.LogInformation("Sending audit-created notification for AuditId: {AuditId} | AuditCode: {AuditCode}",
                    audit.AuditId, audit.AuditCode);

                await _notificationClient.SendNotificationAsync(new SendNotificationDto
                {
                    RecipientUserId = audit.CreatedByUserId, // Notify the creator as confirmation
                    Title = "Audit Created",
                    Type = 0, // ApprovalRequested
                    EntityType = 0, // Audit
                    EntityId = audit.AuditId,
                    Message = $"Audit '{audit.AuditName}' ({audit.AuditCode}) has been created in Draft status. Submit it for approval when ready."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audit-created notification for AuditId: {AuditId}", audit.AuditId);
                // Don't throw - notification failure shouldn't break audit creation
            }
        }

        /// <summary>
        /// Sends notifications based on audit status transitions:
        /// - PendingApproval: Notify Audit Managers
        /// - Scheduled: Notify assigned Auditors (BRD 4.2)
        /// - Approved/Rejected via status change: Notify the creator
        /// </summary>
        private async Task NotifyOnStatusChangeAsync(AuditResponseDto audit, string oldStatus, int changedByUserId)
        {
            try
            {
                switch (audit.Status)
                {
                    case "PendingApproval":
                        // BRD 4.1: Audit submitted for approval → Notify Audit Manager
                        _logger.LogInformation("Sending PendingApproval notification to Audit Manager (UserId: {AuditManagerId}) for AuditId: {AuditId}", 
                            audit.AuditManagerId, audit.AuditId);
                        await _notificationClient.SendNotificationAsync(new SendNotificationDto
                        {
                            RecipientUserId = audit.AuditManagerId, // Notify assigned Audit Manager
                            Title = "Audit Submitted for Approval",
                            Type = 0, // ApprovalRequested
                            EntityType = 0, // Audit
                            EntityId = audit.AuditId,
                            Message = $"Audit '{audit.AuditName}' ({audit.AuditCode}) has been submitted for your approval."
                        });
                        break;

                    case "Scheduled":
                        // BRD 4.2: Audit approved and scheduled → Notify all assigned Auditors
                        if (audit.AuditorUserIds.Any())
                        {
                            _logger.LogInformation("Sending Scheduled notifications to {Count} auditors for AuditId: {AuditId}",
                                audit.AuditorUserIds.Count, audit.AuditId);

                            var notifications = audit.AuditorUserIds.Select(auditorId => new SendNotificationDto
                            {
                                RecipientUserId = auditorId,
                                Title = "Audit Assigned",
                                Type = 3, // AuditAssigned
                                EntityType = 0, // Audit
                                EntityId = audit.AuditId,
                                Message = $"You have been assigned to audit '{audit.AuditName}' ({audit.AuditCode}). The audit is now scheduled from {audit.StartDate} to {audit.EndDate}."
                            });

                            await _notificationClient.SendBatchNotificationsAsync(notifications);
                        }

                        // Also notify the creator that their audit was approved
                        await _notificationClient.SendNotificationAsync(new SendNotificationDto
                        {
                            RecipientUserId = audit.CreatedByUserId,
                            Title = "Audit Approved",
                            Type = 1, // ApprovalApproved
                            EntityType = 0, // Audit
                            EntityId = audit.AuditId,
                            Message = $"Audit '{audit.AuditName}' ({audit.AuditCode}) has been approved and scheduled."
                        });
                        break;

                    case "PendingClosure":
                        // All corrective actions closed by Dept Head → Notify Audit Manager to close the audit
                        _logger.LogInformation("Sending PendingClosure notification to Audit Manager (UserId: {AuditManagerId}) for AuditId: {AuditId}", 
                            audit.AuditManagerId, audit.AuditId);
                        await _notificationClient.SendNotificationAsync(new SendNotificationDto
                        {
                            RecipientUserId = audit.AuditManagerId,
                            Title = "Audit Ready for Closure",
                            Type = 0, // ApprovalRequested
                            EntityType = 0, // Audit
                            EntityId = audit.AuditId,
                            Message = $"All corrective actions for audit '{audit.AuditName}' ({audit.AuditCode}) have been closed. Please review and close the audit."
                        });
                        break;

                    case "Draft":
                        // Audit rejected (moved back to Draft from PendingApproval) → Notify Creator
                        if (oldStatus == "PendingApproval")
                        {
                            _logger.LogInformation("Sending rejection notification for AuditId: {AuditId}", audit.AuditId);
                            await _notificationClient.SendNotificationAsync(new SendNotificationDto
                            {
                                RecipientUserId = audit.CreatedByUserId,
                                Title = "Audit Rejected",
                                Type = 2, // ApprovalRejected
                                EntityType = 0, // Audit
                                EntityId = audit.AuditId,
                                Message = $"Audit '{audit.AuditName}' ({audit.AuditCode}) has been rejected by the Audit Manager. Please review and resubmit."
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status-change notification for AuditId: {AuditId} | NewStatus: {Status}",
                    audit.AuditId, audit.Status);
                // Don't throw - notification failure shouldn't break status update
            }
        }

        private async Task SyncToReportingServiceAsync(AuditResponseDto audit, bool isDeleted = false)
        {
            try
            {
                _logger.LogInformation("Preparing to sync audit {AuditId} to ReportingService | IsDeleted: {IsDeleted} | AuditorUserIds: {Auditors}", 
                    audit.AuditId, isDeleted, string.Join(",", audit.AuditorUserIds));

                var syncDto = new AuditSyncDto
                {
                    AuditId = audit.AuditId,
                    AuditCode = audit.AuditCode,
                    AuditName = audit.AuditName,
                    AuditType = audit.AuditType,
                    DepartmentId = audit.DepartmentId,
                    DepartmentName = string.Empty, // Will need to fetch from UserService if needed
                    CreatedByUserId = audit.CreatedByUserId,
                    CreatedByUserName = string.Empty, // Will need to fetch from UserService if needed
                    AssignedAuditorUserIds = string.Join(",", audit.AuditorUserIds),
                    Status = audit.Status,
                    StartDate = audit.StartDate,
                    EndDate = audit.EndDate,
                    TotalObservations = 0, // Updated by ObservationService
                    TotalActions = 0, // Updated by ActionService
                    ClosedActions = 0, // Updated by ActionService
                    IsDeleted = isDeleted
                };

                _logger.LogInformation("Sending sync request | AssignedAuditorUserIds: '{AuditorIds}'", syncDto.AssignedAuditorUserIds);
                await _reportingClient.SyncAuditAsync(syncDto);
                _logger.LogInformation("Successfully synced audit {AuditId} to ReportingService", audit.AuditId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync audit {AuditId} to ReportingService", audit.AuditId);
                // Don't throw - sync failure shouldn't break audit operations
            }
        }

        public async Task<AuditManagerDashboardDto> GetAuditManagerDashboardAsync(int auditManagerId)
        {
            _logger.LogInformation("Getting dashboard for Audit Manager {AuditManagerId}", auditManagerId);

            var pendingApprovalAudits = await _repository.GetByAuditManagerAndStatusAsync(auditManagerId, AuditStatus.PendingApproval);
            var inProgressAudits = await _repository.GetByAuditManagerAndStatusAsync(auditManagerId, AuditStatus.InProgress);
            var pendingClosureAudits = await _repository.GetByAuditManagerAndStatusAsync(auditManagerId, AuditStatus.PendingClosure);

            // For InProgress, also include Scheduled, ObservationsSubmitted, and FindingsApproved
            var scheduledAudits = await _repository.GetByAuditManagerAndStatusAsync(auditManagerId, AuditStatus.Scheduled);
            var observationsSubmittedAudits = await _repository.GetByAuditManagerAndStatusAsync(auditManagerId, AuditStatus.ObservationsSubmitted);
            var findingsApprovedAudits = await _repository.GetByAuditManagerAndStatusAsync(auditManagerId, AuditStatus.FindingsApproved);

            var allInProgress = inProgressAudits
                .Concat(scheduledAudits)
                .Concat(observationsSubmittedAudits)
                .Concat(findingsApprovedAudits)
                .ToList();

            return new AuditManagerDashboardDto
            {
                PendingApproval = pendingApprovalAudits.Select(a => _mapper.Map<AuditResponseDto>(a)).ToList(),
                InProgress = allInProgress.Select(a => _mapper.Map<AuditResponseDto>(a)).ToList(),
                PendingClosure = pendingClosureAudits.Select(a => _mapper.Map<AuditResponseDto>(a)).ToList(),
                TotalPendingApproval = pendingApprovalAudits.Count(),
                TotalInProgress = allInProgress.Count(),
                TotalPendingClosure = pendingClosureAudits.Count()
            };
        }

        public async Task<int> SyncAllAuditsAsync()
        {
            _logger.LogInformation("Starting bulk sync of all audits to ReportingService");
            var audits = await _repository.GetAllAsync();
            int syncedCount = 0;

            foreach (var audit in audits)
            {
                try
                {
                    var auditResponse = _mapper.Map<AuditResponseDto>(audit);
                    await SyncToReportingServiceAsync(auditResponse);
                    syncedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync audit {AuditId} during bulk sync", audit.AuditId);
                }
            }

            _logger.LogInformation("Bulk sync complete. Synced {Count} audits to ReportingService", syncedCount);
            return syncedCount;
        }
    }
}
