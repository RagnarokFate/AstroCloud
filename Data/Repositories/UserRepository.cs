using AstroCloud.Data.Entities;
using AstroCloud.Data.Interfaces.AstroCloud.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AstroCloud.Data.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly AppDatabaseContext _context;

        public UserRepository(AppDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        // Implement the new methods
        public async Task<User> GetByVerificationToken(string token)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.VerificationToken == token);
        }

        public async Task<User> GetByResetToken(string token)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.ResetToken == token &&
                                        u.ResetTokenExpires > DateTime.UtcNow);
        }
    }
}