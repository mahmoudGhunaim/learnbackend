public interface IShape
{
    string Name { get; }
    double Area();
    double Perimeter();
}

public class Circle : IShape
{
    public Circle(double radius)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }

    public double Radius { get; }
    public string Name => "Circle";
    public double Area() => Math.PI * Radius * Radius;
    public double Perimeter() => 2 * Math.PI * Radius;
}

public class Rectangle : IShape
{
    public Rectangle(double width, double height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }

    // virtual: derived classes are allowed to replace this implementation.
    public virtual string Name => "Rectangle";
    public double Area() => Width * Height;
    public double Perimeter() => 2 * (Width + Height);

    public override string ToString() => $"{Name} {Width}x{Height}";
}

public class Square : Rectangle
{
    public Square(double side) : base(side, side) { }

    // override (not new): works through IShape and Rectangle references too.
    public override string Name => "Square";
}

public static class ShapeTools
{
    public static double TotalArea(IEnumerable<IShape> shapes) => shapes.Sum(s => s.Area());

    public static IShape Largest(IEnumerable<IShape> shapes)
    {
        var list = shapes.ToList();
        if (list.Count == 0) throw new InvalidOperationException("No shapes.");
        return list.MaxBy(s => s.Area())!;
    }
}
