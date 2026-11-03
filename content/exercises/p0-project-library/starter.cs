public enum Genre { Fiction, Science, History, Technology }

public record Book(string Isbn, string Title, string Author, Genre Genre, int Year);

public record Loan(string MemberId, string Isbn, DateOnly BorrowedOn, DateOnly DueOn);

// الساعة كواجهة: الاختبارات تمرر ساعة مزيفة للتحكم في "اليوم"
public interface IClock
{
    DateOnly Today { get; }
}

// مصدر خارجي للكتب (مثل API آخر). قد يحتوي كتباً ناقصة البيانات.
public interface IBookSource
{
    Task<IReadOnlyList<Book?>> GetBooksAsync();
}

public class Library(IClock clock)
{
    public const int LoanDays = 14;
    public const int MaxActiveLoans = 3;
    public const decimal FinePerDay = 0.50m;

    public void AddBook(Book book, int copies = 1) => throw new NotImplementedException();

    public void RegisterMember(string memberId, string name) => throw new NotImplementedException();

    public Loan Borrow(string memberId, string isbn) => throw new NotImplementedException();

    public decimal Return(string memberId, string isbn) => throw new NotImplementedException();

    public int Available(string isbn) => throw new NotImplementedException();

    public IReadOnlyList<Book> Search(string text) => throw new NotImplementedException();

    public IReadOnlyList<Loan> Overdue() => throw new NotImplementedException();

    public Dictionary<Genre, int> BooksPerGenre() => throw new NotImplementedException();

    public Task<int> ImportAsync(IBookSource source) => throw new NotImplementedException();
}
