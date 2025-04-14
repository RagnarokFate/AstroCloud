using AstroCloud.Data.Enum;

namespace AstroCloud.Data.DTO
{
    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserType UserType { get; set; }
    }
}
