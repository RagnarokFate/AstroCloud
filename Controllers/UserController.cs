using AstroCloud.Data.DTO;
using AstroCloud.Data.Entities;
using AstroCloud.Data.Enum;
using AstroCloud.Data.Interfaces;
using AstroCloud.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AstroCloud.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthService _authService;

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
        public async Task<ActionResult<UserResponseDto>> CreateUser(UserCreateDto userDto)
        {
            if (await _userRepository.EmailExistsAsync(userDto.Email))
            {
                return Conflict("Email already exists");
            }

            // Hash password
            var passwordHash = PasswordService.HashPassword(userDto.Password);
            var now = DateTime.UtcNow; // Get current time once

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = userDto.Email,
                Password = passwordHash,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Gender = userDto.Gender,
                PhoneNumber = userDto.PhoneNumber,
                City = userDto.City,
                ZipCode = userDto.ZipCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = now,
                DeviceToken = "",
                IsActive = true,
                UserType = UserType.Default
            };

            await _userRepository.AddAsync(user);
            // Generate JWT token
            var token = _authService.GenerateToken(user);

            /*return CreatedAtAction(nameof(GetUser),
                new { id = user.Id },
                MapToResponseDto(user));*/
            return Ok(new
            {
                User = MapToResponseDto(user),
                Token = token
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UserUpdateDto userDto)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Update only the allowed fields
            existingUser.Email = userDto.Email;
            existingUser.FirstName = userDto.FirstName;
            existingUser.LastName = userDto.LastName;
            existingUser.Gender = userDto.Gender;
            existingUser.PhoneNumber = userDto.PhoneNumber;
            existingUser.City = userDto.City;
            existingUser.ZipCode = userDto.ZipCode;
            existingUser.UpdatedAt = DateTime.UtcNow;

            // Only update password if provided
            if (!string.IsNullOrEmpty(userDto.Password))
            {
                existingUser.Password = PasswordService.HashPassword(userDto.Password);
            }

            try
            {
                await _userRepository.UpdateAsync(existingUser);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _userRepository.ExistsAsync(id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            if (!await _userRepository.ExistsAsync(id))
                return NotFound();

            await _userRepository.DeleteAsync(id);
            return NoContent();
        }


        // NEW ENDPOINT: Register Device Token
        [HttpPost("{id}/device-token")]
        [Authorize] // Requires authentication
        public async Task<IActionResult> RegisterDeviceToken(Guid id, [FromBody] DeviceTokenDto tokenDto)
        {
            // Verify user exists and matches authenticated user
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Simple validation
            if (string.IsNullOrWhiteSpace(tokenDto.Token))
            {
                return BadRequest("Device token is required");
            }

            // Update device token
            user.DeviceToken = tokenDto.Token;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return NoContent();
        }

        // NEW ENDPOINT: Get Device Token
        [HttpGet("{id}/device-token")]
        [Authorize]
        public async Task<ActionResult<string>> GetDeviceToken(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { DeviceToken = user.DeviceToken });
        }

        private string GenerateDeviceToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private UserResponseDto MapToResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                // Map other properties you want to return
                CreatedAt = user.CreatedAt,
                DeviceToken = user.DeviceToken
            };
        }
        
    }

}