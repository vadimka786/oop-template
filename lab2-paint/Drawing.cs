using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;

namespace Lab2Paint;

public record PointDto(int X, int Y);
public record ShapeDto(string Type, int Color, int Width, List<PointDto> Points);

public class Drawing
{
    private readonly List<Shape> _shapes = new();

    public IReadOnlyList<Shape> Shapes => _shapes;
    public int Count => _shapes.Count;

    public void Add(Shape shape) => _shapes.Add(shape);
    public void Insert(int index, Shape shape) => _shapes.Insert(index, shape);
    public void AddRange(IEnumerable<Shape> shapes) => _shapes.AddRange(shapes);
    public bool Remove(Shape shape) => _shapes.Remove(shape);
    public int IndexOf(Shape shape) => _shapes.IndexOf(shape);
    public bool Contains(Shape shape) => _shapes.Contains(shape);

    public void RemoveLast()
    {
        if (_shapes.Count > 0) _shapes.RemoveAt(_shapes.Count - 1);
    }

    public void Clear() => _shapes.Clear();

    public Shape? HitTest(Point p)
    {
        for (int i = _shapes.Count - 1; i >= 0; i--)
            if (_shapes[i].HitTest(p)) return _shapes[i];
        return null;
    }

    public void Draw(Graphics g, Shape? selected = null)
    {
        foreach (var shape in _shapes)
            shape.Draw(g);

        if (selected != null && _shapes.Contains(selected))
        {
            var r = selected.Bounds();
            r.Inflate(6, 6);
            using var pen = new Pen(Color.Red, 1) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, r);
        }
    }

    public void Save(string path)
    {
        var dtos = _shapes.Select(s => new ShapeDto(
            s.GetType().Name,
            s.Color.ToArgb(),
            s.Width,
            s.GetPoints().Select(p => new PointDto(p.X, p.Y)).ToList())).ToList();

        File.WriteAllText(path, JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Load(string path)
    {
        var dtos = JsonSerializer.Deserialize<List<ShapeDto>>(File.ReadAllText(path))
                ?? throw new InvalidDataException("Пустой файл");

        var loaded = new List<Shape>();
        foreach (var dto in dtos)
        {
            if (!ShapeFactory.Kinds.ContainsKey(dto.Type))
                throw new InvalidDataException($"Неизвестный тип фигуры: {dto.Type}");
            if (dto.Points.Count == 0)
                throw new InvalidDataException("Фигура без точек");

            var points = dto.Points.Select(p => new Point(p.X, p.Y)).ToList();
            var shape = ShapeFactory.Create(dto.Type, points[0], Color.FromArgb(dto.Color), dto.Width);
            shape.Restore(points);
            loaded.Add(shape);
        }

        _shapes.Clear();
        _shapes.AddRange(loaded);
    }
}
