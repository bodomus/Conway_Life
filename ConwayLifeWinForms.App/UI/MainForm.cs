using ConwayLifeWinForms.App.Application.Commands;
using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;
using Microsoft.Extensions.Logging;

namespace ConwayLifeWinForms.App.UI;

public sealed class MainForm : Form
{
    private const int MinGridSize = 10;
    private const int MaxGridSize = 2000;

    private readonly ILifeEngine _engine;
    private readonly IStateStorage _stateStorage;
    private readonly IPatternProvider _patternProvider;
    private readonly ITickSource _tickSource;

    private readonly LifeCanvasControl _canvas;
    private readonly ToolStripStatusLabel _statusLabel = new();

    private readonly Button _startPauseButton = new() { Text = "Start", Width = 80 };
    private readonly Button _stepButton = new() { Text = "Step", Width = 80 };
    private readonly Button _newButton = new() { Text = "New", Width = 80 };
    private readonly Button _saveButton = new() { Text = "Save", Width = 80 };
    private readonly Button _loadButton = new() { Text = "Load", Width = 80 };
    private readonly Button _randomButton = new() { Text = "Randomize", Width = 90 };
    private readonly Button _applySizeButton = new() { Text = "Apply", Width = 80 };
    private readonly Button _insertPatternButton = new() { Text = "Insert", Width = 80 };

    private readonly NumericUpDown _speedInput = new() { Minimum = 1, Maximum = 60, Value = 10, Width = 60 };
    private readonly NumericUpDown _randomDensityInput = new() { Minimum = 1, Maximum = 100, Value = 25, Width = 60 };
    private readonly NumericUpDown _widthInput = new() { Minimum = MinGridSize, Maximum = MaxGridSize, Width = 70 };
    private readonly NumericUpDown _heightInput = new() { Minimum = MinGridSize, Maximum = MaxGridSize, Width = 70 };
    private readonly CheckBox _clearOnResize = new() { Text = "Clear on resize", AutoSize = true };
    private readonly ComboBox _patternCombo = new() { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly Dictionary<string, IUiCommand> _commands = [];
    private readonly ILogger<MainForm> _log;
    public MainForm(ILifeEngine engine, IStateStorage stateStorage, IPatternProvider patternProvider, ITickSource tickSource, ILogger<MainForm> log)
    {
        _engine = engine;
        _stateStorage = stateStorage;
        _patternProvider = patternProvider;
        _tickSource = tickSource;
        _canvas = new LifeCanvasControl(_engine);
        _log = log;

        Text = "Conway's Game of Life";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        _canvas.Dock = DockStyle.Fill;
        _canvas.TabStop = true;

        _widthInput.Value = _engine.Width;
        _heightInput.Value = _engine.Height;

        _patternCombo.DisplayMember = nameof(LifePattern.Name);
        _patternCombo.DataSource = _patternProvider.GetPatterns().ToList();

        InitializeLayout();
        WireEvents();
        ConfigureTimer();
        CreateCommands();
        UpdateStatus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Control) == Keys.Control)
        {
            return keyData switch
            {
                Keys.Control | Keys.S => ExecuteCommand("save"),
                Keys.Control | Keys.O => ExecuteCommand("load"),
                Keys.Control | Keys.N => ExecuteCommand("new"),
                Keys.Control | Keys.R => ExecuteCommand("random"),
                Keys.Control | Keys.P => ExecuteCommand("pattern-select"),
                Keys.Control | Keys.Add => ExecuteCommand("zoom-in"),
                Keys.Control | Keys.Oemplus => ExecuteCommand("zoom-in"),
                Keys.Control | Keys.Subtract => ExecuteCommand("zoom-out"),
                Keys.Control | Keys.OemMinus => ExecuteCommand("zoom-out"),
                Keys.Control | Keys.D0 => ExecuteCommand("zoom-reset"),
                Keys.Control | Keys.NumPad0 => ExecuteCommand("zoom-reset"),
                _ => base.ProcessCmdKey(ref msg, keyData)
            };
        }

