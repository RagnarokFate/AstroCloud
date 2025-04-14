using AstroCloud.Data.Entities;
using AstroCloud.Data.Interfaces;
using AstroCloud.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AstroCloud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return Ok(await _userRepository.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            if (await _userRepository.EmailExistsAsync(user.Email))
            {
                return Conflict("Email already exists");
            }

            // Hash password here before saving
            var createdUser = await _userRepository.AddAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, User user)
        {
            if (id != user.Id) return BadRequest();

            try
            {
                await _userRepository.UpdateAsync(user);
            }
            catch
            {
                if (!await _userRepository.ExistsAsync(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            if (!await _userRepository.ExistsAsync(id))
                return NotFound();

            await _userRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}