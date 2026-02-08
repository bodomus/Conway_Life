using ConwayLifeWinForms.App.Core.Abstractions;

namespace ConwayLifeWinForms.App.UI;

public sealed class LifeCanvasControl(ILifeEngine engine) : Control
{
    public const int MinCellSize = 2;
    public const int MaxCellSize = 40;
    public const int DefaultCellSize = 10;

    private readonly ILifeEngine _engine = engine;
    private readonly SolidBrush _aliveBrush = new(Color.FromArgb(34, 139, 34));
    private readonly SolidBrush _deadBrush = new(Color.FromArgb(248, 250, 252));
    private readonly Pen _gridPen = new(Color.FromArgb(220, 225, 230));

    private double _viewOffsetX;
    private double _viewOffsetY;
    private bool _spacePressed;
    private bool _isPanning;
    private Point _panStartMouse;
    private double _panStartOffsetX;
    private double _panStartOffsetY;
    private bool _isDraggingEdit;
    private bool _dragSetAlive;
    private int _lastEditedX = -1;
    private int _lastEditedY = -1;

    public int CellSize { get; private set; } = DefaultCellSize;

    public event EventHandler? GridEdited;
    public event EventHandler? ZoomChanged;
    public event EventHandler? ViewChanged;

    public void SetSpacePressed(bool pressed) => _spacePressed = pressed;

    public void CancelPan()
    {
        _isPanning = false;
        _isDraggingEdit = false;
        Cursor = Cursors.Default;
    }

    public void ChangeZoom(int delta, Point? anchorScreen = null)
    {
        int next = Math.Clamp(CellSize + delta, MinCellSize, MaxCellSize);
        SetZoom(next, anchorScreen);
    }

