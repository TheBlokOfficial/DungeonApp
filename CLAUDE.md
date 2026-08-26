# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

DungeonApp is a desktop application (C# + Avalonia, .NET 10) for a tabletop RPG Game Master. It will keep a consistent campaign state and automate rule bookkeeping, while leaving the actual session traditional (conversation, paper character sheets, physical dice). Project docs (in Polish) live under `docs/` — see [docs/README.md](docs/README.md) for the index. Start with `docs/product/vision.md` for product vision.

**Current state:** the domain/application/infrastructure layers (campaigns, modules, clock, scheduler, JSON persistence) were a prototype and have been deliberately removed to keep the repo lean. Only the Desktop shell (shell layout, sidebar, top bar, status bar, settings persistence, theme tokens) remains. The campaign/module system will be redesigned and rebuilt from scratch when that work is picked up again — do not resurrect the old design from git history without checking with the user first.

## Commands

```bash
# Build the solution
dotnet build DungeonApp.sln

# Run the desktop app
dotnet run --project src/DungeonApp.Desktop/DungeonApp.Desktop.csproj
```

`EnforceCodeStyleInBuild` is set in `Directory.Build.props`, so `dotnet build` also enforces `.editorconfig`-style code style diagnostics.

## Architecture

Single project today: `DungeonApp.Desktop` (Avalonia UI, MVVM, no business logic). When domain logic returns, expect it to live in its own project(s) again rather than inside Desktop — see the vision doc's stated separation of Frontend / Application / Domain.

`src/DungeonApp.Desktop/Shell/` holds the app shell (`AppShellView`/`ViewModel`, a 2-column/3-row grid), global sidebar, top bar, and status bar. `Settings/` persists UI settings (scale profile, sidebar variant) to `%LocalAppData%\DungeonApp\settings.json`. `ViewModels/` has shared MVVM primitives (`ObservableObject`, `AsyncCommand`) — no framework like CommunityToolkit.Mvvm is used. `Themes/` defines design tokens, built-in control styles, and icon resources as Avalonia resource dictionaries.
