using System.Text.Json;
using System.Text.Json.Serialization;
using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Models;

namespace ConwayLifeWinForms.App.Core.Storage;

public sealed class JsonStateStorage : IStateStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Save(string path, LifeSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(snapshot);

        PersistedLifeState persisted = new()
        {
            Width = snapshot.Width,
            Height = snapshot.Height,
            Generation = snapshot.Generation,
            AliveCells = [.. snapshot.AliveCells.Select(static c => new PersistedCell { X = c.X, Y = c.Y })]
        };

        string json = JsonSerializer.Serialize(persisted, SerializerOptions);
        File.WriteAllText(path, json);
    }

    public LifeSnapshot Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string json = File.ReadAllText(path);
        PersistedLifeState? persisted = JsonSerializer.Deserialize<PersistedLifeState>(json, SerializerOptions);
        if (persisted is null)
        {
            throw new InvalidDataException("Invalid JSON state file.");
        }

        Validate(persisted);
        CellPoint[] alive = [.. persisted.AliveCells.Select(static c => new CellPoint(c.X, c.Y))];
        return new LifeSnapshot(persisted.Width, persisted.Height, persisted.Generation, alive);
    }

    private static void Validate(PersistedLifeState persisted)
    {
        if (persisted.Width <= 0 || persisted.Height <= 0)
        {
            throw new InvalidDataException("Grid dimensions in JSON must be positive.");
        }

        foreach (PersistedCell cell in persisted.AliveCells)
        {
            if (cell.X < 0 || cell.Y < 0 || cell.X >= persisted.Width || cell.Y >= persisted.Height)
            {
                throw new InvalidDataException($"Cell ({cell.X},{cell.Y}) is outside persisted grid bounds.");
            }
        }
    }

    private sealed class PersistedLifeState
    {
        public required int Width { get; init; }

        public required int Height { get; init; }

        public required long Generation { get; init; }

        public required List<PersistedCell> AliveCells { get; init; }
    }

    private sealed class PersistedCell
    {
        public required int X { get; init; }

        public required int Y { get; init; }
    }
}
