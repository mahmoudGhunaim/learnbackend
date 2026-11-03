public record Order(int Id, string Customer, string Category, decimal Amount, DateOnly Date);

public static class OrderQueries
{
    public static decimal TotalRevenue(IEnumerable<Order> orders) => orders.Sum(o => o.Amount);

    public static IReadOnlyList<string> BigSpenders(IEnumerable<Order> orders, decimal threshold) =>
        orders
            .GroupBy(o => o.Customer)
            .Select(g => new { Name = g.Key, Total = g.Sum(o => o.Amount) })
            .Where(x => x.Total > threshold)
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .ToList();

    public static Dictionary<string, decimal> RevenueByCategory(IEnumerable<Order> orders) =>
        orders
            .GroupBy(o => o.Category)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Amount));

    public static IReadOnlyList<int> TopOrderIds(IEnumerable<Order> orders, int n) =>
        orders
            .OrderByDescending(o => o.Amount)
            .ThenBy(o => o.Id)
            .Take(n)
            .Select(o => o.Id)
            .ToList();

    public static decimal AverageForMonth(IEnumerable<Order> orders, int year, int month)
    {
        var inMonth = orders.Where(o => o.Date.Year == year && o.Date.Month == month).ToList();
        return inMonth.Count == 0 ? 0 : inMonth.Average(o => o.Amount);
    }
}
