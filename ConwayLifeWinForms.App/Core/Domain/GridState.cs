using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Domain;

public sealed class GridState(int width, int height) : IGridState
{
    private readonly bool[,] _cells = new bool[width, height];

    public int Width { get; } = width;

    public int Height { get; } = height;

    public bool GetCell(int x, int y)
    {
        ValidateBounds(x, y);
        return _cells[x, y];
    }

    public bool SetCell(int x, int y, bool alive)
    {
        ValidateBounds(x, y);
        bool changed = _cells[x, y] != alive;
        _cells[x, y] = alive;
        return changed;
    }

    public int CountAlive()
    {
        int count = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[x, y])
                {
                    count++;
                }
            }
        }

        return count;
    }

    public IEnumerable<CellPoint> EnumerateAliveCells()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_cells[x, y])
                {
                    yield return new CellPoint(x, y);
                }
            }
        }
    }

    public void Clear()
    {
        Array.Clear(_cells);
    }

    public IGridState Clone()
    {
        GridState clone = new(Width, Height);
        Array.Copy(_cells, clone._cells, _cells.Length);
        return clone;
    }

    public IGridState Resize(int newWidth, int newHeight, bool clear)
    {
        GridState resized = new(newWidth, newHeight);
        if (clear)
        {
            return resized;
        }

        int copyWidth = Math.Min(Width, newWidth);
        int copyHeight = Math.Min(Height, newHeight);

        for (int y = 0; y < copyHeight; y++)
        {
            for (int x = 0; x < copyWidth; x++)
            {
                resized._cells[x, y] = _cells[x, y];
            }
        }

        return resized;
    }

    private void ValidateBounds(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        if (x >= Width || y >= Height)
        {
            throw new ArgumentOutOfRangeException($"Cell ({x},{y}) is outside {Width}x{Height}.");
        }
    }
}
