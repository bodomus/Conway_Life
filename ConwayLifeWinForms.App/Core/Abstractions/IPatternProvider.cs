using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface IPatternProvider
{
    IReadOnlyList<LifePattern> GetPatterns();

    IReadOnlyList<string> GetLoadErrors();

    LifePattern GetByName(string name);
}
