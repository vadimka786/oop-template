namespace Lab2Paint;
public interface ICommand
{
    void Execute();
    void Undo();
}

public class AddShapeCommand : ICommand
{
    private readonly Drawing _drawing;
    private readonly Shape _shape;

    public AddShapeCommand(Drawing drawing, Shape shape)
    {
        _drawing = drawing;
        _shape = shape;
    }

    public void Execute() => _drawing.Add(_shape);
    public void Undo() => _drawing.Remove(_shape);
}

public class DeleteShapeCommand : ICommand
{
    private readonly Drawing _drawing;
    private readonly Shape _shape;
    private int _index;

    public DeleteShapeCommand(Drawing drawing, Shape shape)
    {
        _drawing = drawing;
        _shape = shape;
    }

    public void Execute()
    {
        _index = _drawing.IndexOf(_shape);
        _drawing.Remove(_shape);
    }

    public void Undo() => _drawing.Insert(Math.Clamp(_index, 0, _drawing.Count), _shape);
}

public class ClearCommand : ICommand
{
    private readonly Drawing _drawing;
    private List<Shape> _backup = new();

    public ClearCommand(Drawing drawing) => _drawing = drawing;

    public void Execute()
    {
        _backup = _drawing.Shapes.ToList();
        _drawing.Clear();
    }

    public void Undo()
    {
        _drawing.Clear();
        _drawing.AddRange(_backup);
    }
}

public class MoveShapeCommand : ICommand
{
    private readonly Shape _shape;
    private readonly int _dx, _dy;

    public MoveShapeCommand(Shape shape, int dx, int dy)
    {
        _shape = shape;
        _dx = dx;
        _dy = dy;
    }

    public void Execute() => _shape.Move(_dx, _dy);
    public void Undo() => _shape.Move(-_dx, -_dy);
}

public class History
{
    private readonly Stack<ICommand> _undo = new();
    private readonly Stack<ICommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Do(ICommand command)
    {
        command.Execute();
        _undo.Push(command);
        _redo.Clear();     
    }

    public void Record(ICommand command)
    {
        _undo.Push(command);
        _redo.Clear();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var command = _undo.Pop();
        command.Undo();
        _redo.Push(command);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var command = _redo.Pop();
        command.Execute();
        _undo.Push(command);
    }

    public void Reset()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
