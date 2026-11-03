public enum Genre { Fiction, Science, History, Technology }

public record Book(string Isbn, string Title, string Author, Genre Genre, int Year);

public record Loan(string MemberId, string Isbn, DateOnly BorrowedOn, DateOnly DueOn);

public interface IClock
{
    DateOnly Today { get; }
}

public interface IBookSource
{
    Task<IReadOnlyList<Book?>> GetBooksAsync();
}

public class Library(IClock clock)
{
    public const int LoanDays = 14;
    public const int MaxActiveLoans = 3;
    public const decimal FinePerDay = 0.50m;

    private readonly Dictionary<string, Book> _books = new();
    private readonly Dictionary<string, int> _copies = new();
    private readonly Dictionary<string, string> _members = new();
    private readonly List<Loan> _activeLoans = new();

    public void AddBook(Book book, int copies = 1)
    {
        ArgumentNullException.ThrowIfNull(book);
        if (copies <= 0) throw new ArgumentOutOfRangeException(nameof(copies));

        _books.TryAdd(book.Isbn, book);
        _copies[book.Isbn] = _copies.GetValueOrDefault(book.Isbn) + copies;
    }

    public void RegisterMember(string memberId, string name)
    {
        if (!_members.TryAdd(memberId, name))
            throw new InvalidOperationException($"Member '{memberId}' already exists.");
    }

    public Loan Borrow(string memberId, string isbn)
    {
        if (!_members.ContainsKey(memberId)) throw new KeyNotFoundException($"Unknown member '{memberId}'.");
        if (!_books.ContainsKey(isbn)) throw new KeyNotFoundException($"Unknown book '{isbn}'.");
        if (Available(isbn) == 0) throw new InvalidOperationException("No copies available.");
        if (_activeLoans.Count(l => l.MemberId == memberId) >= MaxActiveLoans)
            throw new InvalidOperationException($"A member can have at most {MaxActiveLoans} active loans.");

        var today = clock.Today;
        var loan = new Loan(memberId, isbn, today, today.AddDays(LoanDays));
        _activeLoans.Add(loan);
        return loan;
    }

    public decimal Return(string memberId, string isbn)
    {
        var loan = _activeLoans.FirstOrDefault(l => l.MemberId == memberId && l.Isbn == isbn)
                   ?? throw new InvalidOperationException("This member has not borrowed this book.");
        _activeLoans.Remove(loan);

        var daysLate = clock.Today.DayNumber - loan.DueOn.DayNumber;
        return daysLate > 0 ? daysLate * FinePerDay : 0m;
    }

    public int Available(string isbn) =>
        _copies.GetValueOrDefault(isbn) - _activeLoans.Count(l => l.Isbn == isbn);

    public IReadOnlyList<Book> Search(string text) =>
        _books.Values
            .Where(b => b.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                     || b.Author.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.Title)
            .ToList();

    public IReadOnlyList<Loan> Overdue() =>
        _activeLoans.Where(l => l.DueOn < clock.Today).OrderBy(l => l.DueOn).ToList();

    public Dictionary<Genre, int> BooksPerGenre() =>
        _books.Values.GroupBy(b => b.Genre).ToDictionary(g => g.Key, g => g.Count());

    public async Task<int> ImportAsync(IBookSource source)
    {
        var incoming = await source.GetBooksAsync();
        var added = 0;
        foreach (var book in incoming)
        {
            if (book is null || string.IsNullOrWhiteSpace(book.Isbn) || string.IsNullOrWhiteSpace(book.Title)) continue;
            if (_books.ContainsKey(book.Isbn)) continue;
            AddBook(book);
            added++;
        }
        return added;
    }
}
