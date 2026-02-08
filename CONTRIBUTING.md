# Contributing Guide

Thanks for contributing to ConwayLifeWinForms.

## Development prerequisites
- .NET SDK 10.0.100 (see `global.json`)
- Windows OS (WinForms target)

## Setup
1. Clone the repository.
2. Run:
   - `dotnet restore ConwayLifeWinForms.slnx`
   - `dotnet build ConwayLifeWinForms.slnx -c Release`
   - `dotnet test ConwayLifeWinForms.slnx -c Release`

## Coding standards
- Keep UI and domain logic separated.
- Prefer constructor injection for dependencies.
- Keep nullable warnings at zero.
- Treat all warnings as errors.
- Follow `.editorconfig` formatting and analyzer rules.

## Pull requests
- Create focused PRs with clear rationale.
- Include tests for behavior changes.
- Update docs (`README.md`, `docs/architecture.md`, ADRs) when architecture or behavior changes.
- Ensure CI passes before requesting review.
