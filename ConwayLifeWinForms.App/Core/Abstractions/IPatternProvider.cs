using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Abstractions;

public interface IPatternProvider
{
    IReadOnlyList<LifePattern> GetPatterns();

    LifePattern GetByName(string name);
}
