using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NotAlone.Models;
using NotAlone.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BC = BCrypt.Net.BCrypt;

namespace NotAlone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext dbContext,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">Email and password</param>
        /// <returns>JWT token if successful</returns>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required" });

            if (request.Password.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters" });

            // Check if email already exists
            var existingUser = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already registered" });

            // Generate unique username
            var username = request.Email.Split('@')[0] + Guid.NewGuid().ToString().Substring(0, 4);

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = request.Email,
                PasswordHash = BC.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                Username = user.Username,
                Token = token
            });
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="request">Email and password</param>
        /// <returns>JWT token if successful</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email and password are required" });

            // Find user by email
            var user = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            // Verify password
            if (!BC.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                Username = user.Username,
                Token = token
            });
        }

        /// <summary>
        /// Generate JWT token for user
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? "");
            var key = new SymmetricSecurityKey(secretKey);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"] ?? "60")),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>
    /// Request for user registration
    /// </summary>
    public class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// Request for user login
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// Response after successful authentication
    /// </summary>
    public class AuthResponse
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string Username { get; set; } = "";
        public string Token { get; set; } = "";
    }
}
