using AstroCloud.Data.DTO;
using AstroCloud.Data.Entities;
using AstroCloud.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AstroCloud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            NotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost("push")]
        public async Task<IActionResult> SendPushNotification([FromBody] PushNotificationRequest request)
        {
            try
            {
                await _notificationService.SendPushNotification(request);
                return Ok("Notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
                return StatusCode(500, "Error sending notification");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<NotificationDto>>> GetUserNotifications(Guid userId)
        {
            try
            {
                var notifications = await _notificationService.GetUserNotifications(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notifications");
                return StatusCode(500, "Error getting notifications");
            }
        }

        [HttpPut("mark-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                await _notificationService.MarkAsRead(notificationId);
                return Ok("Notification marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, "Error updating notification");
            }
        }
    }
}