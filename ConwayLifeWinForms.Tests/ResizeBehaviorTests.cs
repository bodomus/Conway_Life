using ConwayLifeWinForms.App.Core.Domain;

namespace ConwayLifeWinForms.Tests;

public sealed class ResizeBehaviorTests
{
    [Fact]
    public void Resize_Expand_KeepsExistingCells()
    {
        LifeEngine engine = new(5, 5, new ClassicLifeRules());
        engine.SetCell(1, 1, true);
        engine.SetCell(4, 4, true);

        engine.Resize(8, 7, clear: false);

        Assert.True(engine.GetCell(1, 1));
        Assert.True(engine.GetCell(4, 4));
        Assert.Equal(2, engine.AliveCount);
        Assert.Equal(8, engine.Width);
        Assert.Equal(7, engine.Height);
    }

    [Fact]
    public void Resize_Shrink_CropsOutsideCells()
    {
        LifeEngine engine = new(6, 6, new ClassicLifeRules());
        engine.SetCell(1, 1, true);
        engine.SetCell(5, 5, true);

        engine.Resize(3, 3, clear: false);

        Assert.True(engine.GetCell(1, 1));
        Assert.Equal(1, engine.AliveCount);
        Assert.Equal(3, engine.Width);
        Assert.Equal(3, engine.Height);
    }
}
