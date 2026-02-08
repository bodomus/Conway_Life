# Architecture Overview

## High-level goals
- Keep Game of Life logic isolated from WinForms UI.
- Keep code testable with clear interfaces.
- Support runtime resize, pan/zoom, and direct editing efficiently.

## Layers
- **UI layer (`UI/`)**
  - `MainForm` handles controls, shortcuts, file dialogs, and user flow.
  - `LifeCanvasControl` renders cells and grid in `OnPaint`, handles pan/zoom/edit input.
- **Application layer (`Application/Commands/`)**
  - Command abstraction for UI actions and shortcut binding.
- **Core layer (`Core/`)**
  - `Abstractions/`: contracts (`ILifeEngine`, `IGridState`, `IStateStorage`, etc.).
  - `Domain/`: life engine, grid state, and classic rules strategy.
  - `Patterns/`: pattern provider and factory.
  - `Storage/`: JSON persistence.
  - `Timing/`: WinForms timer adapter implementing `ITickSource`.

## Design patterns used
- **Strategy**: `ILifeRules` with `ClassicLifeRules`.
- **Factory**: `LifePatternFactory` creates predefined patterns.
- **Command**: `IUiCommand` + `DelegateUiCommand` for action execution and hotkeys.
- **Observer**: `ILifeEngine.StateChanged` event updates UI status and redraw.

## Data flow
1. UI input (mouse/keyboard/buttons) triggers a command.
2. Command invokes engine/storage/pattern operations.
3. Engine updates state and raises `StateChanged`.
4. UI updates status bar and invalidates canvas.

## Performance notes
- Canvas is double-buffered.
- `OnPaint` draws only visible cells (viewport clipping).
- Grid lines are skipped at very low zoom for speed.
- Brush/pen instances are cached in control fields to avoid per-frame allocations.
