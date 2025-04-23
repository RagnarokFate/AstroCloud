using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class PhoneVerificationRequestDto
    {
        [Required]
        [Phone]
        public string PhoneNumber { get; set; }
    }
}
