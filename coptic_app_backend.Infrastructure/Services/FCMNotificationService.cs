using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using coptic_app_backend.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using FirebaseAdmin.Messaging;

namespace coptic_app_backend.Infrastructure.Services
{
    public class FCMNotificationService : INotificationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<FCMNotificationService> _logger;

        public FCMNotificationService(IUserRepository userRepository, ILogger<FCMNotificationService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(string deviceToken, string title, string body)
        {
            try
            {
                // Guard against null or empty parameters
                if (string.IsNullOrEmpty(deviceToken) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
                {
                    _logger.LogWarning("DeviceToken, Title, or Body is null/empty");
                    return false;
                }
                
                _logger.LogInformation("Sending notification to device token: {DeviceToken}, title: {Title}", deviceToken, title);

                var message = new Message()
                {
                    Token = deviceToken,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>()
                    {
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                    }
                };

                if (FirebaseMessaging.DefaultInstance == null)
                {
                    _logger.LogError("FirebaseApp was not initialized. Cannot send notification.");
                    return false;
                }
                
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Successfully sent message: " + response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to device token {DeviceToken}: {Message}", deviceToken, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendNotificationToAllAsync(string title, string body)
        {
            try
            {
                _logger.LogInformation("Sending notification to all users, title: {Title}", title);

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
                    if (user.Id == null) continue;
                    
                    try
                    {
                        var success = await SendNotificationAsync(user.Id, title, body);
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
