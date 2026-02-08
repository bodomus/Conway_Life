using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface IStateStorage
{
    void Save(string path, LifeSnapshot snapshot);

    LifeSnapshot Load(string path);
}
