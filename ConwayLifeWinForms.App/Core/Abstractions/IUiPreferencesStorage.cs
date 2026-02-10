namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface IUiPreferencesStorage
{
    UiPreferences Load();

    void Save(UiPreferences preferences);
}

public sealed record UiPreferences(int PatternPanelWidth = 320);
