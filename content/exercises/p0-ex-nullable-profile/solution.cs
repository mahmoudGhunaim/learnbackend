public class Address
{
    public string? City { get; set; }
}

public class User
{
    public required string UserName { get; init; }
    public string? DisplayName { get; set; }
    public Address? Address { get; set; }
    public int? Age { get; set; }
}

public static class ProfileFormatter
{
    // "??" is not enough: "   " is not null, but it is not a usable name either.
    public static string NameToShow(User user) =>
        string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName;

    public static string CityOf(User? user)
    {
        var city = user?.Address?.City;   // null as soon as any link in the chain is null
        return string.IsNullOrWhiteSpace(city) ? "Unknown" : city;
    }

    public static string DescribeAge(User user) =>
        user.Age is int age ? $"Age: {age}" : "Age: not set";

    public static int? ParseAge(string? text) =>
        int.TryParse(text, out var age) && age is >= 0 and <= 150 ? age : null;

    public static int TotalKnownAge(IEnumerable<User?> users) =>
        users.Sum(u => u?.Age ?? 0);
}
