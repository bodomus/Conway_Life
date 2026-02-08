using ConwayLifeWinForms.App.Core.Abstractions;

namespace ConwayLifeWinForms.App.Core.Timing;

public sealed class FormsTimerTickSource : ITickSource
{
    private readonly System.Windows.Forms.Timer _timer = new();

    public FormsTimerTickSource()
    {
        _timer.Interval = 100;
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public bool Enabled => _timer.Enabled;

    public int IntervalMs
    {
        get => _timer.Interval;
        set => _timer.Interval = Math.Max(1, value);
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose() => _timer.Dispose();
}
