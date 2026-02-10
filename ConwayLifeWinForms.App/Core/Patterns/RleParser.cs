using System.Text.RegularExpressions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Patterns;

public sealed class RleParser
{
    private static readonly Regex HeaderRegex = new(@"x\s*=\s*(\d+)\s*,\s*y\s*=\s*(\d+)(?:\s*,\s*rule\s*=\s*([^,\s]+))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static LifePattern Parse(string name, PatternCategory category, string rleContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(rleContent);

        string[] lines = rleContent
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .Where(static line => !line.StartsWith('#'))
            .ToArray();

        if (lines.Length < 2)
        {
            throw new FormatException($"RLE '{name}' should contain header and body.");
        }

        Match match = HeaderRegex.Match(lines[0]);
        if (!match.Success)
        {
            throw new FormatException($"RLE '{name}' has invalid header. Expected: x = ..., y = ..., rule = ...");
        }

        int width = int.Parse(match.Groups[1].Value);
        int height = int.Parse(match.Groups[2].Value);
        string rule = match.Groups[3].Success ? match.Groups[3].Value : "B3/S23";

        string body = string.Concat(lines.Skip(1));
        List<CellPoint> aliveCells = ParseBody(name, body, width, height);

        return new LifePattern(name, category, rule, aliveCells);
    }

    private static List<CellPoint> ParseBody(string name, string body, int width, int height)
    {
        List<CellPoint> cells = [];
        int x = 0;
        int y = 0;
        int runLength = 0;

        foreach (char token in body)
        {
            if (char.IsDigit(token))
            {
                runLength = (runLength * 10) + (token - '0');
                continue;
            }

            int run = runLength == 0 ? 1 : runLength;
            runLength = 0;

            switch (token)
            {
                case 'b':
                case 'B':
                    x += run;
                    break;
                case 'o':
                case 'O':
                    for (int i = 0; i < run; i++)
                    {
                        cells.Add(new CellPoint(x + i, y));
                    }

                    x += run;
                    break;
                case '$':
                    y += run;
                    x = 0;
                    break;
                case '!':
                    ValidateBounds(name, cells, width, height);
                    return cells;
                case ' ': 
                case '\t':
                    break;
                default:
                    throw new FormatException($"RLE '{name}' contains unsupported token '{token}'.");
            }
        }

        throw new FormatException($"RLE '{name}' does not end with '!'.");
    }

    private static void ValidateBounds(string name, IEnumerable<CellPoint> cells, int width, int height)
    {
        foreach (CellPoint cell in cells)
        {
            if (cell.X < 0 || cell.Y < 0 || cell.X >= width || cell.Y >= height)
            {
                throw new FormatException($"RLE '{name}' has alive cell ({cell.X},{cell.Y}) outside x={width}, y={height}.");
            }
        }
    }
}
