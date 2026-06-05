using System.Net.Http.Json;
using AuditService.API.Contracts.ObservationService;

namespace AuditService.API.HttpClients
{
    public class ObservationServiceHttpClient : IObservationServiceClient
    {
        private readonly HttpClient _httpClient;

        public ObservationServiceHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> AreAllObservationsApprovedAsync(int auditId)
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<AllApprovedResponse>>(
                $"api/Observations/audit/{auditId}/all-approved");
            return apiResponse?.Data?.AllApproved ?? false;
        }

        public async Task<ObservationStatusSummaryDto> GetObservationStatusSummaryAsync(int auditId)
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<ObservationStatusSummaryDto>>(
                $"api/Observations/audit/{auditId}/status-summary");
            return apiResponse?.Data ?? new ObservationStatusSummaryDto { AuditId = auditId };
        }

        public async Task<bool> HasObservationsAsync(int auditId)
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<HasObservationsResponse>>(
                $"api/Observations/audit/{auditId}/has-observations");
            return apiResponse?.Data?.HasObservations ?? false;
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
