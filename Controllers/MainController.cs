using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppsMoodle.Models;

namespace WebAppsMoodle.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase

    { 

        private readonly ILogger<MainController> _logger;
        private readonly DataContext _context;

        public MainController(ILogger<MainController> logger, DataContext context)
        {
            _logger = logger;
            _context  = context;
        }


        // Mock database for storing users
        private static readonly List<Teacher> _teacher = new List<Teacher>();

        // Register endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TeacherRegisterModel model)
        {
            // Check if username already exists
            if (await _context.Teachers.AnyAsync(t => t.Username == model.Username))
            {
                return BadRequest("Username already exists");
            }

            // We should hash the password before storing it
            var newTeacher = new Teacher
            {
                TeacherId = Guid.NewGuid().ToString(),
                Username = model.Username,
                Password = model.Password,
                Title = ""
            };

            _context.Teachers.Add(newTeacher);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        // Login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TeacherLoginModel model)
        {
            // Check if user exists
            // Check if username already exists
            if (await _context.Teachers.AnyAsync(u => u.Username != model.Username || u.Password != model.Password))
            {
                return BadRequest("incorrect");
            }
            else return Ok("User login successful");

            /* // Create JWT token
             var token = GenerateJwtToken(user);

             return Ok(new { Token = token });*/

        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(Teacher user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("your-secret-key-with-at-least-128-bits"); // Ensure your key has at least 128 bits

            // We may also consider using stronger key generation methods
            // var key = new byte[32]; // 256 bits
            // using (var generator = RandomNumberGenerator.Create())
            // {
            //     generator.GetBytes(key);
            // }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, user.Username)

        }),
                Expires = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // Models for registration and login
/*    public class TeacherRegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class TeacherLoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }*/

    // Mock user model (in a real-world scenario, use a proper user model with data annotations)
/*    public class Teacher
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Title { get; set; }
    }*/
}



