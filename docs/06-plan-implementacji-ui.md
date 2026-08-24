# Plan implementacji UI

> **Status:** plan wykonawczy  
> **Wersja:** 0.1  
> **Punkt odniesienia:** [Kontrakt UI v1](05-kontrakt-ui-v1.md) i [Architektura komponentów UI](04-architektura-komponentow-ui.md)

## Zasada realizacji

Interfejs powstaje pionowymi, działającymi przyrostami. Każdy przyrost obejmuje kompozycję widoków, stan prezentacyjny, podłączenie do właściwych przypadków użycia oraz weryfikację geometrii. Nie budujemy z góry pustych ekranów wszystkich przyszłych modułów.

## Faza 1 — shell i biblioteka kampanii

**Status: zaimplementowana.**

- wspólne tokeny kolorów, typografii i bazowych wymiarów;
- `MainWindow` ograniczony do osadzenia `AppShellView`;
- hermetyczne komponenty topbara, globalnego sidebara i paska statusu;
- ekran biblioteki z listą o stałej wysokości wiersza;
- tworzenie kampanii w nakładce, bez zmiany geometrii workspace'u;
- odczyt, utworzenie i otwarcie kampanii podłączone do istniejących przypadków użycia;
- stabilny empty state i lokalny viewport listy.

Otwarcie kampanii nie przełącza jeszcze kontekstu shellu. Potwierdza poprawność odczytu i przygotowuje granicę dla następnej fazy.

## Faza 1.1 — stabilizacja wizualna

**Status: zaimplementowana.**

- jawne `PerMonitorV2` oraz profile `Small`, `Medium` i `Large` oparte na zestawach tokenów;
- lokalny font Alegreya dla sygnetu, tytułów i inicjałów;
- kuratorowany, przypięty do commita podzbiór Lucide wraz z licencją;
- wektorowe ikony w stałych, wycentrowanych slotach;
- rozłączne stany hover i active sidebara, bez domyślnych powierzchni Fluent;
- uproszczone ramy topbara, sidebara i statusbara;
- biblioteka bez ramy obejmującej pusty viewport;
- wariant Wide master–detail zamiast rozciągania metadanych na całą szerokość monitora.

## Faza 2 — nawigacja i trwały stan okna

- jawny router trzech kontekstów: globalny, kreator, workspace kampanii;
- deskryptory ekranów zamiast warunków rozproszonych po widokach;
- aktywne i niedostępne pozycje nawigacji oraz obsługa klawiatury;
- pełny i kompaktowy wariant sidebara;
- zapis i bezpieczne przywracanie rozmiaru, pozycji oraz stanu okna;
- pierwsze testy ViewModeli shellu i routingu.

## Faza 3 — kreator kampanii

- stabilny układ kroków: podstawy, ruleset i zawartość, moduły, podsumowanie;
- zapis szkicu od pierwszego kroku;
- stała strefa akcji `Wstecz` i `Dalej`;
- zarezerwowane miejsca walidacji bez przesuwania formularza;
- filtrowanie kompatybilnych paczek i modułów przez warstwę Application.

## Faza 4 — workspace kampanii i dashboard sesji

- przełączenie topbara i sidebara na kontekst otwartej kampanii;
- dashboard w wariantach `Compact`, `Standard` i `Wide`;
- panele drużyny, czasu, zdarzeń, notatki oraz historii jako osobne komponenty;
- formalne polityki `FixedMetric`, `Bounded` i `FluidData`;
- wykorzystanie istniejących modułów zegara i harmonogramu bez reguł domenowych w Desktop.

## Faza 5 — komponenty gęstych danych i baza wiedzy

- generyczna rama panelu dopiero po potwierdzeniu co najmniej dwóch realnych zastosowań;
- tabele i rejestry o stałych wysokościach wierszy;
- statblock Entity oraz widok read-first;
- układ dokument–drzewo–inspektor bez globalnego ograniczenia szerokości workspace'u;
- rozwój lokalnego podzbioru ikon Lucide wyłącznie o symbole wymagane przez kolejne moduły.

## Bramka jakości każdego przyrostu

Przyrost nie jest zakończony, dopóki:

1. rozwiązanie nie kompiluje się bez ostrzeżeń;
2. testy istniejącego rdzenia pozostają zielone;
3. bindingi są kompilowane i typowane;
4. loading, empty, ready i error nie zmieniają zewnętrznej geometrii komponentu;
5. ekran został sprawdzony co najmniej w `1024 × 680`, `1280 × 800` i jednym szerokim wariancie;
6. nowe surowe kolory i wspólne metryki nie są rozproszone po plikach widoków;
7. komponent ma jasno określonego właściciela stanu i nie wywołuje infrastruktury z code-behind.

## Najbliższy krok

Przed rozpoczęciem pełnego kreatora należy zrealizować fazę 2. Router kontekstów i trwały stan okna są potrzebne zarówno kreatorowi, jak i workspace'owi kampanii; pominięcie tej granicy ponownie skupiłoby odpowiedzialności w jednym ViewModelu.