        return keyData switch
        {
            Keys.Space => ExecuteCommand("start-pause"),
            Keys.Enter => ExecuteCommand("step"),
            Keys.Escape => ExecuteCommand("escape"),
            Keys.F1 => ExecuteCommand("help"),
            Keys.Up => ExecuteCommand("pan-up"),
            Keys.Down => ExecuteCommand("pan-down"),
            Keys.Left => ExecuteCommand("pan-left"),
            Keys.Right => ExecuteCommand("pan-right"),
            _ => base.ProcessCmdKey(ref msg, keyData)
        };
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _tickSource.Dispose();
        base.OnFormClosed(e);
    }

    private void InitializeLayout()
    {
        FlowLayoutPanel topBar = new()
        {
            Dock = DockStyle.Top,
            Height = 74,
            Padding = new Padding(8),
            WrapContents = true,
            AutoSize = false
        };

        topBar.Controls.Add(_startPauseButton);
        topBar.Controls.Add(_stepButton);
        topBar.Controls.Add(_newButton);
        topBar.Controls.Add(_saveButton);
        topBar.Controls.Add(_loadButton);
        topBar.Controls.Add(new Label { Text = "Speed", AutoSize = true, Padding = new Padding(6, 8, 2, 0) });
        topBar.Controls.Add(_speedInput);
        topBar.Controls.Add(_randomButton);
        topBar.Controls.Add(new Label { Text = "Density %", AutoSize = true, Padding = new Padding(6, 8, 2, 0) });
        topBar.Controls.Add(_randomDensityInput);
        topBar.Controls.Add(_patternCombo);
        topBar.Controls.Add(_insertPatternButton);
        topBar.Controls.Add(new Label { Text = "Width", AutoSize = true, Padding = new Padding(6, 8, 2, 0) });
        topBar.Controls.Add(_widthInput);
        topBar.Controls.Add(new Label { Text = "Height", AutoSize = true, Padding = new Padding(6, 8, 2, 0) });
        topBar.Controls.Add(_heightInput);
        topBar.Controls.Add(_clearOnResize);
        topBar.Controls.Add(_applySizeButton);

        StatusStrip statusStrip = new();
        statusStrip.Items.Add(_statusLabel);

        Controls.Add(_canvas);
        Controls.Add(topBar);
        Controls.Add(statusStrip);
    }

    private void WireEvents()
    {
        _startPauseButton.Click += (_, _) => ExecuteCommand("start-pause");
        _stepButton.Click += (_, _) => ExecuteCommand("step");
        _newButton.Click += (_, _) => ExecuteCommand("new");
        _saveButton.Click += (_, _) => ExecuteCommand("save");
        _loadButton.Click += (_, _) => ExecuteCommand("load");
        _randomButton.Click += (_, _) => ExecuteCommand("random");
        _insertPatternButton.Click += (_, _) => ExecuteCommand("pattern-insert");
        _applySizeButton.Click += (_, _) => ExecuteCommand("resize");

        _speedInput.ValueChanged += (_, _) => ConfigureTimer();
        _engine.StateChanged += (_, _) => OnEngineStateChanged();

        _canvas.GridEdited += (_, _) => UpdateStatus();
        _canvas.ZoomChanged += (_, _) => UpdateStatus();
        _canvas.ViewChanged += (_, _) => UpdateStatus();

        _tickSource.Tick += (_, _) => _engine.Step();

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Space)
            {
                _canvas.SetSpacePressed(true);
            }
        };

        KeyUp += (_, e) =>
        {
            if (e.KeyCode == Keys.Space)
            {
                _canvas.SetSpacePressed(false);
            }
        };
    }
    private void CreateCommands()
    {
        _commands["start-pause"] = new DelegateUiCommand("StartPause", ToggleStartPause);
        _commands["step"] = new DelegateUiCommand("Step", () => _engine.Step());
        _commands["new"] = new DelegateUiCommand("New", () => _engine.Clear());
        _commands["save"] = new DelegateUiCommand("Save", SaveToJson);
        _commands["load"] = new DelegateUiCommand("Load", LoadFromJson);
        _commands["random"] = new DelegateUiCommand("Randomize", Randomize);
        _commands["resize"] = new DelegateUiCommand("Resize", ApplyResize);
        _commands["pattern-insert"] = new DelegateUiCommand("PatternInsert", InsertPatternAtViewportCenter);
        _commands["pattern-select"] = new DelegateUiCommand("PatternSelect", OpenPatternSelection);
        _commands["help"] = new DelegateUiCommand("Help", ShowHelp);
        _commands["escape"] = new DelegateUiCommand("Escape", EscapeAction);
        _commands["zoom-in"] = new DelegateUiCommand("ZoomIn", () => _canvas.ChangeZoom(+2));
        _commands["zoom-out"] = new DelegateUiCommand("ZoomOut", () => _canvas.ChangeZoom(-2));
        _commands["zoom-reset"] = new DelegateUiCommand("ZoomReset", _canvas.ResetZoom);
        _commands["pan-up"] = new DelegateUiCommand("PanUp", () => _canvas.NudgeView(0, -2));
        _commands["pan-down"] = new DelegateUiCommand("PanDown", () => _canvas.NudgeView(0, 2));
        _commands["pan-left"] = new DelegateUiCommand("PanLeft", () => _canvas.NudgeView(-2, 0));
        _commands["pan-right"] = new DelegateUiCommand("PanRight", () => _canvas.NudgeView(2, 0));
    }

    private bool ExecuteCommand(string key)
    {
        if (!_commands.TryGetValue(key, out IUiCommand? command))
        {
            return false;
        }

        command.Execute();
        return true;
    }

    private void ToggleStartPause()
    {
        if (_tickSource.Enabled)
        {
            _tickSource.Stop();
            _startPauseButton.Text = "Start";
        }
        else
        {
            _tickSource.Start();
            _startPauseButton.Text = "Pause";
        }

        UpdateStatus();
    }

    private void EscapeAction()
    {
        if (_tickSource.Enabled)
        {
            _tickSource.Stop();
            _startPauseButton.Text = "Start";
        }

        _canvas.CancelPan();
        UpdateStatus();
    }

    private void ConfigureTimer()
    {
        int speed = (int)_speedInput.Value;
        int interval = (int)Math.Round(1000d / speed, MidpointRounding.AwayFromZero);
        _tickSource.IntervalMs = Math.Max(1, interval);
        UpdateStatus();
    }

    private void SaveToJson()
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            AddExtension = true,
            FileName = "life-state.json"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _stateStorage.Save(dialog.FileName, _engine.CreateSnapshot());
    }

    private void LoadFromJson()
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        LifeSnapshot snapshot = _stateStorage.Load(dialog.FileName);
        _engine.LoadSnapshot(snapshot);
        _widthInput.Value = Math.Clamp(snapshot.Width, MinGridSize, MaxGridSize);
        _heightInput.Value = Math.Clamp(snapshot.Height, MinGridSize, MaxGridSize);
        _canvas.PreserveViewportAfterResize(snapshot.Width, snapshot.Height, _canvas.GetViewportCenterWorld());
    }

    private void Randomize()
    {
        double density = (double)_randomDensityInput.Value / 100d;
        _engine.Randomize(density);
    }

    private void ApplyResize()
    {
        int newWidth = (int)_widthInput.Value;
        int newHeight = (int)_heightInput.Value;
        bool clear = _clearOnResize.Checked;

        (double x, double y) centerBefore = _canvas.GetViewportCenterWorld();
        _engine.Resize(newWidth, newHeight, clear);
        _canvas.PreserveViewportAfterResize(newWidth, newHeight, centerBefore);
    }

    private void InsertPatternAtViewportCenter()
    {
        if (_patternCombo.SelectedItem is not LifePattern pattern)
        {
            return;
        }

        Point center = _canvas.GetViewportCenterCell();
        int minX = pattern.AliveCells.Min(static p => p.X);
        int minY = pattern.AliveCells.Min(static p => p.Y);
        int maxX = pattern.AliveCells.Max(static p => p.X);
        int maxY = pattern.AliveCells.Max(static p => p.Y);

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        int originX = center.X - (width / 2) - minX;
        int originY = center.Y - (height / 2) - minY;

        _engine.PlacePattern(pattern, originX, originY);
    }

    private void OpenPatternSelection()
    {
        _patternCombo.Focus();
        _patternCombo.DroppedDown = true;
    }

    private void ShowHelp()
    {
        const string helpText = "Shortcuts:\n"
            + "Space - Start/Pause\n"
            + "Enter - Step\n"
            + "Ctrl+S - Save JSON\n"
            + "Ctrl+O - Load JSON\n"
            + "Ctrl+N - New/Clear\n"
            + "Ctrl+R - Randomize\n"
            + "Ctrl+P - Pattern selection\n"
            + "Esc - Stop simulation and cancel pan mode\n"
            + "F1 - This help\n"
            + "Ctrl++ / Ctrl+- / Ctrl+0 - Zoom in/out/reset\n"
            + "Ctrl+Wheel - Zoom\n"
            + "Arrows - Pan viewport\n"
            + "MMB drag or Space+LMB - Pan\n"
            + "LMB - Toggle cell, LMB drag - draw alive, RMB drag - erase";

        MessageBox.Show(this, helpText, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OnEngineStateChanged()
    {
        _canvas.Invalidate();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        int speed = (int)_speedInput.Value;
        _statusLabel.Text =
            $"Generation: {_engine.Generation}, Alive: {_engine.AliveCount}, Speed: {speed} steps/s, "
            + $"Zoom: {_canvas.CellSize} px, Grid: {_engine.Width}x{_engine.Height}";
    }
}
