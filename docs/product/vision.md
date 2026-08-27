# Wizja i fundamenty projektu

> **Status:** żywy dokument fazy zerowej — wersja 0.1

## Definicja produktu

Desktopowa aplikacja dla Mistrza Gry, która utrzymuje spójny stan kampanii i automatyzuje księgowość oraz konsekwencje reguł. Gra przy stole pozostaje tradycyjna: rozmowa, papierowe karty, fizyczne kości.

## Problem

Tradycyjne TTRPG wymaga ręcznego liczenia i pamiętania stanu świata (zasoby, ekwipunek, obrażenia, efekty, czas, NPC-e, konsekwencje decyzji). Statyczne notatki nie są aktywnym modelem świata — nie stosują reguł ani nie zmieniają stanu same z siebie.

Aplikacja ma przejąć pamięć, liczenie i śledzenie stanu. Nie zastępuje wyobraźni, rozmowy ani odpowiedzialności MG-a.

## Granica produktu

**Jest:** prywatnym panelem pracy MG-a, źródłem prawdy o stanie kampanii, silnikiem deterministycznego i wyjaśnialnego stosowania reguł, aplikacją lokalną i dyskretną przy stole.

**Nie jest:** VTT ani stołem online dla graczy, aplikacją wymagającą od graczy własnych urządzeń/kont, grą komputerową ani symulatorem fizyki, bazą tekstowych notatek, arbitrem narracji zastępującym MG-a.

## Zasady projektowe

1. **Mechanika wspiera narrację** — automatyzacja usuwa uciążliwe liczenie, nie decyzje i tempo.
2. **Stan świata ma przyczynę** — zmiana wynika z działania, upływu czasu, rzutu lub jawnej decyzji MG-a.
3. **System jest wyjaśnialny** — MG odtwarza, co, kiedy i na jakiej podstawie zmieniło stan.
4. **MG pozostaje właścicielem świata** — ręczne korekty są dozwolone, ale jako zapisane zdarzenia, nie ukryte omijanie reguł.
5. **Dokładność nie może utrudniać sesji** — interfejs szybki, czytelny, odporny na pomyłki.
6. **Brak sztucznego skalowania jako domyślnej zasady** — przygotowanie i ryzyko graczy mają realne konsekwencje.

Model zmiany stanu:

```text
stan świata + upływ czasu + działania + wynik rzutu + reguły
    -> nowy stan świata + historia wyjaśniająca zmianę
```

Zakres symulacji poza aktywną sesją jest kontrolowany przez kampanię i ruleset — nie modelujemy całego świata w jednakowej szczegółowości.

## Core, ruleset, zawartość

| Poziom | Odpowiedzialność | Przykłady |
| --- | --- | --- |
| **Core aplikacji** | mechanizmy niezależne od mechaniki | kampania, moduły i ich aktywacja, kronika, zapisy, kanał ogłoszeń, assety |
| **Ruleset** | znaczenie statystyk i reguły świata | walka, odporności, magia, progresja, obliczanie obrażeń |
| **Paczka zawartości** | dane konkretnej kampanii/przygody | bestie, przedmioty, zaklęcia, NPC-e, kupcy, łupy, lokacje |

Tożsamość rzeczy w świecie **nie należy do core'u** — należy do modułu, który daną rzecz modeluje. Moduł drużyny nadaje i pilnuje tożsamości uczestnika, harmonogram — tożsamości zaplanowanego zdarzenia. Core zna wyłącznie tożsamość kampanii i modułu, bo te dwie są mu potrzebne do złożenia i zapisania kampanii. Wspólna klasa bazowa `Entity` pojawi się dopiero wtedy, gdy dwa moduły będą realnie potrzebowały tego samego, a nie z góry.

Core nie zna, czym jest miecz, jego obrażenia ani zasady rzadkości — to należy do rulesetu/paczki.

Pierwszy ruleset jest świadomie ograniczony i służy weryfikacji core'u zamiast projektowania uniwersalnej abstrakcji z góry.

## Model odpowiedzialności technicznych

```text
DungeonApp.Desktop     -> DungeonApp.Core
DungeonApp.Core.Tests  -> DungeonApp.Core
```

`Core` zawiera model świata, reguły, scenariusze użycia, porty i ich lokalne adaptery. Nie zależy od Avalonia — pilnuje tego test architektoniczny, nie dobra wola. `Desktop` zawiera Avalonia, MVVM, nawigację, composition root i zasoby wizualne.

