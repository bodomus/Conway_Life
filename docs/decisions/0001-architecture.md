# ADR 0001: Layered WinForms architecture with explicit core interfaces

- **Status**: Accepted
- **Date**: 2026-02-08

## Context
The application must provide interactive Conway simulation in WinForms while staying maintainable and testable. Requirements include runtime resize, JSON persistence, presets, and multiple hotkeys.

## Decision
Adopt a layered architecture inside one WinForms project:
- Core/domain logic in `Core/*` with interfaces and pure logic classes.
- UI in `UI/*` focused on rendering and interaction.
- Command layer for actions used by buttons and hotkeys.
- Timer abstraction (`ITickSource`) for testability and decoupling.

Use specific patterns:
- Strategy (`ILifeRules`) for Life rule evolution.
- Factory (`LifePatternFactory`) for predefined patterns.
- Command (`IUiCommand`) for Start/Pause/Step/Clear/Resize/Load/Save/etc.
- Observer (`StateChanged` event) for UI synchronization.

## Consequences
### Positive
- Easier unit testing of core behavior.
- UI can evolve without touching rule engine internals.
- Hotkey and button actions reuse identical command paths.
- Future rule variants can be added by new `ILifeRules` implementations.

### Negative
- More files and abstractions compared to a single-form implementation.
- Slightly higher initial complexity.
