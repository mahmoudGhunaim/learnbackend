public static class PriceCalculator
{
    public static decimal Subtotal(decimal unitPrice, int quantity)
    {
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Price cannot be negative.");
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");
        return unitPrice * quantity;
    }

    public static decimal ApplyDiscount(decimal amount, int percent)
    {
        if (percent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        // Multiply first, then divide by a decimal literal: no integer division.
        var discounted = amount * (100 - percent) / 100m;
        return Math.Round(discounted, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal Total(decimal unitPrice, int quantity, int discountPercent, decimal taxRate)
    {
        var afterDiscount = ApplyDiscount(Subtotal(unitPrice, quantity), discountPercent);
        return Math.Round(afterDiscount * (1 + taxRate), 2, MidpointRounding.AwayFromZero);
    }

    public static string Format(decimal amount) => $"{amount:N2} USD";
}
