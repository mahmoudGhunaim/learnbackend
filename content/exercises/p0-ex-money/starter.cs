// TODO: حوّله إلى record مع التحقق من العملة
public class Money
{
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; set; }
    public string Currency { get; set; }

    public static Money operator +(Money a, Money b) => throw new NotImplementedException();

    public Money Multiply(decimal factor) => throw new NotImplementedException();
}

public record LineItem(string Product, Money UnitPrice, int Quantity)
{
    public Money Total => throw new NotImplementedException();
}

public static class Checkout
{
    public static Money Sum(IEnumerable<LineItem> items, string currency) => throw new NotImplementedException();

    public static Money Shipping(Money subtotal) => throw new NotImplementedException();
}