Rozdzielenie jest zabiegiem higienicznym: chodzi o czystą strukturę zależności i testowalność bez okna, nie o hipotetyczną wymienność frontendu. Dlatego nie tworzymy abstrakcji, których jedynym uzasadnieniem byłaby wymienialność.

Osobny projekt `Infrastructure` wydzielimy dopiero pod konkretny wyzwalacz: druga implementacja portu, zależność NuGet, której domena nie powinna widzieć, albo persystencja z własnym cyklem życia (migracje, indeks w tle). Do tego czasu adaptery mieszkają w `Core/Persistence`, `System.IO` nie wychodzi poza ten katalog, a model domenowy nigdy nie jest modelem zapisu.

MVVM jest techniką organizacji wyłącznie warstwy Avalonia — ViewModele nie zawierają reguł kampanii. `Core` nie zna pojęcia „aktualnie otwarta kampania”; to stan shellu.

Zobacz [architekturę komponentów UI](../ui/component-architecture.md).

## Priorytety UX przy stole

- szybkie odczytanie stanu bez przekopywania notatek;
- mało kliknięć, dobre skróty klawiszowe;
- bezpieczne wprowadzanie i korygowanie wyników;
- historia zmian i odzyskiwanie się po błędzie;
- praca lokalna, bez sieci;
- brak presji, by gracze korzystali z aplikacji.

Pierwszy pionowy wycinek: ekran aktywnej sesji i przepływ zmiana czasu → działanie → konsekwencja → wyjaśnialna historia.

## Decyzje podjęte

- Aplikacja desktopowa, przede wszystkim dla MG-a.
- Stack: C# + Avalonia.
- Logika świata oddzielona od frontendu, testowalna bez Avalonia.
- Core nie zawiera na stałe konkretnych przedmiotów, stworzeń ani mechanik jednego systemu.
- Dwa projekty produkcyjne (`Core`, `Desktop`) plus projekt testowy; `Infrastructure` dopiero pod wyzwalacz.
- Stan kampanii zapisywany jako modularny snapshot w duchu save'a gry, nie event sourcing. Dziennik jest kroniką dla MG-a, nigdy źródłem prawdy do odtworzenia stanu.
- Moduły są wbudowane w aplikację i włączane per kampania. Bez zewnętrznych, dynamicznie ładowanych pluginów.
- Moduł jest właścicielem swojego stanu; stan nieznanego modułu jest zachowywany nietknięty, nie kasowany.
- Kampania zapisana jako katalog w duchu save'a gry: manifest, pliki stanu modułów, dziennik i układ biurka rozdzielone wg granic spójności, nie wg tematyki. Manifest i stany modułów commitują się razem i dzielą licznik generacji, który czyni rozerwany zapis wykrywalnym zamiast cichego. Backup to komplet jednej generacji.
- Komunikacja modułów: bezpośrednie, typowane wywołania dla zapytań i poleceń; zdarzenia wyłącznie do ogłaszania faktów dokonanych.
- Silnik liczy, MG zatwierdza, aplikacja nigdy nie blokuje. Blokada zarezerwowana dla operacji nieodwracalnych, nie dla reguł gry.
- Przedmiot w kampanii to referencja do definicji z paczki plus zamknięty zbiór nadpisań instancji. Warianty są definicjami, nie nadpisaniami.
- Rzut kością jest modułem bez stanu: wynik to fakt trafiający do kroniki, nie stan kampanii. Zapis rzutu (`k20`, `2k6+3`) zna kości i jeden płaski modyfikator — znaczenie modyfikatora należy do rulesetu i świadomie nie mieszka w module.
- Ruleset: słownik statystyk, schemat zawartości, tabele i prezentacja jako dane; procedury rozstrzygania jako kod. Pierwszy ruleset w całości w kodzie, obliczenia adresowane po nazwie — to zostawia drogę do przeniesienia ich do danych bez zmiany dla konsumentów.

## Decyzje otwarte

- Pierwszy ruleset: prototypowy czy zalążek autorskiego systemu?
- Zasięg symulacji czasu i zdarzeń poza aktywną sesją.
- Minimalny użyteczny pionowy wycinek aktywnej sesji.
- Szczegóły formatu paczek zawartości i ich wersjonowania.
- Eksport/podgląd/synchronizacja danych dla graczy — czy i kiedy?

## Kryterium oceny nowych funkcji

> Czy pomaga MG-owi utrzymać szybką, przejrzystą i wiarygodnie konsekwentną sesję, bez odbierania graczom tradycyjnego charakteru gry?

Brak wyraźnego „tak” — funkcję odkładamy, zawężamy albo uzasadniamy ponownie.
