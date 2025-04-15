using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class VerifyEmailDto
    {
        [Required]
        public string Token { get; set; }
    }
}
