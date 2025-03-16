using BatchWebApi.ViewModels;
using ExpenseSplitterAPI.Data;
using ExpenseSplitterAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

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

    [HttpPost("login")]
    public IActionResult Login(LoginViewModel user)
    {
        if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            return BadRequest(new { message = "Email and Password are required!" });

        // Directly compare plain text password
        var obj = _context.Users.FirstOrDefault(x => x.Email == user.Email && x.Password == user.Password);

        if (obj == null)
            return Unauthorized(new { message = "Invalid email or password" });

        var tokenString = GenerateJSONWebToken(obj);
        return Ok(new { token = tokenString });
    }

    private string GetRoleName(int roleId)
    {
        return _context.Roles
            .Where(x => x.RoleId == roleId)
            .Select(x => x.RoleName)
            .FirstOrDefault() ?? "User";
    }

    private string GenerateJSONWebToken(User user)
    {
        string role = GetRoleName(user.RoleId);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sid, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("Date", DateTime.UtcNow.ToString("o")) // ISO 8601 format
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
