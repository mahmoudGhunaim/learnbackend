public record Money(decimal Amount, string Currency)
{
    // A property initializer can reference the primary-constructor parameter: validate on creation.
    public string Currency { get; init; } = IsValidCurrency(Currency)
        ? Currency
        : throw new ArgumentException("Currency must be a 3-letter uppercase code like USD.", nameof(Currency));

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} and {b.Currency}.");
        return a with { Amount = a.Amount + b.Amount };
    }

    // Records are immutable: "with" creates a modified COPY.
    public Money Multiply(decimal factor) => this with { Amount = Math.Round(Amount * factor, 2) };

    public override string ToString() => $"{Amount:0.00} {Currency}";

    private static bool IsValidCurrency(string? code) =>
        code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);
}

public record LineItem(string Product, Money UnitPrice, int Quantity)
{
    public Money Total => UnitPrice.Multiply(Quantity);
}

public static class Checkout
{
    public static Money Sum(IEnumerable<LineItem> items, string currency) =>
        items.Aggregate(new Money(0, currency), (sum, item) => sum + item.Total);

    public static Money Shipping(Money subtotal) => new(subtotal.Amount switch
    {
        < 50 => 5m,
        < 100 => 2.5m,
        _ => 0m,
    }, subtotal.Currency);
}
