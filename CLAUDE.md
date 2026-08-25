# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

DungeonApp is a desktop application (C# + Avalonia, .NET 10) for a tabletop RPG Game Master. It keeps a consistent campaign state and automates rule bookkeeping, while leaving the actual session traditional (conversation, paper character sheets, physical dice). Project docs (in Polish) live under `docs/` — see [docs/README.md](docs/README.md) for the index. Start with `docs/product/vision.md` for product vision and `docs/architecture/campaign-core.md` for the campaign core design; ADRs are in `docs/architecture/adr/`.

## Commands

```bash
# Build the whole solution
dotnet build DungeonApp.sln

# Run all tests
dotnet test DungeonApp.sln

# Run a single test (by fully-qualified name filter)
dotnet test --filter "FullyQualifiedName~SchedulerModuleTests"

# Run the desktop app
dotnet run --project src/DungeonApp.Desktop/DungeonApp.Desktop.csproj
```

`EnforceCodeStyleInBuild` is set in `Directory.Build.props`, so `dotnet build` also enforces `.editorconfig`-style code style diagnostics.

## Architecture

Four-project layered solution (see `docs/architecture/adr/0001-cztery-projekty-warstwowe.md`), with a strict dependency direction:

```
Desktop -> Application -> Domain
Desktop -> Infrastructure -> Application
Infrastructure -> Domain
```

- `DungeonApp.Domain` — world model, rules, campaign modules, commands, domain events. No dependency on Avalonia or persistence.
- `DungeonApp.Application` — use cases (`*UseCase` classes), request/response DTOs, and the ports (interfaces) infrastructure must implement (e.g. `ICampaignRepository`).
- `DungeonApp.Infrastructure` — JSON persistence, filesystem, and adapters implementing Application/Domain ports.
- `DungeonApp.Desktop` — Avalonia UI, MVVM, navigation shell, composition root. ViewModels never contain campaign rules; they call into Application use cases.
- `DungeonApp.Domain.Tests` — xunit tests for domain rules, runnable without Avalonia or the filesystem.

Deliberately avoided (per ADR-0001) unless a real need appears: MediatR/custom command frameworks, generic repositories, one project per module, auto-mappers, heavy DI containers.

### Campaign module system (core of the domain)

`Campaign` (`src/DungeonApp.Domain/Campaigns/Campaign.cs`) is the aggregate root and a *local* (non-global, synchronous) event router — one command in, all resulting events processed in one pass, all-or-nothing commit:

```
GM command -> targeted ICampaignModule -> published domain events -> deterministic reactions from active modules -> joint commit of module state + history
```

Key points:
- `CampaignCommand` targets one `ModuleId` explicitly.
- `Campaign.Execute` works on working copies of all modules (`CreateWorkingCopy`); if any handler throws, nothing is committed and campaign state is untouched.
- A single command may produce at most 1000 events (`MaximumEventsPerCommand`), guarding against unintended pub/sub loops.
- Modules are optional and are enabled via the core command `EnableCampaignModule`, resolved through a registered `ICampaignModuleFactory` — the UI never constructs modules directly.
- A module declares dependencies via `ICampaignModuleDependencyDeclaration`; missing required modules fail validation (e.g. `core.scheduler` requires `core.clock`).
- At most one enabled module may implement `ICampaignTimeSource` (provides `CampaignTime`, an elapsed-time counter, not a calendar).

Existing modules, both under `src/DungeonApp.Domain/Campaigns/Modules/`:
- `Clock/` (`core.clock`) — tracks `CampaignTime`; `AdvanceWorldTime` command publishes `WorldTimeAdvanced`.
- `Scheduler/` (`core.scheduler`) — depends on `core.clock`; `ScheduleWorldEvent` command publishes `WorldEventScheduled`; reacts to `WorldTimeAdvanced` and publishes `ScheduledWorldEventDue` for events whose deadline has passed.

Each module has: a `*Module` class (domain state + command/event handling), a `*ModuleFactory` (creates fresh instances when enabled), and its own commands/events under that module's folder.

### Persistence (see `docs/architecture/adr/0002-lokalny-zapis-kampanii.md`)

`JsonCampaignRepository` (`src/DungeonApp.Infrastructure/Campaigns/Persistence/`) serializes a campaign to a single self-contained `campaign.json` (with a `formatVersion`), decoupling serialization from Domain via per-module adapters:
- `ICampaignModulePersistenceAdapter` — saves/restores one module's private state.
- `ICampaignEventPersistenceAdapter` — saves/restores one versioned event payload type (e.g. `core.clock.world-time-advanced.v1`).

Adding a new module means adding its own persistence adapters, not touching the campaign host or the general save format. Campaigns live one-per-directory (`<campaign-id>/campaign.json`, `assets/`, `backups/`) under the user's Documents folder; writes go to a temp file and then replace the target atomically.

### Desktop shell

`src/DungeonApp.Desktop/Shell/` holds the app shell (`AppShellView`/`ViewModel`), global sidebar, top bar, and status bar. `Features/` holds feature areas (currently `CampaignLibrary`). `ViewModels/` has shared MVVM primitives (`ObservableObject`, `AsyncCommand`) — no framework like CommunityToolkit.Mvvm is used. `Themes/` defines design tokens, built-in control styles, and icon resources as Avalonia resource dictionaries.
