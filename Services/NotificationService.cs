using AstroCloud.Data;
using AstroCloud.Data.DTO;
using AstroCloud.Data.Entities;
using AstroCloud.Data.Enum;
using AstroCloud.Data.Interfaces;
using AstroCloud.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class NotificationService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly AppDatabaseContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IConfiguration configuration,
        IUserRepository userRepository,
        AppDatabaseContext context,
        ILogger<NotificationService> logger)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
    }

    public async Task SendPushNotification(PushNotificationRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null || string.IsNullOrEmpty(user.DeviceToken))
            {
                _logger.LogWarning("No device token found for user {UserId}", request.UserId);
                return;
            }

            // Save to database first
            var notification = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Body,
                DataPayload = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                NotificationType = NotificationType.General,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            // Send to Firebase Cloud Messaging (FCM)
            await SendToFcm(user.DeviceToken, request.Title, request.Body, request.Data);

            // Update sent time
            notification.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Push notification sent to user {UserId}", request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            throw;
        }
    }

    private async Task SendToFcm(string deviceToken, string title, string body, Dictionary<string, string>? data)
    {
        var fcmServerKey = _configuration["FCM:ServerKey"];
        var fcmSenderId = _configuration["FCM:SenderId"];

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={fcmServerKey}");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Sender", $"id={fcmSenderId}");

        var message = new
        {
            to = deviceToken,
            notification = new { title, body },
            data
        };

        var content = new StringContent(
            JsonSerializer.Serialize(message),
            Encoding.UTF8,
            "application/json");

        var response = await httpClient.PostAsync("https://fcm.googleapis.com/fcm/send", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<NotificationDto>> GetUserNotifications(Guid userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                DataPayload = n.DataPayload,
                IsRead = n.IsRead,
                NotificationType = n.NotificationType,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    public async Task MarkAsRead(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}