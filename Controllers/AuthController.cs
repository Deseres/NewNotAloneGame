using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NotAlone.Models;
using NotAlone.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BC = BCrypt.Net.BCrypt;

namespace NotAlone.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <summary>Register. Username is optional — auto-generated from email if omitted.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (request.Password.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters." });

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"[!@#$%^&*()_+\-=\[\]{};':""\|,.<>\/?]"))
            return BadRequest(new { message = "Password must contain at least one special character." });

        if (_dbContext.Users.Any(u => u.Email == request.Email))
            return BadRequest(new { message = "Email already registered." });

        var username = string.IsNullOrWhiteSpace(request.Username)
            ? request.Email.Split('@')[0] + Guid.NewGuid().ToString()[..4]
            : request.Username.Trim();

        if (_dbContext.Users.Any(u => u.Username == username))
            return BadRequest(new { message = "Username already taken." });

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Username     = username,
            Email        = request.Email,
            PasswordHash = BC.HashPassword(request.Password),
            CreatedAt    = DateTime.UtcNow
        };

        SetRefreshToken(user);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            UserId       = user.Id.ToString(),
            Email        = user.Email,
            Username     = user.Username,
            Token        = GenerateJwtToken(user),
            RefreshToken = user.RefreshToken!
        });
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email);
        if (user is null || !BC.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        SetRefreshToken(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            UserId       = user.Id.ToString(),
            Email        = user.Email,
            Username     = user.Username,
            Token        = GenerateJwtToken(user),
            RefreshToken = user.RefreshToken!
        });
    }

    /// <summary>Exchange a valid refresh token for a new access token + rotated refresh token.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        SetRefreshToken(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            UserId       = user.Id.ToString(),
            Email        = user.Email,
            Username     = user.Username,
            Token        = GenerateJwtToken(user),
            RefreshToken = user.RefreshToken!
        });
    }

    /// <summary>Returns the current authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return NotFound();

        return Ok(new MeResponse
        {
            UserId    = user.Id.ToString(),
            Email     = user.Email,
            Username  = user.Username,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>Update the authenticated user's username.</summary>
    [HttpPut("username")]
    [Authorize]
    public async Task<IActionResult> UpdateUsername(UpdateUsernameRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 3)
            return BadRequest(new { message = "Username must be at least 3 characters." });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null) return NotFound();

        var newUsername = request.Username.Trim();
        if (_dbContext.Users.Any(u => u.Username == newUsername && u.Id != userId))
            return BadRequest(new { message = "Username already taken." });

        user.Username = newUsername;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Username updated.", username = user.Username });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetRefreshToken(User user)
    {
        var days = int.Parse(_configuration["Jwt:RefreshTokenDays"] ?? "30");
        user.RefreshToken       = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(days);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key         = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["SecretKey"] ?? ""));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.Username)
        };

        var token = new JwtSecurityToken(
            issuer:             jwtSettings["Issuer"],
            audience:           jwtSettings["Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"] ?? "60")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class RegisterRequest
{
    public string  Email    { get; set; } = "";
    public string  Password { get; set; } = "";
    /// <summary>Optional. Auto-generated from email if omitted.</summary>
    public string? Username { get; set; }
}

public class LoginRequest
{
    public string Email    { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = "";
}

public class UpdateUsernameRequest
{
    public string Username { get; set; } = "";
}

public class AuthResponse
{
    public string UserId       { get; set; } = "";
    public string Email        { get; set; } = "";
    public string Username     { get; set; } = "";
    public string Token        { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

public class MeResponse
{
    public string   UserId    { get; set; } = "";
    public string   Email     { get; set; } = "";
    public string   Username  { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
