using LearnHarness;

public class Tests
{
    private static readonly Order[] Orders =
    [
        new(1, "Sara",  "Books",       40m,  new DateOnly(2026, 1, 5)),
        new(2, "Omar",  "Electronics", 300m, new DateOnly(2026, 1, 9)),
        new(3, "Sara",  "Electronics", 120m, new DateOnly(2026, 2, 1)),
        new(4, "Lina",  "Books",       60m,  new DateOnly(2026, 2, 14)),
        new(5, "Lina",  "Clothes",     90m,  new DateOnly(2026, 2, 20)),
        new(6, "Hadi",  "Books",       15m,  new DateOnly(2026, 3, 2)),
        new(7, "Omar",  "Clothes",     120m, new DateOnly(2026, 3, 3)),
    ];

    [Test("مجموع الإيرادات", "Total revenue")]
    public void Total() => Assert.Equal(745m, OrderQueries.TotalRevenue(Orders));

    [Test("مجموع قائمة فارغة = 0", "Total of an empty list is 0")]
    public void Total_empty() => Assert.Equal(0m, OrderQueries.TotalRevenue(Array.Empty<Order>()));

    [Test("كبار العملاء حسب المجموع وليس حسب طلب واحد", "Big spenders by TOTAL, not by single order")]
    public void BigSpenders_use_totals()
    {
        // Omar 420, Sara 160, Lina 150, Hadi 15 → above 140: Omar, Sara, Lina
        Assert.SequenceEqual(new[] { "Omar", "Sara", "Lina" }, OrderQueries.BigSpenders(Orders, 140m));
    }

    [Test("عند التساوي في المجموع: ترتيب أبجدي", "Ties are ordered by name")]
    public void BigSpenders_ties()
    {
        var orders = new[]
        {
            new Order(1, "Zaid", "X", 50m, new DateOnly(2026, 1, 1)),
            new Order(2, "Adam", "X", 50m, new DateOnly(2026, 1, 1)),
        };
        Assert.SequenceEqual(new[] { "Adam", "Zaid" }, OrderQueries.BigSpenders(orders, 10m));
    }

    [Test("الإيرادات حسب الفئة", "Revenue by category")]
    public void ByCategory()
    {
        var result = OrderQueries.RevenueByCategory(Orders);
        Assert.Equal(3, result.Count);
        Assert.Equal(115m, result["Books"]);
        Assert.Equal(420m, result["Electronics"]);
        Assert.Equal(210m, result["Clothes"]);
    }

    [Test("أكبر الطلبات مع كسر التعادل بالـ Id", "Top orders with Id tie-break")]
    public void TopOrders() => Assert.SequenceEqual(new[] { 2, 3, 7 }, OrderQueries.TopOrderIds(Orders, 3));

    [Test("متوسط شهر فبراير", "Average for February")]
    public void AverageMonth() => Assert.Equal(90m, OrderQueries.AverageForMonth(Orders, 2026, 2));

    [Test("متوسط شهر بلا طلبات = 0 (بدون استثناء)", "Average of an empty month is 0 (no exception)")]
    public void AverageEmptyMonth() => Assert.Equal(0m, OrderQueries.AverageForMonth(Orders, 2025, 12));
}
