using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Patterns;

public sealed class DefaultPatternProvider : IPatternProvider
{
    private readonly IReadOnlyList<LifePattern> _patterns =
    [
        LifePatternFactory.CreateBlock(),
        LifePatternFactory.CreateBlinker(),
        LifePatternFactory.CreateGlider(),
        LifePatternFactory.CreateToad(),
        LifePatternFactory.CreateBeacon()
    ];

    public IReadOnlyList<LifePattern> GetPatterns() => _patterns;

    public LifePattern GetByName(string name) =>
        _patterns.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Pattern '{name}' not found.");
}
