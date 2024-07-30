using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankingApp.Data;
using BankingApp.Models;

namespace BankingApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly BankingContext _context;

        public TransactionsController(BankingContext context)
        {
            _context = context;
        }

        [HttpGet("{accountNumber}")]
        public async Task<IActionResult> GetTransactions(string accountNumber)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null)
            {
                return NotFound();
            }

            var transactions = await _context.Transactions
                .Where(t => t.AccountId == account.AccountId)
                .ToListAsync();

            return Ok(transactions);
        }
    }
}
