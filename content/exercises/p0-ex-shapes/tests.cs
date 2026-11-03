using LearnHarness;

public class Tests
{
    [Test("مساحة ومحيط المستطيل", "Rectangle area and perimeter")]
    public void Rectangle_math()
    {
        var r = new Rectangle(2, 3);
        Assert.Near(6, r.Area());
        Assert.Near(10, r.Perimeter());
        Assert.Equal("Rectangle", r.Name);
    }

    [Test("الأبعاد غير الموجبة مرفوضة", "Non-positive dimensions are rejected")]
    public void Rectangle_validates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rectangle(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rectangle(2, -1));
    }

    [Test("المربع يرث من المستطيل", "Square inherits from Rectangle")]
    public void Square_is_a_rectangle()
    {
        object s = new Square(4);
        Assert.True(s is Rectangle, "Square should inherit from Rectangle");
        Assert.True(s is IShape, "Square should be an IShape (through Rectangle)");
    }

    [Test("اسم المربع صحيح حتى عبر IShape (override وليس new)", "Square's name is right through IShape (override, not new)")]
    public void Square_name_is_polymorphic()
    {
        IShape asShape = (IShape)(object)new Square(2);
        Rectangle asRectangle = (Rectangle)(object)new Square(2);
        Assert.Equal("Square", asShape.Name);
        Assert.Equal("Square", asRectangle.Name);
    }

    [Test("ToString يستخدم الاسم والأبعاد", "ToString uses the name and the dimensions")]
    public void ToString_format()
    {
        Assert.Equal("Rectangle 2x3", new Rectangle(2, 3).ToString());
        Assert.Equal("Square 2.5x2.5", ((object)new Square(2.5)).ToString());
    }

    [Test("مجموع المساحات لأشكال مختلفة", "Total area of mixed shapes")]
    public void Total_area()
    {
        var shapes = new IShape[] { new Rectangle(2, 3), (IShape)(object)new Square(2), new Circle(1) };
        Assert.Near(6 + 4 + Math.PI, ShapeTools.TotalArea(shapes), 1e-9);
        Assert.Near(0, ShapeTools.TotalArea(Array.Empty<IShape>()));
    }

    [Test("الشكل الأكبر مساحة", "Largest shape by area")]
    public void Largest_shape()
    {
        var big = new Circle(3);
        var shapes = new IShape[] { new Rectangle(2, 3), big, (IShape)(object)new Square(4) };
        Assert.Equal(big, ShapeTools.Largest(shapes));
    }

    [Test("القائمة الفارغة ترمي InvalidOperationException", "Empty list throws InvalidOperationException")]
    public void Largest_empty() =>
        Assert.Throws<InvalidOperationException>(() => ShapeTools.Largest(new List<IShape>()));
}
