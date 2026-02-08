# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-02-08
### Added
- Initial production-grade WinForms implementation of Conway's Game of Life.
- Custom double-buffered canvas with manual rendering, pan, zoom, and fast viewport clipping.
- Runtime grid resize with optional clear, expansion preservation, shrinking crop, and viewport stabilization.
- Built-in patterns: Block, Blinker, Glider, Toad, Beacon.
- Start/Pause, Step, randomize with density, load/save JSON, and status metrics.
- Command-based UI actions and keyboard shortcut mapping.
- Core architecture with interfaces and separated domain/UI responsibilities.
- xUnit test suite covering rules, serialization, and resize behavior.
- CI workflow for restore, build, and test on Windows.
