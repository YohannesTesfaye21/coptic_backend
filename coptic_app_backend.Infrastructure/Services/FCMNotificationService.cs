using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using System.Linq;

namespace coptic_app_backend.Infrastructure.Services
{
    public class FCMNotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _fcmProjectId;
        private readonly string _fcmServiceAccountJson;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiryUtc = DateTime.MinValue;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<FCMNotificationService> _logger;

        public FCMNotificationService(HttpClient httpClient, IConfiguration configuration, IUserRepository userRepository, ILogger<FCMNotificationService> logger)
        {
            _httpClient = httpClient;

            // Be resilient to different environment variable formats set by CI/CD and docker-compose
            _fcmProjectId =
                configuration["FCM:ProjectId"]
                ?? configuration["FCM__ProjectId"]
                ?? configuration["FCM_PROJECT_ID"]
                ?? Environment.GetEnvironmentVariable("FCM__ProjectId")
                ?? Environment.GetEnvironmentVariable("FCM_PROJECT_ID")
                ?? string.Empty;

            _fcmServiceAccountJson =
                configuration["FCM:ServiceAccountJson"]
                ?? configuration["FCM__ServiceAccountJson"]
                ?? configuration["FCM_SERVICE_ACCOUNT_JSON"]
                ?? Environment.GetEnvironmentVariable("FCM__ServiceAccountJson")
                ?? Environment.GetEnvironmentVariable("FCM_SERVICE_ACCOUNT_JSON")
                ?? string.Empty;

            _userRepository = userRepository;
            _logger = logger;
            
            // Debug logging to see what configuration values we're getting
            _logger.LogInformation("FCM Configuration - ProjectId: '{ProjectId}', ServiceAccountJson: '{ServiceAccountJson}'", 
                string.IsNullOrEmpty(_fcmProjectId) ? "[EMPTY]" : _fcmProjectId,
                string.IsNullOrEmpty(_fcmServiceAccountJson) ? "[EMPTY]" : _fcmServiceAccountJson);
        }

        private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiryUtc.AddMinutes(-2))
                {
                    return _cachedAccessToken;
                }

                GoogleCredential credential;
                if (!string.IsNullOrWhiteSpace(_fcmServiceAccountJson) && _fcmServiceAccountJson.TrimStart().StartsWith("{"))
                {
                    _logger.LogInformation("Using FCM credentials from JSON content");
                    credential = GoogleCredential.FromJson(_fcmServiceAccountJson);
                }
                else if (!string.IsNullOrWhiteSpace(_fcmServiceAccountJson))
                {
                    _logger.LogInformation("Using FCM credentials from file: {FilePath}", _fcmServiceAccountJson);
                    credential = GoogleCredential.FromFile(_fcmServiceAccountJson);
                }
                else
                {
                    _logger.LogWarning("No FCM credentials configured. ProjectId: '{ProjectId}', ServiceAccountJson: '{ServiceAccountJson}'. Falling back to Application Default Credentials.", 
                        string.IsNullOrEmpty(_fcmProjectId) ? "[EMPTY]" : _fcmProjectId,
                        string.IsNullOrEmpty(_fcmServiceAccountJson) ? "[EMPTY]" : _fcmServiceAccountJson);
                    credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
                }

                var scoped = credential.CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                var token = await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);

                // We don't get expiry via GetAccessTokenForRequestAsync; set short cache window
                _cachedAccessToken = token;
                _tokenExpiryUtc = DateTime.UtcNow.AddMinutes(45);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain FCM access token");
                return null;
            }
        }

        public async Task<bool> SendNotificationAsync(string userId, string title, string body)
        {
            try
            {
                _logger.LogInformation("Sending notification to user: {UserId}, title: {Title}", userId, title);
                
                // Get user's device token from Cognito
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                var deviceToken = user.DeviceToken;
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("No device token found for user: {UserId}", userId);
                    return false;
                }

                var message = new
                {
                    message = new
                    {
                        token = deviceToken,
                        notification = new
                        {
                            title = title,
                            body = body
                        },
                        data = new Dictionary<string, string>
                        {
                            ["userId"] = userId,
                            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("Skipping FCM send due to missing access token");
                    return false;
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://fcm.googleapis.com/v1/projects/{_fcmProjectId}/messages:send");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = content;
                var response = await _httpClient.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                _logger.LogInformation("Notification sent to user {UserId}: {Success}", userId, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}: {Message}", userId, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendNotificationToAllAsync(string title, string body)
        {
            try
            {
                _logger.LogInformation("Sending notification to all users, title: {Title}", title);
                
                // Get all users with device tokens
                var users = await _userRepository.GetUsersAsync();
                var usersWithTokens = users.Where(u => !string.IsNullOrEmpty(u.DeviceToken)).ToList();
                
                if (!usersWithTokens.Any())
                {
                    _logger.LogWarning("No users with device tokens found");
                    return false;
                }

                var successCount = 0;
                foreach (var user in usersWithTokens)
                {
                    try
                    {
                        var success = await SendNotificationAsync(user.Id!, title, body);
                        if (success) successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending notification to user {UserId}", user.Id);
                    }
                }

                _logger.LogInformation("Sent notifications to {SuccessCount}/{TotalCount} users", successCount, usersWithTokens.Count);
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to all users: {Message}", ex.Message);
                return false;
            }
        }
    }
}
