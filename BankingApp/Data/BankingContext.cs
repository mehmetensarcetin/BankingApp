using Microsoft.EntityFrameworkCore;
using BankingApp.Models;

namespace BankingApp.Data
{
    public class BankingContext : DbContext
    {
        public BankingContext(DbContextOptions<BankingContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().ToTable("Accounts");
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Transaction>().ToTable("Transactions");
        }
    }
}
