using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    // Data/DTOs/ForgetPasswordDto.cs
    public class ForgetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string CurrentPassword { get; set; }

        [Required, MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain a number, an uppercase letter, a lowercase letter, and a special character.")]
        public string NewPassword { get; set; }

        [Required, Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
        public string ConfirmNewPassword { get; set; }
    }

}
