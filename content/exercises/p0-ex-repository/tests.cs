using LearnHarness;

public class Product : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public class Tests
{
    [Test("Add يعطي معرّفات متسلسلة", "Add assigns sequential ids")]
    public void Add_assigns_ids()
    {
        var repo = new InMemoryRepository<Product>();
        var a = repo.Add(new Product { Name = "Pen" });
        var b = repo.Add(new Product { Name = "Book" });
        Assert.Equal(1, a.Id);
        Assert.Equal(2, b.Id);
        Assert.Equal(2, repo.Count);
    }

    [Test("الـ Id الصريح يُحترم والعدّاد يتجاوزه", "Explicit ids are kept and the counter skips past them")]
    public void Add_respects_explicit_ids()
    {
        var repo = new InMemoryRepository<Product>();
        repo.Add(new Product { Id = 10, Name = "Lamp" });
        var next = repo.Add(new Product { Name = "Desk" });
        Assert.Equal(11, next.Id);
    }

    [Test("الـ Id المكرر يرمي InvalidOperationException", "Duplicate id throws InvalidOperationException")]
    public void Add_rejects_duplicates()
    {
        var repo = new InMemoryRepository<Product>();
        repo.Add(new Product { Id = 5 });
        Assert.Throws<InvalidOperationException>(() => repo.Add(new Product { Id = 5 }));
    }

    [Test("GetById يرجع العنصر أو null", "GetById returns the item or null")]
    public void GetById_returns_null_when_missing()
    {
        var repo = new InMemoryRepository<Product>();
        var pen = repo.Add(new Product { Name = "Pen" });
        Assert.Equal(pen, repo.GetById(pen.Id));
        Assert.Null(repo.GetById(999));
    }

    [Test("GetAll مرتبة حسب Id", "GetAll is ordered by Id")]
    public void GetAll_is_ordered()
    {
        var repo = new InMemoryRepository<Product>();
        repo.Add(new Product { Id = 7 });
        repo.Add(new Product { Id = 2 });
        repo.Add(new Product { Id = 4 });
        Assert.SequenceEqual(new[] { 2, 4, 7 }, repo.GetAll().Select(p => p.Id));
    }

    [Test("Remove يرجع true/false", "Remove returns true/false")]
    public void Remove_works()
    {
        var repo = new InMemoryRepository<Product>();
        var p = repo.Add(new Product());
        Assert.True(repo.Remove(p.Id));
        Assert.False(repo.Remove(p.Id));
        Assert.Equal(0, repo.Count);
    }

    [Test("Find مع شرط (Lambda)", "Find with a lambda predicate")]
    public void Find_filters()
    {
        var repo = new InMemoryRepository<Product>();
        repo.Add(new Product { Id = 3, Name = "Cheap", Price = 5 });
        repo.Add(new Product { Id = 1, Name = "Pricey", Price = 500 });
        repo.Add(new Product { Id = 2, Name = "Mid", Price = 50 });
        Assert.SequenceEqual(new[] { 2, 3 }, repo.Find(p => p.Price < 100).Select(p => p.Id));
    }

    [Test("المستودع يعمل مع أي نوع يطبّق IEntity", "The repository works with any IEntity type")]
    public void Works_with_other_types()
    {
        var repo = new InMemoryRepository<Customer>();
        repo.Add(new Customer { Email = "a@b.c" });
        Assert.Equal("a@b.c", repo.GetById(1)!.Email);
    }

    [Test("CountBy يعدّ حسب المفتاح", "CountBy counts by key")]
    public void CountBy_counts()
    {
        var counts = CollectionHelpers.CountBy(new[] { "a", "bb", "cc", "ddd" }, s => s.Length);
        Assert.Equal(3, counts.Count);
        Assert.Equal(1, counts[1]);
        Assert.Equal(2, counts[2]);
        Assert.Equal(1, counts[3]);
    }

    public class Customer : IEntity
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
    }
}
