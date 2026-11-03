public interface IEntity
{
    int Id { get; set; }
}

// T يجب أن يكون صنفاً (class) ويطبّق IEntity: هذا هو الـ Generic constraint
public class InMemoryRepository<T> where T : class, IEntity
{
    public int Count => throw new NotImplementedException();

    public T Add(T entity) => throw new NotImplementedException();

    public T? GetById(int id) => throw new NotImplementedException();

    public IReadOnlyList<T> GetAll() => throw new NotImplementedException();

    public bool Remove(int id) => throw new NotImplementedException();

    public IReadOnlyList<T> Find(Func<T, bool> predicate) => throw new NotImplementedException();
}

public static class CollectionHelpers
{
    // ["a", "bb", "cc"] مع المفتاح s => s.Length  →  { 1: 1, 2: 2 }
    public static Dictionary<TKey, int> CountBy<T, TKey>(IEnumerable<T> items, Func<T, TKey> keySelector)
        where TKey : notnull
        => throw new NotImplementedException();
}
