public interface IShape
{
    string Name { get; }
    double Area();
    double Perimeter();
}

// مثال جاهز: صنف يطبّق الواجهة
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
        throw new NotImplementedException();
    }

    public double Width { get; }
    public double Height { get; }

    public string Name => throw new NotImplementedException();
    public double Area() => throw new NotImplementedException();
    public double Perimeter() => throw new NotImplementedException();
}

// TODO: اجعل Square يرث من Rectangle
public class Square
{
    public Square(double side)
    {
        throw new NotImplementedException();
    }
}

public static class ShapeTools
{
    public static double TotalArea(IEnumerable<IShape> shapes) => throw new NotImplementedException();

    public static IShape Largest(IEnumerable<IShape> shapes) => throw new NotImplementedException();
}
