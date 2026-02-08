namespace ConwayLifeWinForms.App.Core.Models;

public sealed record LifePattern(string Name, IReadOnlyList<CellPoint> AliveCells);
