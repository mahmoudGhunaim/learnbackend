using LearnHarness;

public class Tests
{
    [Test("المجموع = السعر × الكمية", "Subtotal = price × quantity")]
    public void Subtotal_multiplies() => Assert.Equal(59.97m, PriceCalculator.Subtotal(19.99m, 3));

    [Test("الكمية السالبة ترمي ArgumentOutOfRangeException", "Negative quantity throws ArgumentOutOfRangeException")]
    public void Subtotal_rejects_negative_quantity() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceCalculator.Subtotal(10m, -1));

    [Test("السعر السالب يرمي ArgumentOutOfRangeException", "Negative price throws ArgumentOutOfRangeException")]
    public void Subtotal_rejects_negative_price() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceCalculator.Subtotal(-0.01m, 1));

    [Test("خصم 15% على 100 يعطي 85", "15% off 100 is 85")]
    public void Discount_basic() => Assert.Equal(85.00m, PriceCalculator.ApplyDiscount(100m, 15));

    [Test("الخصم يُقرّب لخانتين: 33% على 10.01", "Discount rounds to 2 decimals: 33% off 10.01")]
    public void Discount_rounds() => Assert.Equal(6.71m, PriceCalculator.ApplyDiscount(10.01m, 33));

    [Test("نسبة خصم أكبر من 100 ترمي استثناء", "Discount above 100% throws")]
    public void Discount_rejects_out_of_range() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PriceCalculator.ApplyDiscount(50m, 101));

    [Test("الإجمالي مع خصم 10% وضريبة 16%", "Total with 10% discount and 16% tax")]
    public void Total_combines() => Assert.Equal(62.61m, PriceCalculator.Total(19.99m, 3, 10, 0.16m));

    [Test("خصم 100% يجعل الإجمالي صفراً", "100% discount makes the total zero")]
    public void Total_full_discount() => Assert.Equal(0m, PriceCalculator.Total(5m, 2, 100, 0.16m));

    [Test("التنسيق: فاصل الآلاف وخانتان عشريتان", "Format uses thousands separator and 2 decimals")]
    public void Format_formats()
    {
        Assert.Equal("1,234.50 USD", PriceCalculator.Format(1234.5m));
        Assert.Equal("0.00 USD", PriceCalculator.Format(0m));
    }
}
