using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using AstroCloud.Data.Enum;

namespace AstroCloud.Data.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        [Column("password")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain a number, an uppercase letter, a lowercase letter, and a special character.")]
        public string Password { get; set; }

        [Required]
        [Column("first_name")]
        public string FirstName { get; set; }

        [Required]
        [Column("last_name")]
        public string LastName { get; set; }

        [Required]
        [Column("gender")]
        [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Gender must be 'Male', 'Female'.")]
        public string Gender { get; set; }

        [Required]
        [Phone]
        [Column("phone")]
        public string PhoneNumber { get; set; }

        [Required]
        [Column("city")]
        public string City { get; set; }

        [Required]
        [Column("zip_code")]
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "ZipCode must be a valid format.")]
        public string ZipCode { get; set; }


        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        [Column("user_token")]
        public string UserToken { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("user_type")]
        public UserType UserType { get; set; } = UserType.Default;
    }
}