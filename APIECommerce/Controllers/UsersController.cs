using APIECommerce.Context;
using APIECommerce.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APIECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }


        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var checkUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (checkUser != null)
            {
                return BadRequest("A user with this email already exists.");
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var currentUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email && u.Password == user.Password);

            if (currentUser == null)
            {
                return NotFound("User not found.");
            }

            var key = _configuration["JWT:Key"] ?? throw new ArgumentNullException("JWT:Key", "JWT:Key cannot be null.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email!)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(10),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new ObjectResult(new
            {
                access_token = jwt,
                token_type = "bearer",
                user_id = currentUser.Id,
                user_name = currentUser.Name
            });
        }
    }
}
