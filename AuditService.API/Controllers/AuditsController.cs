using AuditService.API.DTOs;
using AuditService.API.Enums;
using AuditService.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace AuditService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class AuditsController : ControllerBase
    {
        private readonly IAuditService _service;
        private readonly ILogger<AuditsController> _logger;

        public AuditsController(IAuditService service, ILogger<AuditsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET api/audits
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetAll started");
            // Get user role from JWT token
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            
            var audits = await _service.GetAllAuditsAsync();
            
            // Filter audits based on role
            if (userRole == "Auditor" && int.TryParse(userId, out int auditorUserId))
            {
                // Auditors can only see audits that are Scheduled or later AND they are assigned to
                audits = audits.Where(a => 
                    (a.Status == "Scheduled" || a.Status == "InProgress" || 
                     a.Status == "ObservationsSubmitted" || a.Status == "FindingsApproved" || 
                     a.Status == "Completed" || a.Status == "Closed") &&
                    a.AuditorUserIds.Contains(auditorUserId)
                ).ToList();
            }

            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetAll completed in {ElapsedMs} ms with {Count} audits for role {Role}", stopwatch.ElapsedMilliseconds, audits.Count(), userRole ?? "unknown");
            
            return Ok(ApiResponse<IEnumerable<AuditResponseDto>>.SuccessResponse(audits, "Audits retrieved successfully"));
        }

        // GET api/audits/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetById started for AuditId {AuditId}", id);
            var audit = await _service.GetAuditByIdAsync(id);
            if (audit == null)
            {
                _logger.LogWarning("AuditController.GetById not found for AuditId {AuditId}", id);
                return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
            }
            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetById completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, id);
            return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit retrieved successfully"));
        }

        // GET api/audits/code/AUD-001
        [HttpGet("code/{auditCode}")]
        public async Task<IActionResult> GetByCode(string auditCode)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetByCode started for AuditCode {AuditCode}", auditCode);
            var audit = await _service.GetAuditByCodeAsync(auditCode);
            if (audit == null)
            {
                _logger.LogWarning("AuditController.GetByCode not found for AuditCode {AuditCode}", auditCode);
                return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
            }
            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetByCode completed in {ElapsedMs} ms for AuditCode {AuditCode}", stopwatch.ElapsedMilliseconds, auditCode);
            return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit retrieved successfully"));
        }

        // GET api/audits/status/Draft
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(AuditStatus status)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetByStatus started for Status {Status}", status);
            var audits = await _service.GetAuditsByStatusAsync(status);
            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetByStatus completed in {ElapsedMs} ms with {Count} records", stopwatch.ElapsedMilliseconds, audits.Count());
            return Ok(ApiResponse<IEnumerable<AuditResponseDto>>.SuccessResponse(audits, "Audits retrieved successfully"));
        }

        // GET api/audits/department/3
        [HttpGet("department/{departmentId:int}")]
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetByDepartment started for DepartmentId {DepartmentId}", departmentId);
            var audits = await _service.GetAuditsByDepartmentAsync(departmentId);
            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetByDepartment completed in {ElapsedMs} ms with {Count} records for DepartmentId {DepartmentId}", stopwatch.ElapsedMilliseconds, audits.Count(), departmentId);
            return Ok(ApiResponse<IEnumerable<AuditResponseDto>>.SuccessResponse(audits, "Audits retrieved successfully"));
        }

        // POST api/audits
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Only Admin can create audits
        public async Task<IActionResult> Create([FromBody] CreateAuditDto dto)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.Create started for AuditCode {AuditCode}", dto.AuditCode);
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    _logger.LogWarning("AuditController.Create model validation failed for AuditCode {AuditCode}", dto.AuditCode);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse($"Validation failed: {string.Join(", ", errors)}"));
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(dto.AuditCode))
                {
                    _logger.LogWarning("AuditController.Create validation failed because AuditCode is missing");
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Audit code is required"));
                }
                
                if (string.IsNullOrWhiteSpace(dto.AuditName))
                {
                    _logger.LogWarning("AuditController.Create validation failed because AuditName is missing for AuditCode {AuditCode}", dto.AuditCode);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Audit name is required"));
                }
                
                if (dto.DepartmentId <= 0)
                {
                    _logger.LogWarning("AuditController.Create validation failed because DepartmentId {DepartmentId} is invalid", dto.DepartmentId);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Valid department is required"));
                }
                
                if (dto.CreatedByUserId <= 0)
                {
                    _logger.LogWarning("AuditController.Create validation failed because CreatedByUserId is invalid for AuditCode {AuditCode}", dto.AuditCode);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Valid creator user ID is required"));
                }
                
                if (dto.StartDate == default)
                {
                    _logger.LogWarning("AuditController.Create validation failed because StartDate is missing for AuditCode {AuditCode}", dto.AuditCode);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Start date is required"));
                }
                
                if (dto.EndDate == default)
                {
                    _logger.LogWarning("AuditController.Create validation failed because EndDate is missing for AuditCode {AuditCode}", dto.AuditCode);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("End date is required"));
                }
                
                if (dto.EndDate < dto.StartDate)
                {
                    _logger.LogWarning("AuditController.Create validation failed because EndDate is before StartDate for AuditCode {AuditCode}", dto.AuditCode);
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("End date must be after start date"));
                }

                var audit = await _service.CreateAuditAsync(dto);
                stopwatch.Stop();
                _logger.LogInformation("AuditController.Create completed in {ElapsedMs} ms for AuditId {AuditId} and AuditCode {AuditCode}", stopwatch.ElapsedMilliseconds, audit.AuditId, audit.AuditCode);
                return CreatedAtAction(nameof(GetById), new { id = audit.AuditId }, ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AuditController.Create business validation failed for AuditCode {AuditCode}", dto.AuditCode);
                return BadRequest(ApiResponse<AuditResponseDto>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditController.Create failed unexpectedly for AuditCode {AuditCode}", dto.AuditCode);
                return StatusCode(500, ApiResponse<AuditResponseDto>.FailResponse($"Error creating audit: {ex.Message}"));
            }
        }

        // PUT api/audits/5?changedByUserId=1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAuditDto dto, [FromQuery] int changedByUserId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.Update started for AuditId {AuditId} by UserId {ChangedByUserId}", id, changedByUserId);
            var audit = await _service.UpdateAuditAsync(id, dto, changedByUserId);
            if (audit == null)
            {
                _logger.LogWarning("AuditController.Update not found for AuditId {AuditId}", id);
                return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
            }
            stopwatch.Stop();
            _logger.LogInformation("AuditController.Update completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, id);
            return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit updated successfully"));
        }

        // PATCH api/audits/5/status
        [HttpPatch("{id:int}/status")]
        [AllowAnonymous] // Allow service-to-service calls without authentication
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAuditStatusDto dto)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.UpdateStatus started for AuditId {AuditId} to Status {Status}", id, dto.Status);
            try
            {
                var audit = await _service.UpdateStatusAsync(id, dto);
                if (audit == null)
                {
                    _logger.LogWarning("AuditController.UpdateStatus not found for AuditId {AuditId}", id);
                    return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
                }
                stopwatch.Stop();
                _logger.LogInformation("AuditController.UpdateStatus completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, id);
                return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit status updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AuditController.UpdateStatus validation failed for AuditId {AuditId} and Status {Status}", id, dto.Status);
                return BadRequest(ApiResponse<AuditResponseDto>.FailResponse(ex.Message));
            }
        }

        // POST api/audits/5/submit-for-approval
        // Submit audit for approval by Admin
        [HttpPost("{id:int}/submit-for-approval")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SubmitForApproval(int id, [FromBody] SubmitForApprovalDto dto)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.SubmitForApproval started for AuditId {AuditId} by UserId {SubmittedByUserId}", id, dto.SubmittedByUserId);
            try
            {
                var audit = await _service.GetAuditByIdAsync(id);
                if (audit == null)
                {
                    _logger.LogWarning("AuditController.SubmitForApproval not found for AuditId {AuditId}", id);
                    return NotFound(ApiResponse<object>.FailResponse("Audit not found"));
                }

                if (audit.Status != "Draft")
                {
                    _logger.LogWarning("AuditController.SubmitForApproval rejected for AuditId {AuditId} because current status is {CurrentStatus}", id, audit.Status);
                    return BadRequest(ApiResponse<object>.FailResponse("Only audits in Draft status can be submitted for approval"));
                }

                var updateStatusDto = new UpdateAuditStatusDto
                {
                    Status = AuditStatus.PendingApproval,
                    ChangedByUserId = dto.SubmittedByUserId
                };

                var updatedAudit = await _service.UpdateStatusAsync(id, updateStatusDto);
                stopwatch.Stop();
                _logger.LogInformation("AuditController.SubmitForApproval completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, id);
                
                return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(
                    updatedAudit, 
                    "Audit submitted for approval successfully. Audit Manager will review it."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditController.SubmitForApproval failed for AuditId {AuditId}", id);
                return StatusCode(500, ApiResponse<object>.FailResponse($"Error submitting audit for approval: {ex.Message}"));
            }
        }


        // DELETE api/audits/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.Delete started for AuditId {AuditId}", id);
            var result = await _service.DeleteAuditAsync(id);
            if (!result)
            {
                _logger.LogWarning("AuditController.Delete not found for AuditId {AuditId}", id);
                return NotFound(ApiResponse<bool>.FailResponse("Audit not found"));
            }
            stopwatch.Stop();
            _logger.LogInformation("AuditController.Delete completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Audit deleted successfully"));
        }

        // GET api/audits/5/exists — used by Observation Service to validate AuditId
        [HttpGet("{id:int}/exists")]
        [AllowAnonymous] // Allow service-to-service calls without authentication
        public async Task<IActionResult> Exists(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.Exists started for AuditId {AuditId}", id);
            var exists = await _service.AuditExistsAsync(id);
            stopwatch.Stop();
            _logger.LogInformation("AuditController.Exists completed in {ElapsedMs} ms for AuditId {AuditId} with Result {Exists}", stopwatch.ElapsedMilliseconds, id, exists);
            return Ok(ApiResponse<object>.SuccessResponse(new { auditId = id, exists }, "Audit existence checked"));
        }

        // GET api/audits/5/status — used by Observation Service to check audit is InProgress
        [HttpGet("{id:int}/status")]
        [AllowAnonymous] // Allow service-to-service calls without authentication
        public async Task<IActionResult> GetStatus(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetStatus started for AuditId {AuditId}", id);
            var status = await _service.GetAuditStatusAsync(id);
            if (status == null)
            {
                _logger.LogWarning("AuditController.GetStatus not found for AuditId {AuditId}", id);
                return NotFound(ApiResponse<object>.FailResponse("Audit not found"));
            }
            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetStatus completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, id);
            return Ok(ApiResponse<object>.SuccessResponse(new { auditId = id, status }, "Audit status retrieved successfully"));
        }

        // GET api/audits/audit-manager/dashboard/{auditManagerId} — Get dashboard for Audit Manager
        [HttpGet("audit-manager/dashboard/{auditManagerId:int}")]
        public async Task<IActionResult> GetAuditManagerDashboard(int auditManagerId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("AuditController.GetAuditManagerDashboard started for AuditManagerId {AuditManagerId}", auditManagerId);
            var dashboard = await _service.GetAuditManagerDashboardAsync(auditManagerId);
            stopwatch.Stop();
            _logger.LogInformation("AuditController.GetAuditManagerDashboard completed in {ElapsedMs} ms for AuditManagerId {AuditManagerId}", stopwatch.ElapsedMilliseconds, auditManagerId);
            return Ok(ApiResponse<AuditManagerDashboardDto>.SuccessResponse(dashboard, "Audit Manager dashboard retrieved successfully"));
        }
    }
}
