// A realistic mistake: integer division (percent / 100 is 0 for any percent below 100).
public static class PriceCalculator
{
    public static decimal Subtotal(decimal unitPrice, int quantity)
    {
        if (unitPrice < 0 || quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return unitPrice * quantity;
    }

    public static decimal ApplyDiscount(decimal amount, int percent)
    {
        if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        return Math.Round(amount * (1 - percent / 100), 2);
    }

    public static decimal Total(decimal unitPrice, int quantity, int discountPercent, decimal taxRate) =>
        Math.Round(ApplyDiscount(Subtotal(unitPrice, quantity), discountPercent) * (1 + taxRate), 2);

    public static string Format(decimal amount) => amount.ToString("N2") + " USD";
}
