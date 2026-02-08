using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface IGridState
{
    int Width { get; }

    int Height { get; }

    bool GetCell(int x, int y);

    bool SetCell(int x, int y, bool alive);

    int CountAlive();

    IEnumerable<CellPoint> EnumerateAliveCells();

    void Clear();

    IGridState Clone();

    IGridState Resize(int newWidth, int newHeight, bool clear);
}
