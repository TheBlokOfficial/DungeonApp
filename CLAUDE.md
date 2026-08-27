# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

DungeonApp is a desktop application (C# + Avalonia, .NET 10) for a tabletop RPG Game Master. It will keep a consistent campaign state and automate rule bookkeeping, while leaving the actual session traditional (conversation, paper character sheets, physical dice). Project docs (in Polish) live under `docs/` — see [docs/README.md](docs/README.md) for the index. Start with `docs/product/vision.md` for product vision.

**Current state:** the original domain/application/infrastructure prototype was deliberately removed. `DungeonApp.Core` is now being rebuilt from scratch in small increments — it currently holds campaign identity (`CampaignId`, `CampaignName`, `Campaign`), the `ICampaignRepository` port and the `CreateCampaign` use case. There is no persistence adapter yet; the only implementation of the port is a test double. Do not resurrect the old design from git history without checking with the user first.

## Commands

```bash
# Build the solution
dotnet build DungeonApp.sln

# Run the tests
dotnet test tests/DungeonApp.Core.Tests/DungeonApp.Core.Tests.csproj

# Run the desktop app
dotnet run --project src/DungeonApp.Desktop/DungeonApp.Desktop.csproj
```

Building the solution fails while the desktop app is running — Windows locks the produced
`.exe`. Close the app first, or build only the project you are working on.

`EnforceCodeStyleInBuild` is set in `Directory.Build.props`, so `dotnet build` also enforces `.editorconfig`-style code style diagnostics.

## Architecture

Two production projects: `DungeonApp.Core` (game logic, no Avalonia) and `DungeonApp.Desktop`
(Avalonia UI, MVVM, composition root, no business logic), plus `tests/DungeonApp.Core.Tests`.
`Desktop -> Core`; nothing points the other way. The split is hygiene, not future frontend
swappability — so do not add abstractions whose only justification is replaceability. A third
`Infrastructure` project is deliberately deferred until a concrete trigger appears; until then
persistence adapters belong in `Core/Persistence`, and `System.IO` must not leak outside it.
`CoreIndependenceTests` fails the build if `Core` ever references Avalonia.

`src/DungeonApp.Desktop/Shell/` holds the app shell (`AppShellView`/`ViewModel`, a 2-column/3-row grid), global sidebar, top bar, and status bar. `Settings/` persists UI settings (scale profile, sidebar variant) to `%LocalAppData%\DungeonApp\settings.json`. `ViewModels/` has shared MVVM primitives (`ObservableObject`, `AsyncCommand`) — no framework like CommunityToolkit.Mvvm is used. `Themes/` defines design tokens, built-in control styles, and icon resources as Avalonia resource dictionaries.
