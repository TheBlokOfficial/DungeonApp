# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

DungeonApp is a desktop application (C# + Avalonia, .NET 10) for a tabletop RPG Game Master. It will keep a consistent campaign state and automate rule bookkeeping, while leaving the actual session traditional (conversation, paper character sheets, physical dice). Project docs (in Polish) live under `docs/` — see [docs/README.md](docs/README.md) for the index. Start with `docs/product/vision.md` for product vision.

**Current state:** the original domain/application/infrastructure prototype was deliberately removed. `DungeonApp.Core` is being rebuilt from scratch in small increments and now holds campaign identity, the campaign itself with its modules, journal and event bus, four built-in modules (`core.clock`, `core.scheduler`, `core.party`, `core.dice`), and the JSON store under `Core/Persistence`. Do not resurrect the old design from git history without checking with the user first.

A **module** is a built-in unit of campaign behaviour, switched on per campaign, that owns a scrap of state and nothing else's. It declares its dependencies in its manifest, is activated in topological order, records what it changed in the chronicle, and hands the store a plain state object without ever learning that JSON exists. Modules ask each other questions through direct typed calls and announce accomplished facts through `CampaignEvents`. New behaviour normally arrives as a module rather than as a widening of the core.

## Commands

```bash
# Build the solution
dotnet build DungeonApp.sln

# Run the tests
dotnet test tests/DungeonApp.Core.Tests/DungeonApp.Core.Tests.csproj
dotnet test tests/DungeonApp.Desktop.Tests/DungeonApp.Desktop.Tests.csproj

# Run the desktop app
dotnet run --project src/DungeonApp.Desktop/DungeonApp.Desktop.csproj
```

Building the solution fails while the desktop app is running — Windows locks the produced
`.exe`. Close the app first, or build to another output root:
`dotnet build DungeonApp.sln -p:BaseOutputPath=<some-temp-dir>/`, which still runs the XAML
compiler and compiled bindings and also works for `dotnet test`.

`EnforceCodeStyleInBuild` is set in `Directory.Build.props`, so `dotnet build` also enforces `.editorconfig`-style code style diagnostics.

## Architecture

Two production projects: `DungeonApp.Core` (game logic, no Avalonia) and `DungeonApp.Desktop`
(Avalonia UI, MVVM, composition root, no business logic), plus `tests/DungeonApp.Core.Tests` and `tests/DungeonApp.Desktop.Tests`.
`Desktop -> Core`; nothing points the other way. The split is hygiene, not future frontend
swappability — so do not add abstractions whose only justification is replaceability. A third
`Infrastructure` project is deliberately deferred until a concrete trigger appears; until then
persistence adapters belong in `Core/Persistence`, and `System.IO` must not leak outside it.
`CoreIndependenceTests` fails the build if `Core` ever references Avalonia.

`src/DungeonApp.Desktop/Shell/` holds the app shell (`AppShellView`/`ViewModel`, a 2-column/3-row grid), global sidebar, top bar, and status bar. `Settings/` persists UI settings (scale profile, sidebar variant) to `%LocalAppData%\DungeonApp\settings.json`. `ViewModels/` has shared MVVM primitives (`ObservableObject`, `AsyncCommand`) — no framework like CommunityToolkit.Mvvm is used. `Themes/` defines design tokens, built-in control styles, and icon resources as Avalonia resource dictionaries.
