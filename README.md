# DungeonApp

Desktopowy panel Mistrza Gry do gry papierowej przy stole. Utrzymuje spójny stan kampanii
i prowadzi księgowość reguł, nie odbierając sesji jej stołowego charakteru.

C#/.NET 10, Avalonia. Jedna maszyna, jeden użytkownik, bez sieci, bez kont, bez synchronizacji.

---

## Problem

Asystenci Mistrza Gry dzielą się na dwa rodzaje i każdy zawodzi z przeciwnego powodu.

**Zbudowane pod jeden system** twardo kodują jego pojęcia. Działają dobrze dokładnie dopóty,
dopóki gra się w to, pod co powstały. Zmiana systemu — albo choćby poważniejszy homebrew —
oznacza czekanie na autora albo forka.

**W pełni konfigurowalne** oddają wszystko danym. Kończy się to tym, że użytkownik projektuje
własny interfejs w plikach tekstowych, a aplikacja jest słabym frameworkiem UI z gorszą obsługą
błędów niż przeglądarka.

DungeonApp szuka trzeciej drogi i przyjmuje asymetrię, która ją definiuje:

> **Aplikacja wie, czym jest potwór i jak go pokazać — bo to zostało zaprojektowane.
> Nie wie, czym jest D&D — bo wiedza o systemie mieszka w wymienialnej warstwie.**

Statblok jest zaprojektowanym układem, nie stosem sekcji poskładanym z pliku konfiguracyjnego.
Jednocześnie nic w rdzeniu aplikacji nie wie, że istnieje klasa pancerza — ta wiedza siedzi
w osobnym, wymienialnym projekcie. Dodanie nowego potwora to plik danych i żadnej przebudowy;
dodanie nowego *rodzaju* rzeczy to kod, świadomie i rzadko.

## Zasada rozstrzygająca

> **Aplikacja prowadzi księgowość. Nie egzekwuje reguł.**
>
> Aplikacja sumuje. Mistrz Gry decyduje o składnikach. Aplikacja nigdy nie podejmuje decyzji,
> której Mistrz Gry nie podjął.

Interpretacja zasad zostaje przy człowieku. Ta zasada rozstrzyga większość sporów o zakres
funkcji, a jej techniczne przełożenie — pięć zakazów, których złamanie zamienia tę aplikację
w silnik cRPG — jest w [CLAUDE.md](CLAUDE.md).

## Czym nie jest

Nie jest stołem wirtualnym do grania online, nie jest aplikacją dla graczy, nie jest generatorem
treści, nie jest silnikiem reguł konkretnego systemu. Cały interfejs jest zaprojektowany pod jedną
osobę prowadzącą jedną kampanię naraz, siedzącą przy stole z ludźmi.

Zakres obejmuje klasę systemów **„trad"**: dyskretne nazwane statystyki, rozstrzyganie akcji rzutem
z modyfikatorem, jakaś forma kolejności działania — D&D, Pathfinder, większość OSR, Call of Cthulhu,
Savage Worlds. Poza zakresem świadomie: gry bez kości, systemy oparte w rdzeniu na puli sukcesów,
gry karciane. To granica produktowa, nie techniczna.

---

## Dokumenty

Hierarchia jest ścisła — przy rozbieżności wygrywa dokument wyżej w tabeli.

| Dokument | Co rozstrzyga | Charakter |
|---|---|---|
| [CLAUDE.md](CLAUDE.md) | reguły wiążące — pięć zakazów | wygrywa ze wszystkim, w tym z kodem |
| [docs/architecture.md](docs/architecture.md) | projekt docelowy: warstwy, treść, nawigacja, przepływy | wygrywa z kodem — to jest cel |
| [docs/hld.md](docs/hld.md) | co faktycznie jest w kodzie dzisiaj | opisuje, nie rozstrzyga |
| [docs/decisions.md](docs/decisions.md) | kierunki odrzucone i dlaczego | nie rozstrzyga; chroni przed powrotem |
| [docs/rpg_systems.md](docs/rpg_systems.md) | pierwotna koncepcja, sprzed osadzenia w repozytorium | źródło historyczne, nie obowiązuje |

Dwie rzeczy warto wiedzieć, zanim się coś zaproponuje:

* **`decisions.md` czyta się przed propozycją, nie po.** Sporo naturalnych pomysłów zostało już raz
  rozważonych i odrzuconych z uzasadnieniem; dokument istnieje po to, żeby nie wracały co kilka
  miesięcy jako nowe.
* **`hld.md` nigdy nie wygrywa z kodem.** Jeśli się rozjeżdżają, prawdą jest kod, a dokument jest
  do poprawienia.

---

## Uruchomienie

```bash
dotnet run --project src/DungeonApp.Desktop
```

## Build i testy

```bash
dotnet build DungeonApp.sln
```

```bash
dotnet test tests/DungeonApp.Core.Tests/DungeonApp.Core.Tests.csproj
```

```bash
dotnet test tests/DungeonApp.Desktop.Tests/DungeonApp.Desktop.Tests.csproj
```

## Struktura

```text
src/
  DungeonApp.Core/      # Kampanie, stan, zdarzenia, zapis, treść. Bez Avalonii.
  DungeonApp.Desktop/   # Avalonia — powłoka, biurko, panele, motyw.
tests/
  DungeonApp.Core.Tests/
  DungeonApp.Desktop.Tests/
docs/
```

Podział na dwa projekty produkcyjne jest zabiegiem higienicznym: logika ma być odseparowana od
okna. Granicy pilnuje test architektoniczny, który odrzuca każdą referencję do Avalonii w `Core` —
bez niego separacja byłaby deklaracją w dokumentacji, a nie czymś wymuszonym przez build.

Docelowo dochodzi trzeci projekt produkcyjny — **zestaw treści** — niosący typy treści, ich widoki
i narzędzia biurka. To jedyne miejsce w całej aplikacji, w którym wolno wiedzieć, czym jest potwór.
Patrz [docs/architecture.md](docs/architecture.md), sekcja 3.

## Jak czytać projekt

1. `src/DungeonApp.Desktop/App.axaml.cs` — korzeń kompozycji, tam aplikacja składa wszystkie
   zależności ręcznie, bez kontenera DI.
2. `Shell/AppShellViewModel.cs` — przełącza między biblioteką kampanii a biurkiem otwartej kampanii.
3. `DungeonApp.Core`: `Campaign`, `CampaignDataBlocks`, `JsonCampaignRepository` — stan, jego zmiana
   i trwały zapis.
4. Licznik jako kompletny przekrój pionowy: `CounterTool` → `CounterPanelViewModel` →
   `CampaignSession` → `JsonCampaignRepository`.

**Licznik jest rusztowaniem, nie funkcją.** Powstał, żeby udowodnić, że cała ścieżka od gestu
w interfejsie do zapisu na dysku działa i daje się przetestować. Zniknie, gdy powstanie pierwsze
prawdziwe narzędzie biurka. Z samego kodu — dopracowanego i najlepiej pokrytego testami w repozytorium
— łatwo wyciągnąć przeciwny wniosek, więc to zdanie jest tu celowo.

Testy w `tests/` są zarazem wykonywalną specyfikacją opisanych zachowań.
