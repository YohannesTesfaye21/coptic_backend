using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace coptic_app_backend.Infrastructure.Services
{
    public class FCMNotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _fcmProjectId;
        private readonly string _fcmServiceAccountJson;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<FCMNotificationService> _logger;

        public FCMNotificationService(HttpClient httpClient, IConfiguration configuration, IUserRepository userRepository, ILogger<FCMNotificationService> logger)
        {
            _httpClient = httpClient;
            _fcmProjectId = configuration["FCM:ProjectId"] ?? "";
            _fcmServiceAccountJson = configuration["FCM:ServiceAccountJson"] ?? "";
            _userRepository = userRepository;
            _logger = logger;
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

                var response = await _httpClient.PostAsync(
                    $"https://fcm.googleapis.com/v1/projects/{_fcmProjectId}/messages:send",
                    content
                );

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
