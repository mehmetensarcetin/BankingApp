namespace BankingApp.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public int CustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Password { get; set; }
        public decimal Balance { get; set; }
    }
}
