using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface ILifeEngine : ILifeGrid
{
    long Generation { get; }

    int AliveCount { get; }

    event EventHandler<StateChangedEventArgs>? StateChanged;

    bool GetCell(int x, int y);

    void Step();

    void Clear();

    void Resize(int width, int height, bool clear);

    void Randomize(double density);

    void PlacePattern(LifePattern pattern, int originX, int originY);

    LifeSnapshot CreateSnapshot();

    void LoadSnapshot(LifeSnapshot snapshot);
}

public sealed class StateChangedEventArgs(LifeChangeKind changeKind) : EventArgs
{
    public LifeChangeKind ChangeKind { get; } = changeKind;
}

public enum LifeChangeKind
{
    Step,
    Edit,
    Resize,
    Clear,
    Load,
    Randomize,
    Pattern
}
