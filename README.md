# DungeonApp

Desktopowy panel Mistrza Gry: utrzymuje spójny stan kampanii i prowadzi
księgowość reguł, nie odbierając sesji jej stołowego charakteru. Nie jest
stołem wirtualnym ani grą dla graczy — pełne granice produktu opisuje
[wizja](docs/vision.md).

C#/.NET 10, Avalonia. Jedna maszyna, jeden użytkownik, bez warstwy
sieciowej.

## Uruchomienie

```bash
dotnet run --project src/DungeonApp.Desktop
```

## Build i testy

```bash
dotnet build DungeonApp.sln
dotnet test tests/DungeonApp.Core.Tests/DungeonApp.Core.Tests.csproj
dotnet test tests/DungeonApp.Desktop.Tests/DungeonApp.Desktop.Tests.csproj
```

## Struktura

```text
src/
  DungeonApp.Core/      # Kampanie, moduły, dziennik, zapis. Bez Avalonia.
  DungeonApp.Desktop/   # Aplikacja Avalonia — shell, biurko, panele, motyw.
tests/
  DungeonApp.Core.Tests/
  DungeonApp.Desktop.Tests/
docs/
```

Podział na dwa projekty produkcyjne jest zabiegiem higienicznym: logika ma
być testowalna bez okna. Granicy pilnuje test architektoniczny, który
odrzuca każdą referencję do Avalonii w `Core`.

## Dokumentacja

| Dokument | Co zawiera |
|---|---|
| [wizja](docs/vision.md) | czym produkt jest, czego świadomie nie robi, co odłożone |
| [architektura](docs/architecture.md) | szwy i niezmienniki: co wolno, co jest egzekwowane |
| [mapa kodu](docs/code-map.md) | stan faktyczny zależności i przepływów, ze stemplem commita |
| [UI](docs/ui.md) | język wizualny, gdzie mieszkają liczby, reguły komponentowe |

`CLAUDE.md` opisuje styl i tryb pracy z agentem — nie jest dokumentacją
projektu.
