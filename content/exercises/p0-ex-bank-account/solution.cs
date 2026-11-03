public class BankAccount
{
    private readonly List<decimal> _transactions = new();

    public static int AccountsCreated { get; private set; }

    public string Owner { get; }
    public decimal Balance { get; private set; }
    public IReadOnlyList<decimal> Transactions => _transactions.AsReadOnly();

    public BankAccount(string owner, decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner is required.", nameof(owner));
        if (initialBalance < 0)
            throw new ArgumentOutOfRangeException(nameof(initialBalance), "Initial balance cannot be negative.");

        Owner = owner;
        Balance = initialBalance;
        AccountsCreated++;
    }

    public void Deposit(decimal amount)
    {
        EnsurePositive(amount);
        Balance += amount;
        _transactions.Add(amount);
    }

    public void Withdraw(decimal amount)
    {
        EnsurePositive(amount);
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds.");
        Balance -= amount;
        _transactions.Add(-amount);
    }

    public void TransferTo(BankAccount target, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        Withdraw(amount);       // throws before any money moves if funds are insufficient
        target.Deposit(amount);
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
    }
}
