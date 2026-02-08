using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Domain;

public sealed class LifeEngine : ILifeEngine
{
    private IGridState _grid;
    private readonly ILifeRules _rules;
    private readonly Random _random = new();

    public LifeEngine(int width, int height, ILifeRules rules)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
        }

        _rules = rules;
        _grid = new GridState(width, height);
    }

    public int Width => _grid.Width;

    public int Height => _grid.Height;

    public long Generation { get; private set; }

    public int AliveCount { get; private set; }

    public event EventHandler<StateChangedEventArgs>? StateChanged;

    public bool GetCell(int x, int y) => _grid.GetCell(x, y);

    public bool SetCell(int x, int y, bool alive)
    {
        bool changed = _grid.SetCell(x, y, alive);
        if (!changed)
        {
            return false;
        }

        AliveCount += alive ? 1 : -1;
        OnStateChanged(LifeChangeKind.Edit);
        return true;
    }

    public void Step()
    {
        GridState next = new(Width, Height);
        int nextAliveCount = 0;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                bool current = _grid.GetCell(x, y);
                int neighbors = CountAliveNeighbors(x, y);
                bool alive = _rules.NextState(current, neighbors);
                if (alive)
                {
                    next.SetCell(x, y, alive: true);
                    nextAliveCount++;
                }
            }
        }

        _grid = next;
        AliveCount = nextAliveCount;
        Generation++;
        OnStateChanged(LifeChangeKind.Step);
    }

    public void Clear()
    {
        _grid.Clear();
        AliveCount = 0;
        Generation = 0;
        OnStateChanged(LifeChangeKind.Clear);
    }

    public void Resize(int width, int height, bool clear)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");
        }

        _grid = _grid.Resize(width, height, clear);
        AliveCount = _grid.CountAlive();
        if (clear)
        {
            Generation = 0;
        }

        OnStateChanged(LifeChangeKind.Resize);
    }

    public void Randomize(double density)
    {
        density = Math.Clamp(density, 0d, 1d);

        _grid.Clear();
        AliveCount = 0;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_random.NextDouble() <= density)
                {
                    _grid.SetCell(x, y, alive: true);
                    AliveCount++;
                }
            }
        }

        Generation = 0;
        OnStateChanged(LifeChangeKind.Randomize);
    }
    public void PlacePattern(LifePattern pattern, int originX, int originY)
    {
        foreach (CellPoint point in pattern.AliveCells)
        {
            int x = originX + point.X;
            int y = originY + point.Y;

            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                continue;
            }

            if (_grid.SetCell(x, y, alive: true))
            {
                AliveCount++;
            }
        }

        OnStateChanged(LifeChangeKind.Pattern);
    }

    public LifeSnapshot CreateSnapshot()
    {
        CellPoint[] alive = [.. _grid.EnumerateAliveCells()];
        return new LifeSnapshot(Width, Height, Generation, alive);
    }

    public void LoadSnapshot(LifeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _grid = new GridState(snapshot.Width, snapshot.Height);
        foreach (CellPoint point in snapshot.AliveCells)
        {
            if (point.X < 0 || point.Y < 0 || point.X >= snapshot.Width || point.Y >= snapshot.Height)
            {
                continue;
            }

            _grid.SetCell(point.X, point.Y, alive: true);
        }

        AliveCount = _grid.CountAlive();
        Generation = snapshot.Generation;
        OnStateChanged(LifeChangeKind.Load);
    }

    private int CountAliveNeighbors(int x, int y)
    {
        int alive = 0;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
                {
                    continue;
                }

                if (_grid.GetCell(nx, ny))
                {
                    alive++;
                }
            }
        }

        return alive;
    }

    private void OnStateChanged(LifeChangeKind kind) => StateChanged?.Invoke(this, new StateChangedEventArgs(kind));
}
