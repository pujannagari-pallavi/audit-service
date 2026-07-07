using System.Text;
using System.Text.Json;
using AuditService.API.Contracts.NotificationService;
using System.Diagnostics;

namespace AuditService.API.HttpClients
{
    public class NotificationServiceHttpClient : INotificationServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationServiceHttpClient> _logger;

        public NotificationServiceHttpClient(HttpClient httpClient, ILogger<NotificationServiceHttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(SendNotificationDto notification)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var payload = new
                {
                    RecipientUserId = notification.RecipientUserId,
                    Title = notification.Title,
                    Type = notification.Type,
                    EntityType = notification.EntityType,
                    EntityId = notification.EntityId,
                    Message = notification.Message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending notification to UserId: {UserId} | EntityId: {EntityId} | Type: {Type}",
                    notification.RecipientUserId, notification.EntityId, notification.Type);

                var response = await _httpClient.PostAsync("api/notification", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    stopwatch.Stop();
                    _logger.LogError("NotificationService returned {StatusCode} in {ElapsedMs} ms | Body: {Body}",
                        response.StatusCode, stopwatch.ElapsedMilliseconds, responseBody);
                    return false;
                }

                stopwatch.Stop();
                _logger.LogInformation("Notification sent successfully to UserId: {UserId} in {ElapsedMs} ms", notification.RecipientUserId, stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to send notification to UserId: {UserId} after {ElapsedMs} ms", notification.RecipientUserId, stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        public async Task<bool> SendBatchNotificationsAsync(IEnumerable<SendNotificationDto> notifications)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var payloads = notifications.Select(n => new
                {
                    RecipientUserId = n.RecipientUserId,
                    Title = n.Title,
                    Type = n.Type,
                    EntityType = n.EntityType,
                    EntityId = n.EntityId,
                    Message = n.Message
                }).ToList();

                var json = JsonSerializer.Serialize(payloads);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending batch notifications | Count: {Count}", payloads.Count);

                var response = await _httpClient.PostAsync("api/notification/bulk", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    stopwatch.Stop();
                    _logger.LogError("NotificationService bulk returned {StatusCode} in {ElapsedMs} ms | Body: {Body}",
                        response.StatusCode, stopwatch.ElapsedMilliseconds, responseBody);
                    return false;
                }

                stopwatch.Stop();
                _logger.LogInformation("Batch notifications sent successfully | Count: {Count} | ElapsedMs: {ElapsedMs}", payloads.Count, stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to send batch notifications after {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        private static string GetNotificationTitle(int notificationType)
        {
            return notificationType switch
            {
                0 => "Approval Requested",
                1 => "Approved",
                2 => "Rejected",
                3 => "Audit Assigned",
                4 => "Action Assigned",
                5 => "Due Date Reminder",
                _ => "Notification"
            };
        }
    }
}
