# DungeonApp

Desktopowy panel Mistrza Gry do gry papierowej przy stole. Utrzymuje spójny stan kampanii
i prowadzi księgowość reguł, nie odbierając sesji jej stołowego charakteru.

C#/.NET 10, Avalonia. Jedna maszyna, jeden użytkownik, bez sieci, bez kont, bez synchronizacji.

> **Aplikacja liczy. Ty decydujesz, co policzyć.**
>
> Interpretacja zasad zostaje przy człowieku. Techniczne przełożenie tej zasady — pięć zakazów,
> których złamanie zamienia tę aplikację w silnik cRPG — jest w [CLAUDE.md](CLAUDE.md).

Asystenci Mistrza Gry dzielą się na dwa gatunki i oba zawodzą z przeciwnych stron: napisane pod
jeden system twardo kodują jego pojęcia, a w pełni konfigurowalne każą użytkownikowi projektować
własny interfejs w plikach tekstowych. DungeonApp szuka trzeciej drogi i przyjmuje asymetrię, która
ją definiuje: **aplikacja wie, czym jest potwór i jak go pokazać — bo to zostało zaprojektowane;
nie wie, czym jest D&D — bo wiedza o systemie mieszka w wymienialnej warstwie.**

Nie jest stołem wirtualnym do grania online, aplikacją dla graczy, generatorem treści ani silnikiem
reguł konkretnego systemu.

Pełne wyjaśnienie — czym to jest, jak działa i dlaczego tak — jest w części I
[dokumentu architektury](docs/architecture.md).

## Uruchomienie

```bash
dotnet run --project src/DungeonApp.App
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
  DungeonApp.Core/            # Kampanie, stan, zdarzenia, zapis, treść. Bez Avalonii.
  DungeonApp.Desktop/         # Avalonia — powłoka, biurko, panele, motyw.
  DungeonApp.Content.Dnd5e/   # Zestaw treści D&D 5e — typy treści, karty, narzędzia biurka.
  DungeonApp.App/             # Korzeń kompozycji i plik wykonywalny.
tests/
  DungeonApp.Core.Tests/
  DungeonApp.Desktop.Tests/
docs/
```

Oddzielenie `Core` od `Desktop` jest zabiegiem higienicznym: logika ma być odseparowana od
okna. Granicy pilnuje test architektoniczny, który odrzuca każdą referencję do Avalonii w `Core` —
bez niego separacja byłaby deklaracją w dokumentacji, a nie czymś wymuszonym przez build.

**Zestaw treści**, `DungeonApp.Content.Dnd5e`, już istnieje i niesie
typy treści, ich widoki i narzędzia biurka. To jedyne miejsce w aplikacji, w którym wolno wiedzieć,
czym jest potwór. Ponieważ powłoce nie wolno znać żadnego zestawu po imieniu, korzeń kompozycji
przeniósł się do osobnego projektu wykonywalnego, `DungeonApp.App` — to on jako jedyny wymienia
zestawy z nazwy.

Od czego zacząć czytanie kodu — [docs/code-map.md](docs/code-map.md).

## Dokumenty

Pięć dokumentów, każdy odpowiada na jedno pytanie. Przy rozbieżności wygrywa ten wyżej.

| Dokument | Odpowiada na pytanie | Kiedy po niego sięgnąć |
|---|---|---|
| [CLAUDE.md](CLAUDE.md) | Czego nigdy nie wolno złamać? | zawsze; wygrywa nawet z kodem |
| [docs/architecture.md](docs/architecture.md) | Czym to jest, jak jest zbudowane i dlaczego tak? | żeby się rozeznać (część I) albo coś zmienić (część II–III) |
| [docs/code-map.md](docs/code-map.md) | Co jest w kodzie dzisiaj i gdzie co leży? | gdy szukasz konkretnego miejsca |
| [docs/decisions.md](docs/decisions.md) | Co już odrzuciliśmy i dlaczego? | **zanim** zaproponujesz zmianę architektury |
| [docs/tasks.md](docs/tasks.md) | Co jest do zrobienia dalej? | gdy szukasz następnego kroku |

Dwie rzeczy, które oszczędzają najwięcej czasu:

* **`decisions.md` czyta się przed propozycją, nie po.** Sporo naturalnych pomysłów zostało już raz
  rozważonych i odrzuconych z uzasadnieniem — kontekstowy sidebar, dziedziczenie szablonów, skrypty
  w paczkach, wpisy lokalne dla kampanii. Dokument istnieje po to, żeby nie wracały co kilka
  miesięcy jako nowe.
* **`code-map.md` nigdy nie wygrywa z kodem.** On i `architecture.md` rozjeżdżają się **celowo** —
  jeden opisuje stan, drugi cel.
