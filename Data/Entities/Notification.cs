using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AstroCloud.Data.Enum;

namespace AstroCloud.Data.Entities
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [Column("message")]
        [MaxLength(500)]
        public string Message { get; set; }

        [Column("data_payload")]
        public string? DataPayload { get; set; } // JSON data

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("notification_type")]
        public NotificationType NotificationType { get; set; } = NotificationType.General;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }
    }
}