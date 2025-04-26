namespace AstroCloud.Data.DTO
{
    public class PushNotificationRequest
    {
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string>? Data { get; set; }
    }
}
