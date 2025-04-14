namespace AstroCloud.Data.DTO
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public UserResponseDto User { get; set; }
    }
}