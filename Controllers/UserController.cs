using ExpenseSplitterAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseSplitterAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly JwtAuthenticationService _jwtService;

        public UserController(JwtAuthenticationService jwtService) // ✅ Inject service
        {
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request.Email == "test@example.com" && request.Password == "password123")
            {
                var token = _jwtService.GenerateToken("1", request.Email);
                return Ok(new { Token = token });
            }

            return Unauthorized("Invalid credentials");
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
