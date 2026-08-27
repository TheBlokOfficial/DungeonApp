# DungeonApp

Desktopowa aplikacja dla Mistrza Gry, która utrzymuje spójny stan kampanii i automatyzuje księgowość reguł, bez odbierania sesji jej tradycyjnego, stołowego charakteru.

## Struktura solution

```text
src/
  DungeonApp.Core/      # Logika gry — kampanie, reguły, moduły. Bez Avalonia.
  DungeonApp.Desktop/   # Aplikacja Avalonia — shell, sidebar, ustawienia, motyw
tests/
  DungeonApp.Core.Tests/
  DungeonApp.Desktop.Tests/
docs/                   # Ustalenia projektowe
```

Podział na dwa projekty produkcyjne jest zabiegiem higienicznym: logika gry ma być
testowalna bez okna. Granicy pilnuje test architektoniczny, który odrzuca każdą
referencję do Avalonia w `Core`.

Poprzednia warstwa domenowa była prototypem i została usunięta. `Core` powstaje od nowa,
przyrostami — patrz [wizja i fundamenty](docs/product/vision.md). Aktualny stan prac i to,
co jest następne, opisuje [Co dalej](docs/product/roadmap.md).

## Dokumentacja projektowa

Zobacz [docs/README.md](docs/README.md) po pełny indeks. Struktura:

```text
docs/
  product/  # wizja i fundamenty produktu, stan prac
  ui/       # kierunek wizualny, IA, architektura komponentów, kontrakt, plan implementacji
```
