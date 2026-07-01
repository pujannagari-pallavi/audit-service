using AuditService.API.DTOs;
using AuditService.API.Enums;
using AuditService.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuditService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class AuditsController : ControllerBase
    {
        private readonly IAuditService _service;

        public AuditsController(IAuditService service)
        {
            _service = service;
        }

        // GET api/audits
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
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
            
            return Ok(ApiResponse<IEnumerable<AuditResponseDto>>.SuccessResponse(audits, "Audits retrieved successfully"));
        }

        // GET api/audits/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var audit = await _service.GetAuditByIdAsync(id);
            if (audit == null) return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
            return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit retrieved successfully"));
        }

        // GET api/audits/code/AUD-001
        [HttpGet("code/{auditCode}")]
        public async Task<IActionResult> GetByCode(string auditCode)
        {
            var audit = await _service.GetAuditByCodeAsync(auditCode);
            if (audit == null) return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
            return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit retrieved successfully"));
        }

        // GET api/audits/status/Draft
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(AuditStatus status)
        {
            var audits = await _service.GetAuditsByStatusAsync(status);
            return Ok(ApiResponse<IEnumerable<AuditResponseDto>>.SuccessResponse(audits, "Audits retrieved successfully"));
        }

        // GET api/audits/department/3
        [HttpGet("department/{departmentId:int}")]
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var audits = await _service.GetAuditsByDepartmentAsync(departmentId);
            return Ok(ApiResponse<IEnumerable<AuditResponseDto>>.SuccessResponse(audits, "Audits retrieved successfully"));
        }

        // POST api/audits
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Only Admin can create audits
        public async Task<IActionResult> Create([FromBody] CreateAuditDto dto)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse($"Validation failed: {string.Join(", ", errors)}"));
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(dto.AuditCode))
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Audit code is required"));
                
                if (string.IsNullOrWhiteSpace(dto.AuditName))
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Audit name is required"));
                
                if (dto.DepartmentId <= 0)
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Valid department is required"));
                
                if (dto.CreatedByUserId <= 0)
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Valid creator user ID is required"));
                
                if (dto.StartDate == default)
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("Start date is required"));
                
                if (dto.EndDate == default)
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("End date is required"));
                
                if (dto.EndDate < dto.StartDate)
                    return BadRequest(ApiResponse<AuditResponseDto>.FailResponse("End date must be after start date"));

                var audit = await _service.CreateAuditAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = audit.AuditId }, ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit created successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<AuditResponseDto>.FailResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AuditResponseDto>.FailResponse($"Error creating audit: {ex.Message}"));
            }
        }

        // PUT api/audits/5?changedByUserId=1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAuditDto dto, [FromQuery] int changedByUserId)
        {
            var audit = await _service.UpdateAuditAsync(id, dto, changedByUserId);
            if (audit == null) return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
            return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit updated successfully"));
        }

        // PATCH api/audits/5/status
        [HttpPatch("{id:int}/status")]
        [AllowAnonymous] // Allow service-to-service calls without authentication
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAuditStatusDto dto)
        {
            try
            {
                var audit = await _service.UpdateStatusAsync(id, dto);
                if (audit == null) return NotFound(ApiResponse<AuditResponseDto>.FailResponse("Audit not found"));
                return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(audit, "Audit status updated successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<AuditResponseDto>.FailResponse(ex.Message));
            }
        }

        // POST api/audits/5/submit-for-approval
        // Submit audit for approval by Admin
        [HttpPost("{id:int}/submit-for-approval")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SubmitForApproval(int id, [FromBody] SubmitForApprovalDto dto)
        {
            try
            {
                var audit = await _service.GetAuditByIdAsync(id);
                if (audit == null)
                    return NotFound(ApiResponse<object>.FailResponse("Audit not found"));

                if (audit.Status != "Draft")
                    return BadRequest(ApiResponse<object>.FailResponse("Only audits in Draft status can be submitted for approval"));

                var updateStatusDto = new UpdateAuditStatusDto
                {
                    Status = AuditStatus.PendingApproval,
                    ChangedByUserId = dto.SubmittedByUserId
                };

                var updatedAudit = await _service.UpdateStatusAsync(id, updateStatusDto);
                
                return Ok(ApiResponse<AuditResponseDto>.SuccessResponse(
                    updatedAudit, 
                    "Audit submitted for approval successfully. Audit Manager will review it."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.FailResponse($"Error submitting audit for approval: {ex.Message}"));
            }
        }


        // DELETE api/audits/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAuditAsync(id);
            if (!result) return NotFound(ApiResponse<bool>.FailResponse("Audit not found"));
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Audit deleted successfully"));
        }

        // GET api/audits/5/exists — used by Observation Service to validate AuditId
        [HttpGet("{id:int}/exists")]
        [AllowAnonymous] // Allow service-to-service calls without authentication
        public async Task<IActionResult> Exists(int id)
        {
            var exists = await _service.AuditExistsAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(new { auditId = id, exists }, "Audit existence checked"));
        }

        // GET api/audits/5/status — used by Observation Service to check audit is InProgress
        [HttpGet("{id:int}/status")]
        [AllowAnonymous] // Allow service-to-service calls without authentication
        public async Task<IActionResult> GetStatus(int id)
        {
            var status = await _service.GetAuditStatusAsync(id);
            if (status == null) return NotFound(ApiResponse<object>.FailResponse("Audit not found"));
            return Ok(ApiResponse<object>.SuccessResponse(new { auditId = id, status }, "Audit status retrieved successfully"));
        }

        // GET api/audits/audit-manager/dashboard/{auditManagerId} — Get dashboard for Audit Manager
        [HttpGet("audit-manager/dashboard/{auditManagerId:int}")]
        public async Task<IActionResult> GetAuditManagerDashboard(int auditManagerId)
        {
            var dashboard = await _service.GetAuditManagerDashboardAsync(auditManagerId);
            return Ok(ApiResponse<AuditManagerDashboardDto>.SuccessResponse(dashboard, "Audit Manager dashboard retrieved successfully"));
        }
    }
}
