using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Patterns;

public sealed class PatternStampCommand(ILifeGrid grid, LifePattern pattern, int originX, int originY)
{
    public int Execute()
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(pattern);

        int changedCells = 0;
        foreach (CellPoint point in pattern.AliveCells)
        {
            int x = originX + point.X;
            int y = originY + point.Y;

            if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height)
            {
                continue;
            }

            if (grid.SetCell(x, y, alive: true))
            {
                changedCells++;
            }
        }

        return changedCells;
    }
}
