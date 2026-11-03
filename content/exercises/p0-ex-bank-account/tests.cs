using LearnHarness;

public class Tests
{
    [Test("الحساب الجديد يحفظ المالك والرصيد", "New account stores owner and balance")]
    public void Constructor_sets_state()
    {
        var a = new BankAccount("Sara", 100m);
        Assert.Equal("Sara", a.Owner);
        Assert.Equal(100m, a.Balance);
        Assert.Equal(0, a.Transactions.Count);
    }

    [Test("اسم المالك الفارغ يرمي ArgumentException", "Blank owner throws ArgumentException")]
    public void Constructor_rejects_blank_owner() => Assert.Throws<ArgumentException>(() => new BankAccount("   "));

    [Test("الرصيد الابتدائي السالب يرمي ArgumentOutOfRangeException", "Negative initial balance throws")]
    public void Constructor_rejects_negative_balance() => Assert.Throws<ArgumentOutOfRangeException>(() => new BankAccount("Omar", -5m));

    [Test("الإيداع والسحب يحدّثان الرصيد والعمليات", "Deposit and withdraw update balance and transactions")]
    public void Deposit_and_withdraw()
    {
        var a = new BankAccount("Lina");
        a.Deposit(50m);
        a.Withdraw(20m);
        Assert.Equal(30m, a.Balance);
        Assert.SequenceEqual(new[] { 50m, -20m }, a.Transactions);
    }

    [Test("المبالغ الصفرية أو السالبة مرفوضة", "Zero or negative amounts are rejected")]
    public void Amounts_must_be_positive()
    {
        var a = new BankAccount("Lina", 10m);
        Assert.Throws<ArgumentOutOfRangeException>(() => a.Deposit(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.Withdraw(-1m));
    }

    [Test("لا يمكن سحب أكثر من الرصيد", "Cannot withdraw more than the balance")]
    public void No_overdraft()
    {
        var a = new BankAccount("Ali", 10m);
        Assert.Throws<InvalidOperationException>(() => a.Withdraw(10.01m));
        Assert.Equal(10m, a.Balance);
    }

    [Test("التحويل الفاشل لا يحرك أي مال", "A failed transfer moves no money")]
    public void Transfer_is_safe()
    {
        var from = new BankAccount("A", 30m);
        var to = new BankAccount("B");
        from.TransferTo(to, 25m);
        Assert.Equal(5m, from.Balance);
        Assert.Equal(25m, to.Balance);

        Assert.Throws<InvalidOperationException>(() => from.TransferTo(to, 100m));
        Assert.Equal(5m, from.Balance);
        Assert.Equal(25m, to.Balance);
    }

    [Test("Balance ليس له setter عام", "Balance has no public setter")]
    public void Balance_is_encapsulated()
    {
        var setter = typeof(BankAccount).GetProperty("Balance")!.SetMethod;
        Assert.True(setter is null || !setter.IsPublic, "Balance must not have a public setter");
    }

    [Test("Transactions معرّفة كـ IReadOnlyList<decimal>", "Transactions is declared as IReadOnlyList<decimal>")]
    public void Transactions_are_read_only()
    {
        var type = typeof(BankAccount).GetProperty("Transactions")!.PropertyType;
        Assert.Equal(typeof(IReadOnlyList<decimal>), type, "Expose Transactions as IReadOnlyList<decimal> so callers can't Add/Remove");
    }

    [Test("العدّاد الثابت يزيد مع كل حساب", "The static counter grows with each account")]
    public void Static_counter()
    {
        var before = BankAccount.AccountsCreated;
        _ = new BankAccount("X");
        _ = new BankAccount("Y");
        Assert.Equal(before + 2, BankAccount.AccountsCreated);
    }
}
