using ConwayLifeWinForms.App.Core.Domain;
using ConwayLifeWinForms.App.Core.Models;
using ConwayLifeWinForms.App.Core.Patterns;

namespace ConwayLifeWinForms.Tests;

public sealed class RleParserTests
{
    [Fact]
    public void Parse_ValidRle_ReturnsAliveCells()
    {
        LifePattern pattern = RleParser.Parse(
            "Blinker",
            PatternCategory.Oscillator,
            "x = 3, y = 1, rule = B3/S23\n3o!");

        Assert.Equal("Blinker", pattern.Name);
        Assert.Equal(3, pattern.AliveCells.Count);
        Assert.Contains(new CellPoint(2, 0), pattern.AliveCells);
    }

    [Fact]
    public void Parse_InvalidBody_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => RleParser.Parse("Broken", PatternCategory.StillLife, "x = 1, y = 1\no"));
    }

    [Fact]
    public void PatternStampCommand_StampsInsideGridOnly()
    {
        LifeEngine engine = new(4, 4, new ClassicLifeRules());
        LifePattern pattern = new("Test", PatternCategory.StillLife, "B3/S23", [new CellPoint(0, 0), new CellPoint(1, 0), new CellPoint(5, 5)]);

        PatternStampCommand command = new(engine, pattern, 2, 2);
        int changed = command.Execute();

        Assert.Equal(2, changed);
        Assert.True(engine.GetCell(2, 2));
        Assert.True(engine.GetCell(3, 2));
    }
}
