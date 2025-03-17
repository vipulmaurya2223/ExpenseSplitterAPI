using ExpenseSplitterAPI.Data;
using ExpenseSplitterAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using BatchWebApi.ViewModels;
using BCrypt.Net;
using ExpenseSplitterAPI.ViewModels;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // ✅ Secure User Registration with BCrypt
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel user)
    {
        if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            return BadRequest(new { message = "All fields are required!" });

        // ✅ Check if user already exists
        var existingUser = await _context.Users.AnyAsync(x => x.Email == user.Email);
        if (existingUser)
            return Conflict(new { message = "User already exists with this email." });

        // ✅ Hash the password before storing
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);

        // ✅ Create new user
        var newUser = new User
        {
            Name = user.Name,
            Email = user.Email,
            Password = hashedPassword, // ✅ Securely store hashed password
            RoleId = 2 // Default role (User)
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCurrentUser), new { id = newUser.UserId }, new { message = "User registered successfully!" });
    }

    // ✅ Secure Login with BCrypt
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel user)
    {
        if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            return BadRequest(new { message = "Email and Password are required!" });

        try
        {
            var obj = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(x => x.Email == user.Email);

            if (obj == null || !BCrypt.Net.BCrypt.Verify(user.Password, obj.Password)) // ✅ Secure Check
                return Unauthorized(new { message = "Invalid email or password" });

            var tokenString = GenerateJSONWebToken(obj);
            Response.Cookies.Append("AuthToken", tokenString, new CookieOptions
            {
                HttpOnly = true, // ✅ Client-side JavaScript can't access this (more secure)
                Secure = true,    // ✅ HTTPS required in production
                SameSite = SameSiteMode.Strict, // ✅ Protects against CSRF attacks
                Expires = DateTime.UtcNow.AddHours(1) // ✅ Expiration time
            });

            return Ok(new { message = "Login Successful" });

            //return Ok(new { token = tokenString });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while logging in.", error = ex.Message });
        }
    }

    // ✅ Fetch Authenticated User
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sid);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest(new { message = "Invalid user ID format" });

            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                id = user.UserId,
                name = user.Name,
                email = user.Email,
                role = user.Role?.RoleName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching user details.", error = ex.Message });
        }
    }

    // ✅ Generate JWT Token
    private string GenerateJSONWebToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
            new Claim("Date", DateTime.UtcNow.ToString("o"))
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
