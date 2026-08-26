# DungeonApp

Desktopowa aplikacja dla Mistrza Gry, która utrzymuje spójny stan kampanii i automatyzuje księgowość reguł, bez odbierania sesji jej tradycyjnego, stołowego charakteru.

## Struktura solution

```text
src/
  DungeonApp.Desktop/   # Aplikacja Avalonia — shell, sidebar, ustawienia, motyw
docs/                    # Ustalenia projektowe
```

Logika domenowa (kampanie, moduły, zegar, harmonogram, trwałość) została świadomie
usunięta na tym etapie jako prototyp — zostanie napisana od nowa, gdy przyjdzie na to
czas.

## Dokumentacja projektowa

Zobacz [docs/README.md](docs/README.md) po pełny indeks. Struktura:

```text
docs/
  product/  # wizja i fundamenty produktu
  ui/       # kierunek wizualny, IA, architektura komponentów, kontrakt, plan implementacji
```
