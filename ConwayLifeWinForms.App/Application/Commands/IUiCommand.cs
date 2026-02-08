namespace ConwayLifeWinForms.App.Application.Commands;

public interface IUiCommand
{
    string Name { get; }

    bool CanExecute();

    void Execute();
}
