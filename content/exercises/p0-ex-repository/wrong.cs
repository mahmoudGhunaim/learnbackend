// Realistic mistakes: the Dictionary indexer throws for a missing key,
// and the id counter ignores explicitly assigned ids.
public interface IEntity
{
    int Id { get; set; }
}

public class InMemoryRepository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();
    private int _nextId = 1;

    public int Count => _items.Count;

    public T Add(T entity)
    {
        if (entity.Id == 0) entity.Id = _nextId++;
        _items[entity.Id] = entity;
        return entity;
    }

    public T? GetById(int id) => _items[id];
    public IReadOnlyList<T> GetAll() => _items.Values.ToList();
    public bool Remove(int id) => _items.Remove(id);
    public IReadOnlyList<T> Find(Func<T, bool> predicate) => _items.Values.Where(predicate).ToList();
}

public static class CollectionHelpers
{
    public static Dictionary<TKey, int> CountBy<T, TKey>(IEnumerable<T> items, Func<T, TKey> keySelector)
        where TKey : notnull
        => items.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.Count());
}
