using System.Net.Http.Json;
using AuditService.API.Contracts.ReportingService;

namespace AuditService.API.HttpClients
{
    public class ReportingServiceHttpClient : IReportingServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportingServiceHttpClient> _logger;

        public ReportingServiceHttpClient(HttpClient httpClient, ILogger<ReportingServiceHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SyncAuditAsync(AuditSyncDto auditSync)
        {
            try
            {
                _logger.LogInformation("Syncing audit to ReportingService | AuditId: {AuditId} | AuditCode: {AuditCode}", 
                    auditSync.AuditId, auditSync.AuditCode);
                
                var response = await _httpClient.PostAsJsonAsync("api/report/audits/sync", auditSync);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Audit synced successfully | AuditId: {AuditId}", auditSync.AuditId);
                }
                else
                {
                    _logger.LogWarning("Failed to sync audit to ReportingService | AuditId: {AuditId} | StatusCode: {StatusCode}", 
                        auditSync.AuditId, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                // Don't throw - sync failure shouldn't break audit creation
                _logger.LogError(ex, "Error syncing audit to ReportingService | AuditId: {AuditId}", auditSync.AuditId);
            }
        }
    }
}
