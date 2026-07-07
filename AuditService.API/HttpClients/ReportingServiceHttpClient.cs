using System.Net.Http.Json;
using AuditService.API.Contracts.ReportingService;
using System.Diagnostics;

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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Syncing audit to ReportingService | AuditId: {AuditId} | AuditCode: {AuditCode}", 
                    auditSync.AuditId, auditSync.AuditCode);
                
                var response = await _httpClient.PostAsJsonAsync("api/report/audits/sync", auditSync);
                
                if (response.IsSuccessStatusCode)
                {
                    stopwatch.Stop();
                    _logger.LogInformation("Audit synced successfully | AuditId: {AuditId} | ElapsedMs: {ElapsedMs}", auditSync.AuditId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    stopwatch.Stop();
                    _logger.LogWarning("Failed to sync audit to ReportingService | AuditId: {AuditId} | StatusCode: {StatusCode} | ElapsedMs: {ElapsedMs}", 
                        auditSync.AuditId, response.StatusCode, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                // Don't throw - sync failure shouldn't break audit creation
                stopwatch.Stop();
                _logger.LogError(ex, "Error syncing audit to ReportingService | AuditId: {AuditId} | ElapsedMs: {ElapsedMs}", auditSync.AuditId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
