// Realistic mistake: Square HIDES Name with "new" instead of overriding it.
// Through an IShape or Rectangle reference, the Square says it's a "Rectangle".
public interface IShape
{
    string Name { get; }
    double Area();
    double Perimeter();
}

public class Circle : IShape
{
    public Circle(double radius) { Radius = radius; }
    public double Radius { get; }
    public string Name => "Circle";
    public double Area() => Math.PI * Radius * Radius;
    public double Perimeter() => 2 * Math.PI * Radius;
}

public class Rectangle : IShape
{
    public Rectangle(double width, double height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }
    public string Name => "Rectangle";
    public double Area() => Width * Height;
    public double Perimeter() => 2 * (Width + Height);
    public override string ToString() => $"{Name} {Width}x{Height}";
}

public class Square : Rectangle
{
    public Square(double side) : base(side, side) { }
    public new string Name => "Square";
}

public static class ShapeTools
{
    public static double TotalArea(IEnumerable<IShape> shapes) => shapes.Sum(s => s.Area());
    public static IShape Largest(IEnumerable<IShape> shapes) => shapes.MaxBy(s => s.Area())!;
}
