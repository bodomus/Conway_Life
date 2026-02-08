# ConwayLifeWinForms

Production-grade WinForms application for Conway's Game of Life.

## Tech stack
- .NET 10 (`net10.0-windows`)
- C# 12 (`LangVersion=12.0`)
- Windows Forms
- xUnit tests

## Features
- Built-in patterns: Block, Blinker, Glider, Toad, Beacon.
- Simulation control: Start/Pause, Step.
- Save/load state to/from JSON.
- Live status: generation, alive cells, speed, zoom, grid size.
- Runtime grid resize with width/height controls and optional clear.
- Pan/zoom canvas with fast manual rendering and double buffering.

## Grid resizing behavior
- Inputs: `Width` and `Height` (cells), plus `Apply` button.
- Range is fixed in code: **10..2000** for both dimensions.
- Default behavior keeps old content:
  - expanding adds dead cells;
  - shrinking crops to top-left region.
- Option `Clear on resize` resets all cells to dead.
- Viewport is stabilized to keep the visible center as close as possible after resize.

## Controls
### Mouse
- `LMB` click: toggle cell.
- `LMB` drag: draw alive cells.
- `RMB` drag: erase cells.
- `MMB` drag or `Space + LMB`: pan.
- `Ctrl + Mouse Wheel`: zoom in/out.

### Keyboard shortcuts
- `Space`: Start/Pause.
- `Enter`: Step.
- `Ctrl+S`: Save JSON.
- `Ctrl+O`: Load JSON.
- `Ctrl+N`: Clear field.
- `Ctrl+R`: Randomize (uses density control).
- `Ctrl+P`: Open pattern dropdown.
- `Esc`: Stop simulation and reset pan mode.
- `F1`: Help window with shortcuts.
- `Ctrl + +`, `Ctrl + -`, `Ctrl + 0`: zoom in/out/reset.
- Arrow keys: pan by small step.

## JSON format
Saved file format:

```json
{
  "width": 120,
  "height": 80,
  "generation": 42,
  "aliveCells": [
    { "x": 10, "y": 10 },
    { "x": 11, "y": 10 }
  ]
}
```

## Project structure
- `ConwayLifeWinForms.App` - main WinForms app and core/domain logic.
- `ConwayLifeWinForms.Tests` - xUnit tests.
- `docs/architecture.md` - architecture overview.
- `docs/decisions/0001-architecture.md` - architecture ADR.

## Build and run
```bash
dotnet restore ConwayLifeWinForms.slnx
dotnet build ConwayLifeWinForms.slnx -c Release
dotnet test ConwayLifeWinForms.slnx -c Release
dotnet run --project ConwayLifeWinForms.App/ConwayLifeWinForms.App.csproj
```

## CI
GitHub Actions workflow in `.github/workflows/ci.yml` restores, builds, and tests on Windows.

## License
MIT (`LICENSE`).
