using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lab2Paint;

public class LineShape : Shape
{
    public LineShape(Point start, Color color, int width) : base(start, color, width) { }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(Color, Width) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, Start, End);
    }

    public override bool HitTest(Point p) => DistToSegment(p, Start, End) <= Tolerance;
}

public class RectShape : Shape
{
    public RectShape(Point start, Color color, int width) : base(start, color, width) { }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(Color, Width);
        g.DrawRectangle(pen, Bounds());
    }

    public override bool HitTest(Point p)
    {
        var r = Bounds();
        r.Inflate((int)Tolerance, (int)Tolerance);
        return r.Contains(p);
    }
}

public class EllipseShape : Shape
{
    public EllipseShape(Point start, Color color, int width) : base(start, color, width) { }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(Color, Width);
        g.DrawEllipse(pen, Bounds());
    }

    public override bool HitTest(Point p)
    {
        var r = Bounds();
        r.Inflate((int)Tolerance, (int)Tolerance);
        if (r.Width == 0 || r.Height == 0) return false;
        double a = r.Width / 2.0, b = r.Height / 2.0;
        double cx = r.X + a, cy = r.Y + b;
        double nx = (p.X - cx) / a, ny = (p.Y - cy) / b;
        return nx * nx + ny * ny <= 1;
    }
}

public class PencilShape : Shape
{
    private readonly List<Point> _points = new();

    public PencilShape(Point start, Color color, int width) : base(start, color, width)
    {
        _points.Add(start);
    }

    public override void MoveEnd(Point p)
    {
        base.MoveEnd(p);
        _points.Add(p);
    }

    public override void Move(int dx, int dy)
    {
        base.Move(dx, dy);
        for (int i = 0; i < _points.Count; i++)
            _points[i] = new Point(_points[i].X + dx, _points[i].Y + dy);
    }

    public override bool IsEmpty => _points.Count < 2;

    public override List<Point> GetPoints() => new(_points);

    public override void Restore(IReadOnlyList<Point> points)
    {
        base.Restore(points);
        _points.Clear();
        _points.AddRange(points);
    }

    public override Rectangle Bounds()
    {
        if (_points.Count == 0) return Rectangle.Empty;
        int minX = _points.Min(p => p.X), maxX = _points.Max(p => p.X);
        int minY = _points.Min(p => p.Y), maxY = _points.Max(p => p.Y);
        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    public override void Draw(Graphics g)
    {
        if (_points.Count < 2) return;
        using var pen = new Pen(Color, Width) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, _points.ToArray());
    }

    public override bool HitTest(Point p)
    {
        for (int i = 1; i < _points.Count; i++)
            if (DistToSegment(p, _points[i - 1], _points[i]) <= Tolerance) return true;
        return false;
    }
}
