# DungeonApp

Desktopowy panel Mistrza Gry do gry papierowej przy stole. Utrzymuje spójny stan kampanii i prowadzi
księgowość reguł, nie odbierając sesji jej stołowego charakteru.

> **Aplikacja liczy. Ty decydujesz, co policzyć.**

C#/.NET 10, Avalonia 12. Jedna maszyna, jeden użytkownik, bez sieci, bez kont.

## Uruchomienie, build, testy

```bash
dotnet run --project src/DungeonApp.App
```

```bash
dotnet build DungeonApp.sln
```

```bash
dotnet test DungeonApp.sln
```

## Struktura

```text
src/
  DungeonApp.Core/            domena bez UI: kampanie, stan, zapis, wpisy, paczki
  DungeonApp.Desktop/         Avalonia: powłoka, zakładki treści, biurko, motyw
  DungeonApp.Content.Dnd5e/   system D&D 5e — jedyne miejsce, które wie, czym jest potwór
  DungeonApp.App/             korzeń kompozycji i plik wykonywalny
tests/                        testy per projekt, testy granic, wspólne pomocniki, paczki wzorcowe
docs/                         architektura, rozstrzygnięcia, plan
```

## Dokumenty

- [CLAUDE.md](CLAUDE.md) — granica automatyzacji, sposób pracy, pułapki
- [docs/architecture.md](docs/architecture.md) — jak to jest zbudowane
- [docs/decisions.md](docs/decisions.md) — co przesądzone i czego nie robimy
- [docs/roadmap.md](docs/roadmap.md) — co dalej
