// Realistic mistakes: available copies ignore active loans, search is case-sensitive,
// and the late fine counts from the borrow date instead of the due date.
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
    private readonly List<Loan> _loans = new();

    public void AddBook(Book book, int copies = 1)
    {
        _books[book.Isbn] = book;
        _copies[book.Isbn] = copies;
    }

    public void RegisterMember(string memberId, string name) => _members[memberId] = name;

    public Loan Borrow(string memberId, string isbn)
    {
        if (Available(isbn) == 0) throw new InvalidOperationException("No copies available.");
        var loan = new Loan(memberId, isbn, clock.Today, clock.Today.AddDays(LoanDays));
        _loans.Add(loan);
        return loan;
    }

    public decimal Return(string memberId, string isbn)
    {
        var loan = _loans.First(l => l.MemberId == memberId && l.Isbn == isbn);
        _loans.Remove(loan);
        return (clock.Today.DayNumber - loan.BorrowedOn.DayNumber) * FinePerDay;
    }

    public int Available(string isbn) => _copies.GetValueOrDefault(isbn);

    public IReadOnlyList<Book> Search(string text) =>
        _books.Values.Where(b => b.Title.Contains(text) || b.Author.Contains(text)).ToList();

    public IReadOnlyList<Loan> Overdue() => _loans.Where(l => l.DueOn < clock.Today).ToList();

    public Dictionary<Genre, int> BooksPerGenre() =>
        _books.Values.GroupBy(b => b.Genre).ToDictionary(g => g.Key, g => g.Sum(b => _copies[b.Isbn]));

    public async Task<int> ImportAsync(IBookSource source)
    {
        var incoming = await source.GetBooksAsync();
        foreach (var book in incoming) AddBook(book!);
        return incoming.Count;
    }
}