    public void SetZoom(int zoom, Point? anchorScreen = null)
    {
        int clamped = Math.Clamp(zoom, MinCellSize, MaxCellSize);
        if (clamped == CellSize)
        {
            return;
        }

        Point anchor = anchorScreen ?? new Point(ClientSize.Width / 2, ClientSize.Height / 2);
        double worldX = (anchor.X + _viewOffsetX) / CellSize;
        double worldY = (anchor.Y + _viewOffsetY) / CellSize;

        CellSize = clamped;
        _viewOffsetX = (worldX * CellSize) - anchor.X;
        _viewOffsetY = (worldY * CellSize) - anchor.Y;

        ClampOffsets();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void ResetZoom() => SetZoom(DefaultCellSize);

    public void NudgeView(int dxCells, int dyCells)
    {
        _viewOffsetX += dxCells * CellSize;
        _viewOffsetY += dyCells * CellSize;
        ClampOffsets();
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public (double X, double Y) GetViewportCenterWorld()
    {
        double centerX = (_viewOffsetX + (ClientSize.Width / 2d)) / CellSize;
        double centerY = (_viewOffsetY + (ClientSize.Height / 2d)) / CellSize;
        return (centerX, centerY);
    }

    public Point GetViewportCenterCell()
    {
        (double x, double y) = GetViewportCenterWorld();
        return new Point((int)Math.Round(x), (int)Math.Round(y));
    }

    public void PreserveViewportAfterResize(int newWidth, int newHeight, (double X, double Y) oldCenter)
    {
        double centerX = Math.Clamp(oldCenter.X, 0d, Math.Max(0d, newWidth - 1d));
        double centerY = Math.Clamp(oldCenter.Y, 0d, Math.Max(0d, newHeight - 1d));

        _viewOffsetX = (centerX * CellSize) - (ClientSize.Width / 2d);
        _viewOffsetY = (centerY * CellSize) - (ClientSize.Height / 2d);

        ClampOffsets();
        ViewChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.Clear(_deadBrush.Color);

        int cell = CellSize;
        int width = _engine.Width;
        int height = _engine.Height;

        int startX = Math.Max(0, (int)Math.Floor(_viewOffsetX / cell));
        int startY = Math.Max(0, (int)Math.Floor(_viewOffsetY / cell));
        int endX = Math.Min(width - 1, (int)Math.Ceiling((_viewOffsetX + ClientSize.Width) / cell));
        int endY = Math.Min(height - 1, (int)Math.Ceiling((_viewOffsetY + ClientSize.Height) / cell));

        for (int y = startY; y <= endY; y++)
        {
            int sy = (int)(y * cell - _viewOffsetY);
            for (int x = startX; x <= endX; x++)
            {
                if (_engine.GetCell(x, y))
                {
                    int sx = (int)(x * cell - _viewOffsetX);
                    g.FillRectangle(_aliveBrush, sx, sy, cell, cell);
                }
            }
        }

        if (cell >= 6)
        {
            for (int x = startX; x <= endX + 1; x++)
            {
                int sx = (int)(x * cell - _viewOffsetX);
                g.DrawLine(_gridPen, sx, 0, sx, ClientSize.Height);
            }

            for (int y = startY; y <= endY + 1; y++)
            {
                int sy = (int)(y * cell - _viewOffsetY);
                g.DrawLine(_gridPen, 0, sy, ClientSize.Width, sy);
            }
        }
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) == Keys.Control)
        {
            int step = e.Delta > 0 ? 2 : -2;
            ChangeZoom(step, e.Location);
            return;
        }

        base.OnMouseWheel(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();

        bool panGesture = e.Button == MouseButtons.Middle || (_spacePressed && e.Button == MouseButtons.Left);
        if (panGesture)
        {
            BeginPan(e.Location);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            if (TryScreenToCell(e.Location, out int x, out int y))
            {
                bool current = _engine.GetCell(x, y);
                _engine.SetCell(x, y, !current);
                GridEdited?.Invoke(this, EventArgs.Empty);
                _isDraggingEdit = true;
                _dragSetAlive = true;
                _lastEditedX = x;
                _lastEditedY = y;
                Invalidate();
            }

            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            _isDraggingEdit = true;
            _dragSetAlive = false;
            if (TryScreenToCell(e.Location, out int x, out int y))
            {
                _engine.SetCell(x, y, alive: false);
                _lastEditedX = x;
                _lastEditedY = y;
                GridEdited?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isPanning)
        {
            _viewOffsetX = _panStartOffsetX - (e.X - _panStartMouse.X);
            _viewOffsetY = _panStartOffsetY - (e.Y - _panStartMouse.Y);
            ClampOffsets();
            ViewChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }

        if (!_isDraggingEdit)
        {
            return;
        }

        if (!TryScreenToCell(e.Location, out int x, out int y))
        {
            return;
        }

        if (x == _lastEditedX && y == _lastEditedY)
        {
            return;
        }

        _lastEditedX = x;
        _lastEditedY = y;

        _engine.SetCell(x, y, _dragSetAlive);
        GridEdited?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isPanning = false;
        _isDraggingEdit = false;
        Cursor = Cursors.Default;
        base.OnMouseUp(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ClampOffsets();
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        if (keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right)
        {
            return true;
        }

        return base.IsInputKey(keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _aliveBrush.Dispose();
            _deadBrush.Dispose();
            _gridPen.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BeginPan(Point mouse)
    {
        _isPanning = true;
        _panStartMouse = mouse;
        _panStartOffsetX = _viewOffsetX;
        _panStartOffsetY = _viewOffsetY;
        Cursor = Cursors.SizeAll;
    }

    private bool TryScreenToCell(Point point, out int x, out int y)
    {
        x = (int)Math.Floor((point.X + _viewOffsetX) / CellSize);
        y = (int)Math.Floor((point.Y + _viewOffsetY) / CellSize);

        if (x < 0 || y < 0 || x >= _engine.Width || y >= _engine.Height)
        {
            return false;
        }

        return true;
    }

    private void ClampOffsets()
    {
        double maxX = Math.Max(0, (_engine.Width * CellSize) - ClientSize.Width);
        double maxY = Math.Max(0, (_engine.Height * CellSize) - ClientSize.Height);

        _viewOffsetX = Math.Clamp(_viewOffsetX, 0, maxX);
        _viewOffsetY = Math.Clamp(_viewOffsetY, 0, maxY);
    }
}
