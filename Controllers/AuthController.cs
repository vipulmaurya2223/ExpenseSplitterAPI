using BatchWebApi.ViewModels;
using ExpenseSplitterAPI.Data;
using ExpenseSplitterAPI.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    ApplicationDbContext _context;
    IConfiguration _configuration;
    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    [HttpPost]
    public IActionResult Login(LoginViewModel user)
    {
        IActionResult response = Unauthorized();
        var obj = _context.Users.FirstOrDefault(x => x.Email == user.Email && x.PasswordHash == user.Password);
        if (obj != null)
        {
            var tokenString = GenerateJSONWebToken(obj);
            response = Ok(new { token = tokenString });
        }
        return response;

    }


    private string GetRoleName(int roleId)
    {
        string roleName = (from x in _context.Roles
                           where x.RoleId == roleId
                           select x.RoleName).FirstOrDefault();
        return roleName;
    }

    private string GenerateJSONWebToken(User user)
    {
        string role = GetRoleName(user.RoleId);

        //List<Claim> claims = new List<Claim> {
        //     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //     new Claim(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
        //     new Claim(JwtRegisteredClaimNames.Name, user.FirstName + " " + user.LastName),
        //     new Claim("Role", role.ToString()),
        //     new Claim(type:"Date", DateTime.Now.ToString())
        //};

        var claims = new Claim[]
       {
                new Claim(JwtRegisteredClaimNames.Jti, new Guid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sid,user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Name ),
                new Claim(ClaimTypes.Email , user.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim(type:"DateOnly", DateTime.Now.ToString())
       };


        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
          _configuration["Jwt:Audience"],
          claims,
          expires: DateTime.Now.AddMinutes(120),
          signingCredentials: credentials);




        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}