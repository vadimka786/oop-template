using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lab2Paint;

public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
    }
}

public class MainForm : Form
{
    private const string SelectTool = "Select";

    private static readonly Dictionary<string, string> ToolTitles = new()
    {
        ["LineShape"] = "╱ Линия",
        ["RectShape"] = "▭ Прямоугольник",
        ["EllipseShape"] = "◯ Эллипс",
        ["PencilShape"] = "✎ Карандаш",
        [SelectTool] = "⬚ Выбор",
    };

    private static readonly (Color Color, string Name)[] Palette =
    {
        (Color.Black, "чёрный"),
        (Color.FromArgb(0xE0, 0x30, 0x30), "красный"),
        (Color.FromArgb(0x2B, 0x6C, 0xC4), "синий"),
        (Color.FromArgb(0x2E, 0x9E, 0x4F), "зелёный"),
        (Color.FromArgb(0xE8, 0xA0, 0x10), "оранжевый"),
    };

    private readonly Drawing _drawing = new();
    private readonly History _history = new();
    private readonly DoubleBufferedPanel _canvas = new();
    private readonly ToolStripStatusLabel _status = new();
    private readonly ComboBox _widthBox = new();
    private readonly List<Button> _colorButtons = new();
    private readonly Button _undoButton = new();
    private readonly Button _redoButton = new();

    private string _tool = "RectShape";
    private Color _color = Color.Black;
    private string _colorName = "чёрный";
    private int _width = 4;

    private Shape? _current; 
    private Shape? _selected; 
    private bool _dragging;
    private Point _last;
    private int _totalDx, _totalDy;
    private string? _filePath;

    public MainForm()
    {
        Text = "Paint — новый рисунок";
        ClientSize = new Size(960, 640);
        MinimumSize = new Size(760, 400);
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        UpdateStatus();
    }

    // ---------- интерфейс ----------

