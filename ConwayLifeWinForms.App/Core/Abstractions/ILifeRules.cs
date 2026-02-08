namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface ILifeRules
{
    bool NextState(bool currentAlive, int aliveNeighbors);
}
