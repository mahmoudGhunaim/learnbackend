public class Address
{
    public string? City { get; set; }      // قد تكون null
}

public class User
{
    public required string UserName { get; init; }   // إلزامية ولا تقبل null
    public string? DisplayName { get; set; }          // اختيارية
    public Address? Address { get; set; }             // اختيارية
    public int? Age { get; set; }                     // Nullable value type
}

public static class ProfileFormatter
{
    public static string NameToShow(User user) => throw new NotImplementedException();

    public static string CityOf(User? user) => throw new NotImplementedException();

    public static string DescribeAge(User user) => throw new NotImplementedException();

    public static int? ParseAge(string? text) => throw new NotImplementedException();

    public static int TotalKnownAge(IEnumerable<User?> users) => throw new NotImplementedException();
}
