namespace ConwayLifeWinForms.App.Application.Commands;

public sealed class DelegateUiCommand(
    string name,
    Action execute,
    Func<bool>? canExecute = null) : IUiCommand
{
    public string Name { get; } = name;

    public bool CanExecute() => canExecute?.Invoke() ?? true;

    public void Execute()
    {
        if (!CanExecute())
        {
            return;
        }

        execute();
    }
}
