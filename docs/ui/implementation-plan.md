# Plan implementacji UI

> **Status:** plan wykonawczy — wersja 0.1
> **Punkt odniesienia:** [Kontrakt UI](contract.md), [Architektura komponentów UI](component-architecture.md)

## Zasada realizacji

Interfejs powstaje pionowymi, działającymi przyrostami: kompozycja widoków, stan prezentacyjny, podłączenie do przypadków użycia, weryfikacja geometrii. Nie budujemy z góry pustych ekranów przyszłych modułów.

## Faza 1 — shell i biblioteka kampanii

**Status: wycofana.** Ekran biblioteki kampanii oraz logika domenowa, na której się opierał, zostały usunięte jako prototyp (2026-08-26) — zostaną zaprojektowane od nowa, gdy przyjdzie na to czas. Poniższy opis jest zachowany jako punkt odniesienia, nie jako aktualny stan.

- wspólne tokeny kolorów, typografii i bazowych wymiarów;
- `MainWindow` ograniczony do osadzenia `AppShellView`;
- hermetyczne komponenty topbara, globalnego sidebara, paska statusu;
- ekran biblioteki z listą o stałej wysokości wiersza;
- tworzenie kampanii w nakładce, bez zmiany geometrii workspace'u;
- odczyt/utworzenie/otwarcie kampanii podłączone do istniejących use case'ów;
- stabilny empty state i lokalny viewport listy.

Otwarcie kampanii nie przełącza jeszcze kontekstu shellu — potwierdza poprawność odczytu i przygotowuje granicę dla fazy 2.

## Faza 1.1 — stabilizacja wizualna

**Status: zaimplementowana.**

- jawne `PerMonitorV2` i profile `Small`/`Medium`/`Large`;
- lokalny font Alegreya dla sygnetu, tytułów, inicjałów;
- kuratorowany, przypięty do commita podzbiór Lucide z licencją;
- wektorowe ikony w stałych, wycentrowanych slotach;
- rozłączne stany hover/active sidebara, bez domyślnych powierzchni Fluent;
- uproszczone ramy topbara, sidebara, statusbara;
- biblioteka bez ramy obejmującej pusty viewport;
- wariant Wide master–detail zamiast rozciągania metadanych na całą szerokość.

## Faza 2 — nawigacja i trwały stan okna

- jawny router trzech kontekstów: globalny, kreator, workspace kampanii;
- deskryptory ekranów zamiast warunków rozproszonych po widokach;
- aktywne/niedostępne pozycje nawigacji, obsługa klawiatury;
- ✅ pełny i kompaktowy wariant sidebara — dedykowany przełącznik w stopce sidebara, zapisywany trwale;
- ✅ zapis i bezpieczne przywracanie profilu skali UI i wariantu sidebara (`AppSettingsStore`, `%LocalAppData%\DungeonApp\settings.json`) — zapis rozmiaru/pozycji okna pozostaje do zrobienia;
- pierwsze testy ViewModeli shellu i routingu.

## Faza 3 — kreator kampanii

- stabilny układ kroków: podstawy, ruleset i zawartość, moduły, podsumowanie;
- zapis szkicu od pierwszego kroku;
- stała strefa akcji `Wstecz`/`Dalej`;
- zarezerwowane miejsca walidacji, bez przesuwania formularza;
- filtrowanie kompatybilnych paczek i modułów przez `Application`.

## Faza 4 — workspace kampanii i dashboard sesji

- przełączenie topbara i sidebara na kontekst otwartej kampanii;
- dashboard w wariantach `Compact`/`Standard`/`Wide`;
- panele drużyny, czasu, zdarzeń, notatki, historii jako osobne komponenty;
- formalne polityki `FixedMetric`/`Bounded`/`FluidData`;
- wykorzystanie istniejących modułów zegara i harmonogramu bez reguł domenowych w Desktop.

## Faza 5 — komponenty gęstych danych i baza wiedzy

- generyczna rama panelu dopiero po ≥2 realnych zastosowaniach;
- tabele i rejestry o stałych wysokościach wierszy;
- statblock Entity i widok read-first;
- układ dokument–drzewo–inspektor bez globalnego ograniczenia szerokości workspace'u;
- rozwój lokalnego podzbioru ikon Lucide wyłącznie o symbole wymagane przez kolejne moduły.

## Bramka jakości każdego przyrostu

1. Rozwiązanie kompiluje się bez ostrzeżeń.
2. Testy istniejącego rdzenia pozostają zielone.
3. Bindingi są kompilowane i typowane.
4. Loading/empty/ready/error nie zmieniają zewnętrznej geometrii komponentu.
5. Ekran sprawdzony co najmniej w `1024 × 680`, `1280 × 800` i jednym szerokim wariancie.
6. Nowe surowe kolory i wspólne metryki nie są rozproszone po plikach widoków.
7. Komponent ma jasno określonego właściciela stanu, bez wywoływania infrastruktury z code-behind.

## Najbliższy krok

Faza 2 przed pełnym kreatorem. Router kontekstów i trwały stan okna są potrzebne zarówno kreatorowi, jak i workspace'owi kampanii — pominięcie tej granicy ponownie skupiłoby odpowiedzialności w jednym ViewModelu.
