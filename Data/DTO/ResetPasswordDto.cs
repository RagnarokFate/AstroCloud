using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
    }
}
