public class BankAccount
{
    // TODO: هذه الخصائص موجودة لكن تصميمها خاطئ. أصلحها حسب المطلوب:
    //   - AccountsCreated: عدّاد ثابت لا يُعدَّل من الخارج
    //   - Owner: للقراءة فقط
    //   - Balance: قراءة عامة، كتابة خاصة
    //   - Transactions: IReadOnlyList<decimal>
    public static int AccountsCreated { get; set; }
    public string Owner { get; set; } = "";
    public decimal Balance { get; set; }
    public IReadOnlyList<decimal> Transactions => throw new NotImplementedException();

    public BankAccount(string owner, decimal initialBalance = 0)
    {
        throw new NotImplementedException();
    }

    public void Deposit(decimal amount)
    {
        throw new NotImplementedException();
    }

    public void Withdraw(decimal amount)
    {
        throw new NotImplementedException();
    }

    public void TransferTo(BankAccount target, decimal amount)
    {
        throw new NotImplementedException();
    }
}
