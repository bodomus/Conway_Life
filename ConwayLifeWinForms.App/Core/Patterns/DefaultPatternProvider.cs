using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Patterns;

public sealed class DefaultPatternProvider : IPatternProvider
{
    private readonly IReadOnlyList<LifePattern> _patterns;
    private readonly IReadOnlyList<string> _loadErrors;

    public DefaultPatternProvider()
    {
        RleParser parser = new();
        List<LifePattern> patterns = [];
        List<string> errors = [];

        foreach ((string fileName, PatternCategory category) in PatternDefinitions.All)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Patterns", fileName);

            try
            {
                string rle = File.ReadAllText(path);
                string name = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');
                patterns.Add(parser.Parse(name, category, rle));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                errors.Add($"{fileName}: {ex.Message}");
            }
        }

        _patterns = patterns
            .OrderBy(static pattern => pattern.Category)
            .ThenBy(static pattern => pattern.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _loadErrors = errors;
    }

    public IReadOnlyList<LifePattern> GetPatterns() => _patterns;

    public IReadOnlyList<string> GetLoadErrors() => _loadErrors;

    public LifePattern GetByName(string name) =>
        _patterns.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Pattern '{name}' not found.");

    private static class PatternDefinitions
    {
        public static IReadOnlyList<(string FileName, PatternCategory Category)> All { get; } =
        [
            ("01_Block.rle", PatternCategory.StillLife),
            ("02_Beehive.rle", PatternCategory.StillLife),
            ("03_Loaf.rle", PatternCategory.StillLife),
            ("04_Boat.rle", PatternCategory.StillLife),
            ("05_Tub.rle", PatternCategory.StillLife),
            ("06_Pond.rle", PatternCategory.StillLife),
            ("07_Blinker.rle", PatternCategory.Oscillator),
            ("08_Toad.rle", PatternCategory.Oscillator),
            ("09_Beacon_1.rle", PatternCategory.Oscillator),
            ("10_Pulsar.rle", PatternCategory.Oscillator),
            ("11_Pentadecathlon.rle", PatternCategory.Oscillator),
            ("12_Glider.rle", PatternCategory.Spaceship),
            ("13_LWSS.rle", PatternCategory.Spaceship),
            ("14_MWSS.rle", PatternCategory.Spaceship),
            ("15_HWSS.rle", PatternCategory.Spaceship),
            ("16_R_pentomino.rle", PatternCategory.Methuselah),
            ("17_Die_hard.rle", PatternCategory.Methuselah),
            ("18_Acorn.rle", PatternCategory.Methuselah),
            ("19_Gosper_glider_gun.rle", PatternCategory.Gun),
            ("20_Queen_bee_shuttle.rle", PatternCategory.Gun)
        ];
    }
}
