using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using BankingApp.Data;
using BankingApp.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Data.SqlClient;

namespace BankingApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly BankingContext _context;
        private readonly IConfiguration _configuration;

        public CustomersController(BankingContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (await _context.Customers.AnyAsync(c => c.Email == registerDto.Email))
            {
                return BadRequest("Email already exists.");
            }

            var customer = new Customer
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                AccountNumber = registerDto.AccountNumber
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var account = new Account
            {
                CustomerId = customer.CustomerId,
                AccountNumber = registerDto.AccountNumber,
                Password = HashPassword(registerDto.Password),
                Balance = 0 // Initial balance can be set to 0 or any default value
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok("Registration successful.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == loginDto.AccountNumber);

            if (account == null || !VerifyPassword(loginDto.Password, account.Password))
            {
                return Unauthorized("Invalid account number or password.");
            }

            var token = GenerateJwtToken(account);
            return Ok(new { token });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }

        private string GenerateJwtToken(Account account)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, account.AccountNumber),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
