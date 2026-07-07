using System.Net.Http.Json;
using AuditService.API.Contracts.ObservationService;
using System.Diagnostics;

namespace AuditService.API.HttpClients
{
    public class ObservationServiceHttpClient : IObservationServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ObservationServiceHttpClient> _logger;

        public ObservationServiceHttpClient(HttpClient httpClient, ILogger<ObservationServiceHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> AreAllObservationsApprovedAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("ObservationServiceHttpClient.AreAllObservationsApprovedAsync started for AuditId {AuditId}", auditId);
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<AllApprovedResponse>>(
                $"api/Observations/audit/{auditId}/all-approved");
            var result = apiResponse?.Data?.AllApproved ?? false;
            stopwatch.Stop();
            _logger.LogInformation("ObservationServiceHttpClient.AreAllObservationsApprovedAsync completed in {ElapsedMs} ms for AuditId {AuditId} with Result {Result}", stopwatch.ElapsedMilliseconds, auditId, result);
            return result;
        }

        public async Task<ObservationStatusSummaryDto> GetObservationStatusSummaryAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("ObservationServiceHttpClient.GetObservationStatusSummaryAsync started for AuditId {AuditId}", auditId);
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<ObservationStatusSummaryDto>>(
                $"api/Observations/audit/{auditId}/status-summary");
            var result = apiResponse?.Data ?? new ObservationStatusSummaryDto { AuditId = auditId };
            stopwatch.Stop();
            _logger.LogInformation("ObservationServiceHttpClient.GetObservationStatusSummaryAsync completed in {ElapsedMs} ms for AuditId {AuditId}", stopwatch.ElapsedMilliseconds, auditId);
            return result;
        }

        public async Task<bool> HasObservationsAsync(int auditId)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("ObservationServiceHttpClient.HasObservationsAsync started for AuditId {AuditId}", auditId);
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<HasObservationsResponse>>(
                $"api/Observations/audit/{auditId}/has-observations");
            var result = apiResponse?.Data?.HasObservations ?? false;
            stopwatch.Stop();
            _logger.LogInformation("ObservationServiceHttpClient.HasObservationsAsync completed in {ElapsedMs} ms for AuditId {AuditId} with Result {Result}", stopwatch.ElapsedMilliseconds, auditId, result);
            return result;
        }

        // Wrapper class for API responses
        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
            public object? Errors { get; set; }
        }

        private class AllApprovedResponse
        {
            public int AuditId { get; set; }
            public bool AllApproved { get; set; }
        }

        private class HasObservationsResponse
        {
            public int AuditId { get; set; }
            public bool HasObservations { get; set; }
        }
    }
}
