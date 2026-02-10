namespace ConwayLifeWinForms.App.Core.Models;

public sealed record LifePattern(string Name, PatternCategory Category, string Rule, IReadOnlyList<CellPoint> AliveCells);

public enum PatternCategory
{
    StillLife,
    Oscillator,
    Spaceship,
    Methuselah,
    Gun
}
