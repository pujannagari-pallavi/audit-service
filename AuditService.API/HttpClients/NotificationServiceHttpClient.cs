using System.Text;
using System.Text.Json;
using AuditService.API.Contracts.NotificationService;

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
                    _logger.LogError("NotificationService returned {StatusCode} | Body: {Body}",
                        response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Notification sent successfully to UserId: {UserId}", notification.RecipientUserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to UserId: {UserId}", notification.RecipientUserId);
                return false;
            }
        }

        public async Task<bool> SendBatchNotificationsAsync(IEnumerable<SendNotificationDto> notifications)
        {
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
                    _logger.LogError("NotificationService bulk returned {StatusCode} | Body: {Body}",
                        response.StatusCode, responseBody);
                    return false;
                }

                _logger.LogInformation("Batch notifications sent successfully | Count: {Count}", payloads.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch notifications");
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
