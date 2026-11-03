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
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Id == 0)
            entity.Id = _nextId;
        if (_items.ContainsKey(entity.Id))
            throw new InvalidOperationException($"An entity with Id {entity.Id} already exists.");

        _items.Add(entity.Id, entity);
        _nextId = Math.Max(_nextId, entity.Id + 1);
        return entity;
    }

    // TryGetValue never throws: a missing id simply returns null.
    public T? GetById(int id) => _items.TryGetValue(id, out var item) ? item : null;

    public IReadOnlyList<T> GetAll() => _items.Values.OrderBy(e => e.Id).ToList();

    public bool Remove(int id) => _items.Remove(id);

    public IReadOnlyList<T> Find(Func<T, bool> predicate) =>
        _items.Values.Where(predicate).OrderBy(e => e.Id).ToList();
}

public static class CollectionHelpers
{
    public static Dictionary<TKey, int> CountBy<T, TKey>(IEnumerable<T> items, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var counts = new Dictionary<TKey, int>();
        foreach (var item in items)
        {
            var key = keySelector(item);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }
        return counts;
    }
}
