// Realistic mistakes: BigSpenders filters single orders instead of each customer's total,
// and AverageForMonth crashes on an empty month.
public record Order(int Id, string Customer, string Category, decimal Amount, DateOnly Date);

public static class OrderQueries
{
    public static decimal TotalRevenue(IEnumerable<Order> orders) => orders.Sum(o => o.Amount);

    public static IReadOnlyList<string> BigSpenders(IEnumerable<Order> orders, decimal threshold) =>
        orders.Where(o => o.Amount > threshold)
              .OrderByDescending(o => o.Amount)
              .Select(o => o.Customer)
              .Distinct()
              .ToList();

    public static Dictionary<string, decimal> RevenueByCategory(IEnumerable<Order> orders) =>
        orders.GroupBy(o => o.Category).ToDictionary(g => g.Key, g => g.Sum(o => o.Amount));

    public static IReadOnlyList<int> TopOrderIds(IEnumerable<Order> orders, int n) =>
        orders.OrderByDescending(o => o.Amount).Take(n).Select(o => o.Id).ToList();

    public static decimal AverageForMonth(IEnumerable<Order> orders, int year, int month) =>
        orders.Where(o => o.Date.Year == year && o.Date.Month == month).Average(o => o.Amount);
}
