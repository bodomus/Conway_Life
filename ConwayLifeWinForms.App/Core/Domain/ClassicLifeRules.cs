using ConwayLifeWinForms.App.Core.Abstractions;

namespace ConwayLifeWinForms.App.Core.Domain;

public sealed class ClassicLifeRules : ILifeRules
{
    public bool NextState(bool currentAlive, int aliveNeighbors) =>
        currentAlive switch
        {
            true when aliveNeighbors is 2 or 3 => true,
            false when aliveNeighbors == 3 => true,
            _ => false
        };
}
