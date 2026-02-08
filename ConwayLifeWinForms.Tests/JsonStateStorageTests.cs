using ConwayLifeWinForms.App.Core.Domain;
using ConwayLifeWinForms.App.Core.Storage;

namespace ConwayLifeWinForms.Tests;

public sealed class JsonStateStorageTests
{
    [Fact]
    public void SaveLoadJson_PreservesCellsAndGeneration()
    {
        LifeEngine engine = new(12, 9, new ClassicLifeRules());
        engine.SetCell(1, 1, true);
        engine.SetCell(5, 2, true);
        engine.Step();

        JsonStateStorage storage = new();
        string path = Path.Combine(Path.GetTempPath(), $"life-{Guid.NewGuid():N}.json");

        try
        {
            storage.Save(path, engine.CreateSnapshot());
            var loaded = storage.Load(path);

            Assert.Equal(engine.Width, loaded.Width);
            Assert.Equal(engine.Height, loaded.Height);
            Assert.Equal(engine.Generation, loaded.Generation);
            Assert.Equal(engine.AliveCount, loaded.AliveCells.Count);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
