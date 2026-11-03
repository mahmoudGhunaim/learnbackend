public record Order(int Id, string Customer, string Category, decimal Amount, DateOnly Date);

public static class OrderQueries
{
    public static decimal TotalRevenue(IEnumerable<Order> orders)
        => throw new NotImplementedException();

    public static IReadOnlyList<string> BigSpenders(IEnumerable<Order> orders, decimal threshold)
        => throw new NotImplementedException();

    public static Dictionary<string, decimal> RevenueByCategory(IEnumerable<Order> orders)
        => throw new NotImplementedException();

    public static IReadOnlyList<int> TopOrderIds(IEnumerable<Order> orders, int n)
        => throw new NotImplementedException();

    public static decimal AverageForMonth(IEnumerable<Order> orders, int year, int month)
        => throw new NotImplementedException();
}
