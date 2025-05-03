using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class EmailVerificationRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
