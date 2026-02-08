namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface ITickSource : IDisposable
{
    event EventHandler? Tick;

    bool Enabled { get; }

    int IntervalMs { get; set; }

    void Start();

    void Stop();
}
