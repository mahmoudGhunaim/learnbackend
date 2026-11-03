using LearnHarness;

public class Tests
{
    [Test("مبلغان بنفس القيمة متساويان (Value equality)", "Two equal amounts are equal (value equality)")]
    public void Value_equality()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10.00m, "USD");
        Assert.True(a == b, "== should compare values for a record");
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.False(new Money(10m, "USD") == new Money(10m, "EUR"));
    }

    [Test("العملة غير الصالحة ترمي ArgumentException", "Invalid currency throws ArgumentException")]
    public void Validates_currency()
    {
        Assert.Throws<ArgumentException>(() => new Money(1m, "usd"));
        Assert.Throws<ArgumentException>(() => new Money(1m, "US"));
        Assert.Throws<ArgumentException>(() => new Money(1m, "US1"));
    }

    [Test("الجمع بنفس العملة", "Adding the same currency")]
    public void Add() => Assert.Equal(new Money(15.5m, "JOD"), new Money(10m, "JOD") + new Money(5.5m, "JOD"));

    [Test("الجمع بعملتين مختلفتين يرمي InvalidOperationException", "Adding different currencies throws")]
    public void Add_mismatch() =>
        Assert.Throws<InvalidOperationException>(() => _ = new Money(1m, "USD") + new Money(1m, "EUR"));

    [Test("Multiply يرجع نسخة جديدة ولا يغيّر الأصل", "Multiply returns a new copy and leaves the original alone")]
    public void Multiply_is_immutable()
    {
        var price = new Money(19.99m, "USD");
        var triple = price.Multiply(3);
        Assert.Equal(new Money(59.97m, "USD"), triple);
        Assert.Equal(19.99m, price.Amount, "The original must not change");
    }

    [Test("ToString بخانتين عشريتين", "ToString with 2 decimals")]
    public void Formats() => Assert.Equal("12.50 USD", new Money(12.5m, "USD").ToString());

    [Test("مجموع سلة الشراء", "Cart total")]
    public void Sum_items()
    {
        var items = new[]
        {
            new LineItem("Pen", new Money(1.25m, "USD"), 4),
            new LineItem("Book", new Money(20m, "USD"), 2),
        };
        Assert.Equal(new Money(45m, "USD"), Checkout.Sum(items, "USD"));
        Assert.Equal(new Money(0m, "EUR"), Checkout.Sum(Array.Empty<LineItem>(), "EUR"));
    }

    [Test("الشحن حسب المبلغ (Switch expression)", "Shipping tiers (switch expression)")]
    public void Shipping_tiers()
    {
        Assert.Equal(new Money(5m, "USD"), Checkout.Shipping(new Money(49.99m, "USD")));
        Assert.Equal(new Money(2.5m, "USD"), Checkout.Shipping(new Money(50m, "USD")));
        Assert.Equal(new Money(0m, "SAR"), Checkout.Shipping(new Money(100m, "SAR")));
    }
}
