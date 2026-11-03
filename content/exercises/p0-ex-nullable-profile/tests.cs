using LearnHarness;

public class Tests
{
    [Test("DisplayName يُستخدم إذا كان موجوداً", "DisplayName is used when present")]
    public void Name_uses_display_name() =>
        Assert.Equal("Sara A.", ProfileFormatter.NameToShow(new User { UserName = "sara99", DisplayName = "Sara A." }));

    [Test("DisplayName = null يعود لـ UserName", "Null DisplayName falls back to UserName")]
    public void Name_null_falls_back() =>
        Assert.Equal("sara99", ProfileFormatter.NameToShow(new User { UserName = "sara99" }));

    [Test("DisplayName مسافات فقط يعود لـ UserName", "Whitespace DisplayName falls back to UserName")]
    public void Name_blank_falls_back() =>
        Assert.Equal("sara99", ProfileFormatter.NameToShow(new User { UserName = "sara99", DisplayName = "   " }));

    [Test("المدينة عندما تكون موجودة", "City when present")]
    public void City_present() =>
        Assert.Equal("Amman", ProfileFormatter.CityOf(new User { UserName = "u", Address = new Address { City = "Amman" } }));

    [Test("المدينة Unknown لأي حلقة null في السلسلة", "City is Unknown for any null link in the chain")]
    public void City_missing()
    {
        Assert.Equal("Unknown", ProfileFormatter.CityOf(null));
        Assert.Equal("Unknown", ProfileFormatter.CityOf(new User { UserName = "u" }));
        Assert.Equal("Unknown", ProfileFormatter.CityOf(new User { UserName = "u", Address = new Address() }));
        Assert.Equal("Unknown", ProfileFormatter.CityOf(new User { UserName = "u", Address = new Address { City = "" } }));
    }

    [Test("وصف العمر", "Age description")]
    public void Describe_age()
    {
        Assert.Equal("Age: 30", ProfileFormatter.DescribeAge(new User { UserName = "u", Age = 30 }));
        Assert.Equal("Age: not set", ProfileFormatter.DescribeAge(new User { UserName = "u" }));
    }

    [Test("تحويل نص العمر بأمان", "Parse age text safely")]
    public void Parse_age()
    {
        Assert.Equal((int?)42, ProfileFormatter.ParseAge("42"));
        Assert.Null(ProfileFormatter.ParseAge(null));
        Assert.Null(ProfileFormatter.ParseAge("abc"));
        Assert.Null(ProfileFormatter.ParseAge("-3"));
        Assert.Null(ProfileFormatter.ParseAge("999"));
    }

    [Test("مجموع الأعمار المعروفة فقط", "Sum of known ages only")]
    public void Total_age()
    {
        var users = new User?[]
        {
            new User { UserName = "a", Age = 20 },
            null,
            new User { UserName = "b" },
            new User { UserName = "c", Age = 31 },
        };
        Assert.Equal(51, ProfileFormatter.TotalKnownAge(users));
    }
}
