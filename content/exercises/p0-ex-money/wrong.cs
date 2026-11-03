// Realistic mistake: Money stays a class, so two equal amounts are "different" objects
// (reference equality), it is mutable, and Multiply changes the original.
public class Money
{
    public Money(decimal amount, string currency)
    {
        if (currency.Length != 3) throw new ArgumentException("Bad currency");
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; set; }
    public string Currency { get; set; }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency) throw new InvalidOperationException();
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public Money Multiply(decimal factor)
    {
        Amount = Math.Round(Amount * factor, 2);
        return this;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

public record LineItem(string Product, Money UnitPrice, int Quantity)
{
    public Money Total => UnitPrice.Multiply(Quantity);
}

public static class Checkout
{
    public static Money Sum(IEnumerable<LineItem> items, string currency) =>
        items.Aggregate(new Money(0, currency), (sum, item) => sum + item.Total);

    public static Money Shipping(Money subtotal) =>
        new(subtotal.Amount < 50 ? 5m : subtotal.Amount < 100 ? 2.5m : 0m, subtotal.Currency);
}
