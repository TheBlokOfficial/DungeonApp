# DungeonApp

Desktopowa aplikacja dla Mistrza Gry, która utrzymuje spójny stan kampanii i automatyzuje księgowość reguł, bez odbierania sesji jej tradycyjnego, stołowego charakteru.

## Struktura solution

```text
src/
  DungeonApp.Domain/          # Model świata oraz reguły niezależne od UI
  DungeonApp.Application/     # Scenariusze użycia i porty aplikacji
  DungeonApp.Infrastructure/  # Pliki, serializacja, assety i implementacje portów
  DungeonApp.Desktop/         # Aplikacja Avalonia — do dodania przez Rider
tests/
  DungeonApp.Domain.Tests/    # Testy reguł domenowych
docs/                         # Ustalenia projektowe
```

## Zależności

```text
Desktop -> Application -> Domain
Desktop -> Infrastructure -> Application
Tests -> Domain
```

Szczegółową wizję i granice projektu opisuje [dokument fazy zerowej](docs/00-wizja-i-fundamenty.md).
