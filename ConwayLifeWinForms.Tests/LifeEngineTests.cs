using ConwayLifeWinForms.App.Core.Domain;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.Tests;

public sealed class LifeEngineTests
{
    [Fact]
    public void Block_StillLife_RemainsUnchangedAfterStep()
    {
        LifeEngine engine = new(8, 8, new ClassicLifeRules());
        engine.PlacePattern(new LifePattern("Block", [new(2, 2), new(3, 2), new(2, 3), new(3, 3)]), 0, 0);

        engine.Step();

        Assert.True(engine.GetCell(2, 2));
        Assert.True(engine.GetCell(3, 2));
        Assert.True(engine.GetCell(2, 3));
        Assert.True(engine.GetCell(3, 3));
        Assert.Equal(4, engine.AliveCount);
        Assert.Equal(1, engine.Generation);
    }

    [Fact]
    public void Blinker_OscillatesBetweenHorizontalAndVertical()
    {
        LifeEngine engine = new(10, 10, new ClassicLifeRules());
        engine.PlacePattern(new LifePattern("Blinker", [new(1, 2), new(2, 2), new(3, 2)]), 0, 0);

        engine.Step();

        Assert.True(engine.GetCell(2, 1));
        Assert.True(engine.GetCell(2, 2));
        Assert.True(engine.GetCell(2, 3));
        Assert.False(engine.GetCell(1, 2));
        Assert.False(engine.GetCell(3, 2));

        engine.Step();

        Assert.True(engine.GetCell(1, 2));
        Assert.True(engine.GetCell(2, 2));
        Assert.True(engine.GetCell(3, 2));
        Assert.Equal(2, engine.Generation);
    }

    [Fact]
    public void Glider_ShiftsByOneCellDiagonalAfterFourSteps()
    {
        LifeEngine engine = new(20, 20, new ClassicLifeRules());
        engine.PlacePattern(new LifePattern("Glider", [new(1, 0), new(2, 1), new(0, 2), new(1, 2), new(2, 2)]), 1, 1);

        for (int i = 0; i < 4; i++)
        {
            engine.Step();
        }

        CellPoint[] expected =
        [
            new(3, 2),
            new(4, 3),
            new(2, 4),
            new(3, 4),
            new(4, 4)
        ];

        foreach (CellPoint point in expected)
        {
            Assert.True(engine.GetCell(point.X, point.Y));
        }

        Assert.Equal(5, engine.AliveCount);
    }
}
