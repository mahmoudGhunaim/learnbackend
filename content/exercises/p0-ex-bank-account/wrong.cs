// Realistic mistakes: public setter on Balance, no overdraft check,
// the internal List is exposed, and TransferTo deposits before withdrawing.
public class BankAccount
{
    public static int AccountsCreated { get; set; }
    public string Owner { get; }
    public decimal Balance { get; set; }
    public List<decimal> Transactions { get; } = new();

    public BankAccount(string owner, decimal initialBalance = 0)
    {
        Owner = owner;
        Balance = initialBalance;
        AccountsCreated++;
    }

    public void Deposit(decimal amount) { Balance += amount; Transactions.Add(amount); }
    public void Withdraw(decimal amount) { Balance -= amount; Transactions.Add(-amount); }
    public void TransferTo(BankAccount target, decimal amount) { target.Deposit(amount); Withdraw(amount); }
}
