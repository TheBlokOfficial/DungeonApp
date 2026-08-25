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
| **Core aplikacji** | mechanizmy niezależne od mechaniki | kampania, czas, tożsamość encji, historia zdarzeń, zapisy, assety, ładowanie modułów |
| **Ruleset** | znaczenie statystyk i reguły świata | walka, odporności, magia, progresja, obliczanie obrażeń |
| **Paczka zawartości** | dane konkretnej kampanii/przygody | bestie, przedmioty, zaklęcia, NPC-e, kupcy, łupy, lokacje |

Core może wprowadzać pojęcia typu `Entity` (trwała tożsamość obiektu w świecie) jako koncept architektoniczny, nie przesądzoną klasę bazową. Core nie zna, czym jest miecz, jego obrażenia ani zasady rzadkości — to należy do rulesetu/paczki.

Pierwszy ruleset jest świadomie ograniczony i służy weryfikacji core'u zamiast projektowania uniwersalnej abstrakcji z góry.

## Model odpowiedzialności technicznych

```text
Frontend (Avalonia) -> Application -> Domain
Infrastructure -> implementuje porty wymagane przez Application/Domain
```

`Application` i `Domain` nie zależą od Avalonia. UI wywołuje scenariusze użycia, nie wykonuje decyzji domenowych. MVVM jest techniką organizacji wyłącznie warstwy Avalonia — ViewModele nie zawierają reguł kampanii.

Zobacz [ADR-0001](../architecture/adr/0001-cztery-projekty-warstwowe.md) i [architekturę komponentów UI](../ui/component-architecture.md).

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

## Decyzje otwarte

- Pierwszy ruleset: prototypowy czy zalążek autorskiego systemu?
- Zasięg symulacji czasu i zdarzeń poza aktywną sesją.
- Minimalny użyteczny pionowy wycinek aktywnej sesji.
- Format save'ów, paczek zawartości i wersjonowania danych.
- Które elementy rulesetu są danymi, a które rozszerzeniami w kodzie?
- Eksport/podgląd/synchronizacja danych dla graczy — czy i kiedy?

## Kryterium oceny nowych funkcji

> Czy pomaga MG-owi utrzymać szybką, przejrzystą i wiarygodnie konsekwentną sesję, bez odbierania graczom tradycyjnego charakteru gry?

Brak wyraźnego „tak” — funkcję odkładamy, zawężamy albo uzasadniamy ponownie.
