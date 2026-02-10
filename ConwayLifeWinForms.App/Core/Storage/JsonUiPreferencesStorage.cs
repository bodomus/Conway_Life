using System.Text.Json;
using ConwayLifeWinForms.App.Core.Abstractions;

namespace ConwayLifeWinForms.App.Core.Storage;

public sealed class JsonUiPreferencesStorage : IUiPreferencesStorage
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ConwayLifeWinForms",
        "ui-preferences.json");

    public UiPreferences Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new UiPreferences();
            }

            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<UiPreferences>(json) ?? new UiPreferences();
        }
        catch
        {
            return new UiPreferences();
        }
    }

    public void Save(UiPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        string directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        string json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