    private void BuildUi()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6, 6, 6, 2),
            BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3),
        };

        foreach (var kind in ShapeFactory.Kinds.Keys)
            bar.Controls.Add(MakeToolButton(kind));
        bar.Controls.Add(MakeToolButton(SelectTool));

        bar.Controls.Add(Spacer(10));
        foreach (var (color, name) in Palette)
        {
            var b = new Button
            {
                Size = new Size(26, 26),
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3),
                TabStop = false,
            };
            b.FlatAppearance.BorderColor = Color.DimGray;
            var c = color; var n = name;
            b.Click += (_, _) => { _color = c; _colorName = n; RefreshColorButtons(); UpdateStatus(); };
            _colorButtons.Add(b);
            bar.Controls.Add(b);
        }
        RefreshColorButtons();

        bar.Controls.Add(Spacer(10));
        bar.Controls.Add(new Label { Text = "Толщина", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });
        _widthBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _widthBox.Width = 52;
        _widthBox.Margin = new Padding(3, 6, 3, 3);
        _widthBox.Items.AddRange(new object[] { 1, 2, 4, 6, 8, 12 });
        _widthBox.SelectedItem = _width;
        _widthBox.SelectedIndexChanged += (_, _) =>
        {
            _width = (int)_widthBox.SelectedItem!;
            UpdateStatus();
        };
        bar.Controls.Add(_widthBox);

        bar.Controls.Add(Spacer(10));
        _undoButton.Text = "↶ Отменить";
        _undoButton.AutoSize = true;
        _undoButton.Click += (_, _) => DoUndo();
        _redoButton.Text = "↷ Повторить";
        _redoButton.AutoSize = true;
        _redoButton.Click += (_, _) => DoRedo();
        var clearButton = new Button { Text = "Очистить", AutoSize = true };
        clearButton.Click += (_, _) => DoClear();
        var openButton = new Button { Text = "Открыть…", AutoSize = true };
        openButton.Click += (_, _) => OpenFile();
        var saveButton = new Button { Text = "Сохранить…", AutoSize = true };
        saveButton.Click += (_, _) => SaveFile();
        bar.Controls.AddRange(new Control[] { _undoButton, _redoButton, clearButton, Spacer(10), openButton, saveButton });

        _canvas.Dock = DockStyle.Fill;
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);

        Controls.Add(_canvas);
        Controls.Add(bar);
        Controls.Add(statusStrip);
    }

    private static Control Spacer(int w) => new Panel { Width = w, Height = 1, Margin = new Padding(0) };

    private RadioButton MakeToolButton(string kind)
    {
        var rb = new RadioButton
        {
            Appearance = Appearance.Button,
            Text = ToolTitles[kind],
            AutoSize = true,
            Padding = new Padding(4, 2, 4, 2),
            Margin = new Padding(3),
            Checked = kind == _tool,
            TabStop = false,
        };
        rb.CheckedChanged += (_, _) =>
        {
            if (!rb.Checked) return;
            _tool = kind;
            _canvas.Cursor = kind == SelectTool ? Cursors.Default : Cursors.Cross;
            if (kind != SelectTool) { _selected = null; _canvas.Invalidate(); }
            UpdateStatus();
        };
        return rb;
    }

    private void RefreshColorButtons()
    {
        for (int i = 0; i < _colorButtons.Count; i++)
        {
            bool active = Palette[i].Color == _color;
            _colorButtons[i].FlatAppearance.BorderSize = active ? 3 : 1;
            _colorButtons[i].FlatAppearance.BorderColor = active ? Color.DodgerBlue : Color.DimGray;
        }
    }

    private void UpdateStatus()
    {
        string tool = ToolTitles[_tool].Substring(2);
        _status.Text = $"{tool} · {_colorName} · {_width} px · фигур на холсте: {_drawing.Count} · Ctrl+Z — отменить, Ctrl+Y — повторить";
        _undoButton.Enabled = _history.CanUndo;
        _redoButton.Enabled = _history.CanRedo;
    }

    // ---------- рисование ----------

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        _drawing.Draw(e.Graphics, _selected);   
        _current?.Draw(e.Graphics);    
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _canvas.Focus();

        if (_tool == SelectTool)
        {
            _selected = _drawing.HitTest(e.Location);
            _dragging = _selected != null;
            _last = e.Location;
            _totalDx = _totalDy = 0;
        }
        else
        {
            _selected = null;
            _current = ShapeFactory.Create(_tool, e.Location, _color, _width);
        }
        _canvas.Invalidate();
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_current != null)
        {
            _current.MoveEnd(e.Location);
            _canvas.Invalidate();
        }
        else if (_dragging && _selected != null)
        {
            int dx = e.X - _last.X, dy = e.Y - _last.Y;
            _selected.Move(dx, dy);
            _totalDx += dx;
            _totalDy += dy;
            _last = e.Location;
            _canvas.Invalidate();
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        if (_current != null)
        {
            var shape = _current;
            _current = null;
            if (!shape.IsEmpty)
                _history.Do(new AddShapeCommand(_drawing, shape));
        }
        else if (_dragging && _selected != null)
        {
            if (_totalDx != 0 || _totalDy != 0)
                _history.Record(new MoveShapeCommand(_selected, _totalDx, _totalDy));
        }
        _dragging = false;
        _canvas.Invalidate();
        UpdateStatus();
    }




    private bool Busy => _current != null || _dragging;

    private void DoUndo()
    {
        if (Busy) return;
        _history.Undo();
        AfterHistoryChange();
    }

    private void DoRedo()
    {
        if (Busy) return;
        _history.Redo();
        AfterHistoryChange();
    }

    private void DoClear()
    {
        if (Busy || _drawing.Count == 0) return;
        _history.Do(new ClearCommand(_drawing));
        AfterHistoryChange();
    }

    private void DeleteSelected()
    {
        if (Busy || _selected == null) return;
        _history.Do(new DeleteShapeCommand(_drawing, _selected));
        _selected = null;
        AfterHistoryChange();
    }

    private void AfterHistoryChange()
    {
        if (_selected != null && !_drawing.Contains(_selected)) _selected = null;
        _canvas.Invalidate();
        UpdateStatus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Z: DoUndo(); return true;
            case Keys.Control | Keys.Y: DoRedo(); return true;
            case Keys.Delete when _selected != null: DeleteSelected(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }




    private const string FileFilter = "Рисунок (*.json)|*.json|Все файлы (*.*)|*.*";

    private void SaveFile()
    {
        using var dlg = new SaveFileDialog { Filter = FileFilter, DefaultExt = "json", FileName = _filePath ?? "рисунок.json" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _drawing.Save(dlg.FileName);
            SetFile(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось сохранить", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenFile()
    {
        using var dlg = new OpenFileDialog { Filter = FileFilter };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _drawing.Load(dlg.FileName);
            _history.Reset();
            _selected = null;
            SetFile(dlg.FileName);
            _canvas.Invalidate();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось открыть файл", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetFile(string path)
    {
        _filePath = path;
        Text = $"Paint — {Path.GetFileName(path)}";
    }
}
