namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface ILifeGrid
{
    int Width { get; }

    int Height { get; }

    bool SetCell(int x, int y, bool alive);
}
