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
być odseparowana od okna. Granicy pilnuje test architektoniczny, który
odrzuca każdą referencję do Avalonii w `Core`.

## Dokumentacja

`CLAUDE.md` opisuje styl i tryb pracy z agentem — nie jest dokumentacją
projektu.
