using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Patterns;

public static class LifePatternFactory
{
    public static LifePattern CreateBlock() => new(
        "Block",
        [
            new CellPoint(0, 0),
            new CellPoint(1, 0),
            new CellPoint(0, 1),
            new CellPoint(1, 1)
        ]);

    public static LifePattern CreateBlinker() => new(
        "Blinker",
        [
            new CellPoint(0, 1),
            new CellPoint(1, 1),
            new CellPoint(2, 1)
        ]);

    public static LifePattern CreateGlider() => new(
        "Glider",
        [
            new CellPoint(1, 0),
            new CellPoint(2, 1),
            new CellPoint(0, 2),
            new CellPoint(1, 2),
            new CellPoint(2, 2)
        ]);

    public static LifePattern CreateToad() => new(
        "Toad",
        [
            new CellPoint(1, 0),
            new CellPoint(2, 0),
            new CellPoint(3, 0),
            new CellPoint(0, 1),
            new CellPoint(1, 1),
            new CellPoint(2, 1)
        ]);

    public static LifePattern CreateBeacon() => new(
        "Beacon",
        [
            new CellPoint(0, 0),
            new CellPoint(1, 0),
            new CellPoint(0, 1),
            new CellPoint(1, 1),
            new CellPoint(2, 2),
            new CellPoint(3, 2),
            new CellPoint(2, 3),
            new CellPoint(3, 3)
        ]);
}
