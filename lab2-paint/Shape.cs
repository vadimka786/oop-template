using System.Drawing;

namespace Lab2Paint;

public abstract class Shape
{
    public Point Start { get; protected set; }
    public Point End { get; protected set; }
    public Color Color { get; }
    public int Width { get; }

    protected Shape(Point start, Color color, int width)
    {
        Start = End = start;
        Color = color;
        Width = width;
    }


    public virtual void MoveEnd(Point p) => End = p;


    public virtual void Move(int dx, int dy)
    {
        Start = new Point(Start.X + dx, Start.Y + dy);
        End = new Point(End.X + dx, End.Y + dy);
    }


    public virtual bool IsEmpty => Start == End;

    public virtual List<Point> GetPoints() => new() { Start, End };

    public virtual void Restore(IReadOnlyList<Point> points)
    {
        Start = points[0];
        End = points[^1];
    }


    public virtual Rectangle Bounds() => new(
        Math.Min(Start.X, End.X),
        Math.Min(Start.Y, End.Y),
        Math.Abs(End.X - Start.X),
        Math.Abs(End.Y - Start.Y));

    public abstract void Draw(Graphics g);

    public abstract bool HitTest(Point p);

    protected double Tolerance => Width / 2.0 + 4;

    protected static double DistToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len2 = dx * dx + dy * dy;
        double t = len2 == 0 ? 0 : ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
        t = Math.Clamp(t, 0, 1);
        double px = a.X + t * dx, py = a.Y + t * dy;
        return Math.Sqrt((p.X - px) * (p.X - px) + (p.Y - py) * (p.Y - py));
    }
}
