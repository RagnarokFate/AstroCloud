namespace AstroCloud.Data.DTO
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}