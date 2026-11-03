// Realistic mistakes: "??" lets a blank display name through, and the null-forgiving
// operator "!" silences the compiler instead of handling a missing address.
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
    public static string NameToShow(User user) => user.DisplayName ?? user.UserName;

    public static string CityOf(User? user) => user!.Address!.City ?? "Unknown";

    public static string DescribeAge(User user) => $"Age: {user.Age}";

    public static int? ParseAge(string? text) => int.TryParse(text, out var age) ? age : null;

    public static int TotalKnownAge(IEnumerable<User?> users) => users.Sum(u => u!.Age ?? 0);
}
