using LearnHarness;

public class FakeClock : IClock
{
    public DateOnly Today { get; set; } = new(2026, 3, 1);
}

public class FakeSource(params Book?[] books) : IBookSource
{
    public async Task<IReadOnlyList<Book?>> GetBooksAsync()
    {
        await Task.Delay(20);
        return books;
    }
}

public class Tests
{
    private static readonly Book CleanCode = new("111", "Clean Code", "Robert Martin", Genre.Technology, 2008);
    private static readonly Book Dune = new("222", "Dune", "Frank Herbert", Genre.Fiction, 1965);
    private static readonly Book Cosmos = new("333", "Cosmos", "Carl Sagan", Genre.Science, 1980);
    private static readonly Book Refactoring = new("444", "Refactoring", "Martin Fowler", Genre.Technology, 1999);

    private static (Library Library, FakeClock Clock) Create()
    {
        var clock = new FakeClock();
        var lib = new Library(clock);
        lib.AddBook(CleanCode, 2);
        lib.AddBook(Dune);
        lib.AddBook(Cosmos);
        lib.RegisterMember("m1", "Sara");
        lib.RegisterMember("m2", "Omar");
        return (lib, clock);
    }

    [Test("إضافة نفس الكتاب تزيد النسخ", "Adding the same book adds copies")]
    public void Copies_add_up()
    {
        var (lib, _) = Create();
        lib.AddBook(CleanCode, 3);
        Assert.Equal(5, lib.Available("111"));
        Assert.Equal(0, lib.Available("nope"));
        Assert.Throws<ArgumentOutOfRangeException>(() => lib.AddBook(Dune, 0));
    }

    [Test("العضو المكرر مرفوض", "Duplicate member is rejected")]
    public void Duplicate_member()
    {
        var (lib, _) = Create();
        Assert.Throws<InvalidOperationException>(() => lib.RegisterMember("m1", "Someone"));
    }

    [Test("الإعارة: مدة 14 يوماً وتقلل المتاح", "Borrow: 14 days and reduces availability")]
    public void Borrow_creates_loan()
    {
        var (lib, _) = Create();
        var loan = lib.Borrow("m1", "111");
        Assert.Equal(new DateOnly(2026, 3, 15), loan.DueOn);
        Assert.Equal(new DateOnly(2026, 3, 1), loan.BorrowedOn);
        Assert.Equal(1, lib.Available("111"));
    }

    [Test("قواعد الإعارة: غير موجود، لا نسخ، الحد الأقصى", "Borrow rules: unknown, no copies, max loans")]
    public void Borrow_rules()
    {
        var (lib, _) = Create();
        Assert.Throws<KeyNotFoundException>(() => lib.Borrow("ghost", "111"));
        Assert.Throws<KeyNotFoundException>(() => lib.Borrow("m1", "999"));

        lib.Borrow("m2", "222");
        Assert.Throws<InvalidOperationException>(() => lib.Borrow("m1", "222")); // only 1 copy of Dune

        lib.AddBook(Refactoring);
        lib.Borrow("m1", "111");
        lib.Borrow("m1", "333");
        lib.Borrow("m1", "444");
        lib.AddBook(Dune, 1);
        Assert.Throws<InvalidOperationException>(() => lib.Borrow("m1", "222")); // 4th active loan
    }

    [Test("الإرجاع في الوقت: بلا غرامة", "On-time return: no fine")]
    public void Return_on_time()
    {
        var (lib, clock) = Create();
        lib.Borrow("m1", "222");
        clock.Today = new DateOnly(2026, 3, 15);
        Assert.Equal(0m, lib.Return("m1", "222"));
        Assert.Equal(1, lib.Available("222"));
        Assert.Throws<InvalidOperationException>(() => lib.Return("m1", "222"));
    }

    [Test("الإرجاع المتأخر: 0.50 لكل يوم بعد تاريخ الاستحقاق", "Late return: 0.50 per day after the due date")]
    public void Return_late()
    {
        var (lib, clock) = Create();
        lib.Borrow("m1", "222");
        clock.Today = new DateOnly(2026, 3, 19);   // due 15th → 4 days late
        Assert.Equal(2.00m, lib.Return("m1", "222"));
    }

    [Test("البحث في العنوان والمؤلف بدون حالة الأحرف ومرتب", "Search title and author, case-insensitive, ordered")]
    public void Search_books()
    {
        var (lib, _) = Create();
        lib.AddBook(Refactoring);
        Assert.SequenceEqual(new[] { "Clean Code", "Refactoring" }, lib.Search("MARTIN").Select(b => b.Title));
        Assert.SequenceEqual(new[] { "Dune" }, lib.Search("dun").Select(b => b.Title));
        Assert.Equal(0, lib.Search("zzz").Count);
    }

    [Test("الإعارات المتأخرة مرتبة حسب تاريخ الاستحقاق", "Overdue loans ordered by due date")]
    public void Overdue_loans()
    {
        var (lib, clock) = Create();
        lib.Borrow("m1", "222");
        clock.Today = new DateOnly(2026, 3, 5);
        lib.Borrow("m2", "111");
        clock.Today = new DateOnly(2026, 3, 18);   // first is overdue (due 15th), second due 19th
        var overdue = lib.Overdue();
        Assert.Equal(1, overdue.Count);
        Assert.Equal("m1", overdue[0].MemberId);

        clock.Today = new DateOnly(2026, 4, 1);
        Assert.SequenceEqual(new[] { "222", "111" }, lib.Overdue().Select(l => l.Isbn));
    }

    [Test("عدد العناوين لكل تصنيف (وليس النسخ)", "Titles per genre (not copies)")]
    public void Genres()
    {
        var (lib, _) = Create();
        lib.AddBook(Refactoring, 5);
        var map = lib.BooksPerGenre();
        Assert.Equal(2, map[Genre.Technology]);
        Assert.Equal(1, map[Genre.Fiction]);
        Assert.Equal(1, map[Genre.Science]);
        Assert.False(map.ContainsKey(Genre.History));
    }

    [Test("الاستيراد غير المتزامن يتجاهل الناقص والمكرر", "Async import skips invalid and existing books")]
    public async Task Import()
    {
        var (lib, _) = Create();
        var source = new FakeSource(
            Refactoring,
            null,
            new Book("", "No Isbn", "X", Genre.History, 2000),
            new Book("555", " ", "No Title", Genre.History, 2000),
            Dune,
            new Book("666", "SPQR", "Mary Beard", Genre.History, 2015));
        Assert.Equal(2, await lib.ImportAsync(source));
        Assert.Equal(1, lib.Available("666"));
        Assert.Equal(1, lib.Available("222"), "Existing books must not get extra copies from an import");
    }
}
