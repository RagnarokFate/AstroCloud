using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AstroCloud.Data.DTO;
using AstroCloud.Data.Entities;
using AstroCloud.Data.Enum;
using AstroCloud.Data.Interfaces;
using AstroCloud.Data.Repositories;
using AstroCloud.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AstroCloud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly AuthService _authService;
        private readonly RateLimitService _rateLimitService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepository userRepository, AuthService authService,
            RateLimitService rateLimitService, ILogger<UserController> logger, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _authService = authService;
            _rateLimitService = rateLimitService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            _logger.LogInformation("GetUsers: Fetching all users");
            return Ok(await _userRepository.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            _logger.LogInformation("GetUser: Fetching user with ID {UserId}", id);
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(UserCreateDto userDto)
        {
            _logger.LogInformation("CreateUser: Creating user with email {Email}", userDto.Email);

            if (await _userRepository.EmailExistsAsync(userDto.Email))
            {
                _logger.LogWarning("CreateUser: Email {Email} already exists", userDto.Email);
                return Conflict("Email already exists");
            }

            var passwordHash = PasswordService.HashPassword(userDto.Password);
            var now = DateTime.UtcNow;

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
                CreatedAt = now,
                UpdatedAt = now,
                DeviceToken = "",
                IsActive = true,
                UserType = UserType.Default,
                IsEmailVerified = false,
                IsPhoneVerified = false
            };

            await _userRepository.AddAsync(user);

            return Ok(new
            {
                User = MapToResponseDto(user),
                Message = "User created successfully. Please verify your email and phone number."
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UserUpdateDto userDto)
        {
            _logger.LogInformation("UpdateUser: Updating user {UserId} with new data {@UserDto}", id, userDto);

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("UpdateUser: User {UserId} not found", id);
                return NotFound();
            }

            existingUser.Email = userDto.Email;
            existingUser.FirstName = userDto.FirstName;
            existingUser.LastName = userDto.LastName;
            existingUser.Gender = userDto.Gender;
            existingUser.PhoneNumber = userDto.PhoneNumber;
            existingUser.City = userDto.City;
            existingUser.ZipCode = userDto.ZipCode;
            existingUser.UpdatedAt = DateTime.UtcNow;

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
            _logger.LogInformation("DeleteUser: Deleting user with ID {UserId}", id);

            if (!await _userRepository.ExistsAsync(id))
            {
                _logger.LogWarning("DeleteUser: User {UserId} not found", id);
                return NotFound();
            }

            await _userRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/device-token")]
        [Authorize]
        public async Task<IActionResult> RegisterDeviceToken(Guid id, [FromBody] DeviceTokenDto tokenDto)
        {
            _logger.LogInformation("RegisterDeviceToken: Registering device token for user {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(tokenDto.Token))
            {
                _logger.LogWarning("RegisterDeviceToken: Empty token provided for user {UserId}", id);
                return BadRequest("Device token is required");
            }

            user.DeviceToken = tokenDto.Token;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return NoContent();
        }

        [HttpGet("{id}/device-token")]
        [Authorize]
        public async Task<ActionResult<string>> GetDeviceToken(Guid id)
        {
            _logger.LogInformation("GetDeviceToken: Getting device token for user {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { DeviceToken = user.DeviceToken });
        }

        [HttpPost("send-email-verification")]
        public async Task<IActionResult> SendEmailVerification([FromBody] EmailVerificationRequestDto request)
        {
            _logger.LogInformation("SendEmailVerification: Sending verification email to {Email}", request.Email);

            if (_rateLimitService.IsRateLimited($"email_{request.Email}", 5, TimeSpan.FromHours(1)))
            {
                _logger.LogWarning("SendEmailVerification: Rate limit exceeded for {Email}", request.Email);
                return StatusCode(429, "Too many requests. Please try again later.");
            }

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("SendEmailVerification: User with email {Email} not found", request.Email);
                return NotFound("User not found");
            }

            user.EmailVerificationCode = new Random().Next(100000, 999999).ToString();
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
            user.VerificationAttempts = 0;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("SendEmailVerification: Verification code {Code} sent to {Email}",
                user.EmailVerificationCode, user.Email);

            // TODO: Implement actual email sending
            // _emailService.SendVerificationEmail(user.Email, user.EmailVerificationCode);

            return Ok(new
            {
                Message = "Verification email sent",
                Method = "Email",
                ExpiryMinutes = 15
            });
        }

        [HttpPost("send-phone-verification")]
        public async Task<IActionResult> SendPhoneVerification([FromBody] PhoneVerificationRequestDto request)
        {
            _logger.LogInformation("SendPhoneVerification: Sending verification SMS to {PhoneNumber}", request.PhoneNumber);

            if (_rateLimitService.IsRateLimited($"phone_{request.PhoneNumber}", 5, TimeSpan.FromHours(1)))
            {
                _logger.LogWarning("SendPhoneVerification: Rate limit exceeded for {PhoneNumber}", request.PhoneNumber);
                return StatusCode(429, "Too many requests. Please try again later.");
            }

            var user = await _userRepository.GetByPhoneAsync(request.PhoneNumber);
            if (user == null)
            {
                _logger.LogWarning("SendPhoneVerification: User with phone {PhoneNumber} not found", request.PhoneNumber);
                return NotFound("User not found");
            }

            user.PhoneVerificationCode = new Random().Next(100000, 999999).ToString();
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
            user.VerificationAttempts = 0;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("SendPhoneVerification: Verification code {Code} sent to {PhoneNumber}",
                user.PhoneVerificationCode, user.PhoneNumber);

            // TODO: Implement actual SMS sending
            // _smsService.SendVerificationSms(user.PhoneNumber, user.PhoneVerificationCode);

            return Ok(new
            {
                Message = "Verification SMS sent",
                Method = "SMS",
                ExpiryMinutes = 15
            });
        }
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDto verificationDto)
        {
            _logger.LogInformation("VerifyEmail: Verifying email {Email}", verificationDto.Email);

            var user = await _userRepository.GetByEmailAsync(verificationDto.Email);
            if (user == null)
            {
                _logger.LogWarning("VerifyEmail: User with email {Email} not found", verificationDto.Email);
                return NotFound("User not found");
            }

            if (user.IsEmailVerified)
            {
                _logger.LogInformation("VerifyEmail: Email {Email} is already verified", verificationDto.Email);
                return BadRequest("Email is already verified");
            }

            if (string.IsNullOrEmpty(user.EmailVerificationCode))
            {
                _logger.LogWarning("VerifyEmail: No verification code found for {Email}", verificationDto.Email);
                return BadRequest("No verification code requested");
            }

            if (user.VerificationCodeExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("VerifyEmail: Expired verification code for {Email}", verificationDto.Email);
                return BadRequest("Verification code has expired");
            }

            if (user.EmailVerificationCode != verificationDto.Code)
            {
                user.VerificationAttempts++;
                await _userRepository.UpdateAsync(user);

                _logger.LogWarning("VerifyEmail: Invalid verification code for {Email}. Attempt {Attempt}",
                    verificationDto.Email, user.VerificationAttempts);
                return BadRequest("Invalid verification code");
            }

            user.IsEmailVerified = true;
            user.EmailVerificationCode = null;
            user.VerificationCodeExpiry = null;
            user.VerificationAttempts = 0;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("VerifyEmail: Email {Email} successfully verified", verificationDto.Email);

            return Ok(new
            {
                Message = "Email verified successfully",
                UserId = user.Id
            });
        }
        [HttpPost("verify-phone")]
        public async Task<IActionResult> VerifyPhone([FromBody] PhoneVerificationDto verificationDto)
        {
            _logger.LogInformation("VerifyPhone: Verifying phone {PhoneNumber}", verificationDto.PhoneNumber);

            var user = await _userRepository.GetByPhoneAsync(verificationDto.PhoneNumber);
            if (user == null)
            {
                _logger.LogWarning("VerifyPhone: User with phone {PhoneNumber} not found", verificationDto.PhoneNumber);
                return NotFound("User not found");
            }

            if (user.IsPhoneVerified)
            {
                _logger.LogInformation("VerifyPhone: Phone {PhoneNumber} is already verified", verificationDto.PhoneNumber);
                return BadRequest("Phone is already verified");
            }

            if (string.IsNullOrEmpty(user.PhoneVerificationCode))
            {
                _logger.LogWarning("VerifyPhone: No verification code found for {PhoneNumber}", verificationDto.PhoneNumber);
                return BadRequest("No verification code requested");
            }

            if (user.VerificationCodeExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("VerifyPhone: Expired verification code for {PhoneNumber}", verificationDto.PhoneNumber);
                return BadRequest("Verification code has expired");
            }

            if (user.PhoneVerificationCode != verificationDto.Code)
            {
                user.VerificationAttempts++;
                await _userRepository.UpdateAsync(user);

                _logger.LogWarning("VerifyPhone: Invalid verification code for {PhoneNumber}. Attempt {Attempt}",
                    verificationDto.PhoneNumber, user.VerificationAttempts);
                return BadRequest("Invalid verification code");
            }

            user.IsPhoneVerified = true;
            user.PhoneVerificationCode = null;
            user.VerificationCodeExpiry = null;
            user.VerificationAttempts = 0;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("VerifyPhone: Phone {PhoneNumber} successfully verified", verificationDto.PhoneNumber);

            return Ok(new
            {
                Message = "Phone verified successfully",
                UserId = user.Id
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto, [FromServices] IHttpContextAccessor httpContextAccessor)
        {
            _logger.LogInformation("Login: Attempting login for {Email}", loginDto.Email);

            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null || user.Password != PasswordService.HashPassword(loginDto.Password))
            {
                _logger.LogWarning("Login: Invalid credentials for {Email}", loginDto.Email);
                return Unauthorized("Invalid credentials");
            }

            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login: Email not verified for {Email}", loginDto.Email);
                return Unauthorized("Email not verified");
            }

            // Generate new device token and update last login info
            user.DeviceToken = GenerateDeviceToken();
            user.LastLoginIp = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            user.LastLoginDevice = httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Generate JWT token
            var token = _authService.GenerateToken(user);

            _logger.LogInformation("Login: Successful login for {Email}", loginDto.Email);

            return Ok(new
            {
                Token = token,
                ExpiresIn = Convert.ToInt32(_configuration["Jwt:ExpiryInMinutes"]) * 60,
                TokenType = "Bearer"
            });
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Logout: Invalid user claim in token");
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Logout: User {UserId} not found", userId);
                return NotFound("User not found");
            }

            // Invalidate device token
            user.DeviceToken = null;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Logout: User {UserId} logged out", userId);

            return Ok(new { Message = "Successfully logged out" });
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
                CreatedAt = user.CreatedAt,
                DeviceToken = user.DeviceToken
            };
        }
    }
}
