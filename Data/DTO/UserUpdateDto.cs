using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class UserUpdateDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")]
        public string? Password { get; set; } // Optional for updates

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [RegularExpression(@"^(Male|Female)$")]
        public string Gender { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        [RegularExpression(@"^\d{5}(-\d{4})?$")]
        public string ZipCode { get; set; }
    }
}
