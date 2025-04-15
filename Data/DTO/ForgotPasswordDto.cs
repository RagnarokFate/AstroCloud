using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
