using ConwayLifeWinForms.App.Application.Commands;
using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;
using Microsoft.Extensions.Logging;

namespace ConwayLifeWinForms.App.UI;

public sealed class MainForm : Form
{
    private const int MinGridSize = 10;
    private const int MaxGridSize = 2000;
    private const string PatternDragFormat = "application/x-conway-life-pattern";

    private readonly ILifeEngine _engine;
    private readonly IStateStorage _stateStorage;
    private readonly IUiPreferencesStorage _uiPreferencesStorage;
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

    private readonly NumericUpDown _speedInput = new() { Minimum = 1, Maximum = 60, Value = 10, Width = 60 };
    private readonly NumericUpDown _randomDensityInput = new() { Minimum = 1, Maximum = 100, Value = 25, Width = 60 };
    private readonly NumericUpDown _widthInput = new() { Minimum = MinGridSize, Maximum = MaxGridSize, Width = 70 };
    private readonly NumericUpDown _heightInput = new() { Minimum = MinGridSize, Maximum = MaxGridSize, Width = 70 };
    private readonly CheckBox _clearOnResize = new() { Text = "Clear on resize", AutoSize = true };

    private readonly SplitContainer _splitContainer = new() { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2 };
    private readonly ComboBox _patternCategoryFilter = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ListBox _patternListBox = new() { Dock = DockStyle.Fill, DisplayMember = nameof(LifePattern.Name) };

    private readonly Dictionary<string, IUiCommand> _commands = [];
    private readonly ILogger<MainForm> _log;

    public MainForm(
        ILifeEngine engine,
        IStateStorage stateStorage,
        IUiPreferencesStorage uiPreferencesStorage,
        IPatternProvider patternProvider,
        ITickSource tickSource,
        ILogger<MainForm> log)
    {
        _engine = engine;
        _stateStorage = stateStorage;
        _uiPreferencesStorage = uiPreferencesStorage;
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
        _canvas.AllowDrop = true;

        _widthInput.Value = _engine.Width;
        _heightInput.Value = _engine.Height;

        InitializeLayout();
        InitializePatternPanel();
        ApplySavedSplitWidth();
        WireEvents();
        ConfigureTimer();
        CreateCommands();
        UpdateStatus();

        ShowPatternLoadErrors();
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
        SaveSplitWidth();
        _tickSource.Dispose();
        base.OnFormClosed(e);
    }

    private void InitializeLayout()
    {
        FlowLayoutPanel topBar = new() { Dock = DockStyle.Top, Height = 74, Padding = new Padding(8), WrapContents = true, AutoSize = false };

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
        topBar.Controls.Add(new Label { Text = "Width", AutoSize = true, Padding = new Padding(6, 8, 2, 0) });
        topBar.Controls.Add(_widthInput);
        topBar.Controls.Add(new Label { Text = "Height", AutoSize = true, Padding = new Padding(6, 8, 2, 0) });
        topBar.Controls.Add(_heightInput);
        topBar.Controls.Add(_clearOnResize);
        topBar.Controls.Add(_applySizeButton);

        StatusStrip statusStrip = new();
        statusStrip.Items.Add(_statusLabel);

        Panel workspacePanel = new() { Dock = DockStyle.Fill };
        workspacePanel.Controls.Add(_splitContainer);
        workspacePanel.Controls.Add(topBar);

        _splitContainer.Panel1.Controls.Add(_canvas);

        Controls.Add(workspacePanel);
        Controls.Add(statusStrip);
    }

    private void InitializePatternPanel()
    {
        _splitContainer.SplitterWidth = 6;
        _splitContainer.Panel2MinSize = 220;

        Label title = new() { Dock = DockStyle.Top, Text = "Паттерны", Font = new Font(Font, FontStyle.Bold), Height = 26, Padding = new Padding(6, 6, 6, 0) };
        Label hint = new() { Dock = DockStyle.Bottom, Height = 34, Text = "Перетащите паттерн на поле (якорь: верхний левый).", Padding = new Padding(6, 6, 6, 6) };

        _patternCategoryFilter.Items.AddRange(["All", .. Enum.GetNames<PatternCategory>()]);
        _patternCategoryFilter.SelectedIndex = 0;

        _splitContainer.Panel2.Controls.Add(_patternListBox);
        _splitContainer.Panel2.Controls.Add(hint);
        _splitContainer.Panel2.Controls.Add(_patternCategoryFilter);
        _splitContainer.Panel2.Controls.Add(title);

        RebindPatternList();
    }

