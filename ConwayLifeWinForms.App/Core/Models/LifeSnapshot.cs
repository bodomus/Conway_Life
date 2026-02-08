namespace ConwayLifeWinForms.App.Core.Models;

public sealed record LifeSnapshot(
    int Width,
    int Height,
    long Generation,
    IReadOnlyList<CellPoint> AliveCells);
