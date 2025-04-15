using AstroCloud.Data.Entities;
using System;
using System.Threading.Tasks;

namespace AstroCloud.Data.Interfaces
{
    namespace AstroCloud.Data.Interfaces
    {
        public interface IUserRepository : IRepository<User>
        {
            Task<User> GetByEmailAsync(string email);
            Task<bool> EmailExistsAsync(string email);

            // Add these new methods
            Task<User> GetByVerificationToken(string token);
            Task<User> GetByResetToken(string token);
        }
    }
}
