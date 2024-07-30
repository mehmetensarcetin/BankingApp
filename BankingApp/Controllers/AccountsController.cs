using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankingApp.Data;
using BankingApp.Models;
using System.Data.SqlClient;

namespace BankingApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly BankingContext _context;
        private readonly IConfiguration _configuration;

        public AccountsController(BankingContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("balance/{accountNumber}")]
        public async Task<IActionResult> GetBalance(string accountNumber)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null)
            {
                return NotFound();
            }

            return Ok(new { balance = account.Balance });
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            var fromAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.FromAccountNumber);
            var toAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber);

            if (fromAccount == null || toAccount == null)
            {
                return NotFound("One or both accounts not found.");
            }

            if (fromAccount.Balance < request.Amount)
            {
                return BadRequest("Insufficient balance.");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    fromAccount.Balance -= request.Amount;
                    toAccount.Balance += request.Amount;

                    await _context.Database.ExecuteSqlRawAsync("EXEC CreateTransaction @AccountId, @Amount, @TransactionDate, @TransactionType",
                        new SqlParameter("@AccountId", fromAccount.AccountId),
                        new SqlParameter("@Amount", -request.Amount),
                        new SqlParameter("@TransactionDate", DateTime.Now),
                        new SqlParameter("@TransactionType", "Transfer"));

                    await _context.Database.ExecuteSqlRawAsync("EXEC CreateTransaction @AccountId, @Amount, @TransactionDate, @TransactionType",
                        new SqlParameter("@AccountId", toAccount.AccountId),
                        new SqlParameter("@Amount", request.Amount),
                        new SqlParameter("@TransactionDate", DateTime.Now),
                        new SqlParameter("@TransactionType", "Transfer"));

                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return Ok("Transfer successful.");
        }
    }

    public class TransferRequest
    {
        public string FromAccountNumber { get; set; }
        public string ToAccountNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
