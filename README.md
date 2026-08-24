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

## Dokumentacja projektowa

- [Wizja i fundamenty](docs/00-wizja-i-fundamenty.md)
- [Modułowy rdzeń kampanii](docs/01-modularny-rdzen-kampanii.md)
- [Kierunek wizualny i stabilność UI](docs/02-kierunek-wizualny-i-stabilnosc-ui.md)
- [Architektura informacji i layout aplikacji](docs/03-architektura-informacji-i-layout.md)
- [Architektura komponentów UI](docs/04-architektura-komponentow-ui.md)
- [Kontrakt UI v1](docs/05-kontrakt-ui-v1.md)
- [Plan implementacji UI](docs/06-plan-implementacji-ui.md)
- [ADR-0001: Cztery projekty warstwowe](docs/adr/0001-cztery-projekty-warstwowe.md)
- [ADR-0002: Lokalny zapis kampanii v1](docs/adr/0002-lokalny-zapis-kampanii-v1.md)
