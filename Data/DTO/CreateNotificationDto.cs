using AstroCloud.Data.Enum;

namespace AstroCloud.Data.DTO
{
    public class CreateNotificationDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string? DataPayload { get; set; }
        public NotificationType NotificationType { get; set; } = NotificationType.General;
    }
}
