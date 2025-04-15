namespace AstroCloud.Data.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmail(string email, string token);
        Task SendPasswordResetEmail(string email, string token);
    }
}