    private void WireEvents()
    {
        _startPauseButton.Click += (_, _) => ExecuteCommand("start-pause");
        _stepButton.Click += (_, _) => ExecuteCommand("step");
        _newButton.Click += (_, _) => ExecuteCommand("new");
        _saveButton.Click += (_, _) => ExecuteCommand("save");
        _loadButton.Click += (_, _) => ExecuteCommand("load");
        _randomButton.Click += (_, _) => ExecuteCommand("random");
        _applySizeButton.Click += (_, _) => ExecuteCommand("resize");

        _speedInput.ValueChanged += (_, _) => ConfigureTimer();
        _engine.StateChanged += (_, _) => OnEngineStateChanged();

        _canvas.GridEdited += (_, _) => UpdateStatus();
        _canvas.ZoomChanged += (_, _) => UpdateStatus();
        _canvas.ViewChanged += (_, _) => UpdateStatus();

        _tickSource.Tick += (_, _) => _engine.Step();
        _splitContainer.SplitterMoved += (_, _) => SaveSplitWidth();

        _patternCategoryFilter.SelectedIndexChanged += (_, _) => RebindPatternList();
        _patternListBox.MouseDown += OnPatternListMouseDown;

        _canvas.DragEnter += OnCanvasDragEnter;
        _canvas.DragOver += OnCanvasDragOver;
        _canvas.DragDrop += OnCanvasDragDrop;

        KeyDown += (_, e) => { if (e.KeyCode == Keys.Space) { _canvas.SetSpacePressed(true); } };
        KeyUp += (_, e) => { if (e.KeyCode == Keys.Space) { _canvas.SetSpacePressed(false); } };
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
        using SaveFileDialog dialog = new() { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*", DefaultExt = "json", AddExtension = true, FileName = "life-state.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK) { return; }
        _stateStorage.Save(dialog.FileName, _engine.CreateSnapshot());
    }

    private void LoadFromJson()
    {
        using OpenFileDialog dialog = new() { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) { return; }

        LifeSnapshot snapshot = _stateStorage.Load(dialog.FileName);
        _engine.LoadSnapshot(snapshot);
        _widthInput.Value = Math.Clamp(snapshot.Width, MinGridSize, MaxGridSize);
        _heightInput.Value = Math.Clamp(snapshot.Height, MinGridSize, MaxGridSize);
        _canvas.PreserveViewportAfterResize(snapshot.Width, snapshot.Height, _canvas.GetViewportCenterWorld());
    }

    private void Randomize() => _engine.Randomize((double)_randomDensityInput.Value / 100d);

    private void ApplyResize()
    {
        int newWidth = (int)_widthInput.Value;
        int newHeight = (int)_heightInput.Value;
        bool clear = _clearOnResize.Checked;
        (double x, double y) centerBefore = _canvas.GetViewportCenterWorld();
        _engine.Resize(newWidth, newHeight, clear);
        _canvas.PreserveViewportAfterResize(newWidth, newHeight, centerBefore);
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
            + "Esc - Stop simulation and cancel pan mode\n"
            + "F1 - This help\n"
            + "Ctrl++ / Ctrl+- / Ctrl+0 - Zoom in/out/reset\n"
            + "Ctrl+Wheel - Zoom\n"
            + "Arrows - Pan viewport\n"
            + "MMB drag or Space+LMB - Pan\n"
            + "LMB - Toggle cell, LMB drag - draw alive, RMB drag - erase\n"
            + "DnD: drag pattern from side panel to stamp on grid.";

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

    private void RebindPatternList()
    {
        IEnumerable<LifePattern> patterns = _patternProvider.GetPatterns();
        if (_patternCategoryFilter.SelectedItem is string categoryName
            && !string.Equals(categoryName, "All", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse(categoryName, out PatternCategory category))
        {
            patterns = patterns.Where(pattern => pattern.Category == category);
        }

        _patternListBox.DataSource = patterns.ToList();
    }

    private void OnPatternListMouseDown(object? sender, MouseEventArgs e)
    {
        int index = _patternListBox.IndexFromPoint(e.Location);
        if (index < 0)
        {
            return;
        }

        _patternListBox.SelectedIndex = index;
        if (_patternListBox.SelectedItem is LifePattern selected)
        {
            DataObject data = new();
            data.SetData(PatternDragFormat, selected);
            _patternListBox.DoDragDrop(data, DragDropEffects.Copy);
        }
    }

    private void OnCanvasDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(PatternDragFormat) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnCanvasDragOver(object? sender, DragEventArgs e)
    {
        if (_tickSource.Enabled)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        if (e.Data?.GetDataPresent(PatternDragFormat) == true)
        {
            e.Effect = DragDropEffects.Copy;
            return;
        }

        e.Effect = DragDropEffects.None;
    }

    private void OnCanvasDragDrop(object? sender, DragEventArgs e)
    {
        if (_tickSource.Enabled)
        {
            _statusLabel.Text = "Drop ignored while simulation is running.";
            return;
        }

        if (e.Data?.GetData(PatternDragFormat) is not LifePattern pattern)
        {
            return;
        }

        if (!_canvas.TryGetCellFromScreenPoint(new Point(e.X, e.Y), out Point cell))
        {
            return;
        }

        _engine.PlacePattern(pattern, cell.X, cell.Y);
        _canvas.Focus();
    }

    private void ShowPatternLoadErrors()
    {
        IReadOnlyList<string> errors = _patternProvider.GetLoadErrors();
        if (errors.Count == 0)
        {
            return;
        }

        string message = "Некоторые RLE-паттерны не были загружены:\n\n" + string.Join(Environment.NewLine, errors);
        MessageBox.Show(this, message, "Ошибка загрузки паттернов", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ApplySavedSplitWidth()
    {
        UiPreferences preferences = _uiPreferencesStorage.Load();
        int width = Math.Max(_splitContainer.Panel2MinSize, preferences.PatternPanelWidth);
        _splitContainer.SplitterDistance = Math.Max(100, ClientSize.Width - width);
    }

    private void SaveSplitWidth()
    {
        int panelWidth = _splitContainer.Width - _splitContainer.SplitterDistance;
        _uiPreferencesStorage.Save(new UiPreferences(panelWidth));
    }
}
