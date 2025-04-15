using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class DeviceTokenDto
    {
        [Required]
        [StringLength(255, ErrorMessage = "Device token must be 255 characters or less")]
        public string Token { get; set; }
    }
}
