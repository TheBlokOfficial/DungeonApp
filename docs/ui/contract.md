# Kontrakt UI v1

> **Status:** zaakceptowany kontrakt implementacyjny — wersja 1.1 — 2026-08-24

Normatywne wartości geometrii, responsywności i zachowania shellu Desktop przed pierwszą fazą implementacji Avalonia. Jeżeli makieta, PoC albo lokalny styl XAML są sprzeczne z tym dokumentem, pierwszeństwo ma kontrakt. Nie zamraża zawartości przyszłych modułów — określa wspólny układ współrzędnych, w którym moduły muszą działać.

## Jednostki i skalowanie systemowe

Wszystkie wymiary w logicznych, niezależnych od DPI jednostkach Avalonia — nie pod fizyczne piksele monitora ani etykietę rozdzielczości. Breakpointy reagują na rzeczywisty logiczny rozmiar kontenera po uwzględnieniu skalowania systemu.

- layout respektuje skalowanie DPI systemu;
- Windows: jawny `PerMonitorV2`, Avalonia przelicza DIPs na piksele dla bieżącego monitora;
- nie mnożymy tokenów ręcznie przez `Screen.Scaling` (podwójne skalowanie);
- główna siatka: wielokrotności `4`;
- zaokrąglanie layoutu do granic pikseli urządzenia włączone;
- brak transformacji skalującej całe drzewo UI;
- większy monitor pokazuje więcej danych, nie większe kontrolki;
- ustawienie wielkości tekstu nie zwiększa samodzielnie zawartości w niezmienionym slocie.

## Okno aplikacji

| Parametr | Wartość v1 |
| --- | ---: |
| Rozmiar referencyjny obszaru klienta | `1280 × 800` |
| Minimalny obszar klienta | `1024 × 680` (`Small`/`Medium`); `1180 × 760` (`Large`) |
| Stan po pierwszym uruchomieniu | `1280 × 800`, ograniczony do dostępnego obszaru roboczego |
| Kolejne uruchomienia | ostatni zapisany rozmiar, pozycja, stan okna |
| Maksymalizacja globalna | niewymuszana |
| Aktywna sesja | projektowana głównie pod okno zmaksymalizowane |

Brak wariantu mobilnego/jednokolumnowego poniżej minimalnego rozmiaru. System nie przywraca zapisanego okna poza aktualnie dostępnym obszarem monitorów. Przyszły „Tryb sesji" (maksymalizacja + ograniczony chrome) nie jest częścią v1.

## Bazowa siatka i geometria

| Token | Wartość |
| --- | ---: |
| Jednostka bazowa | `4` |
| Odstęp mały | `4` |
| Odstęp standardowy | `8` |
| Odstęp między sekcjami | `16` |
| Padding workspace'u | `16` |
| Przerwa pomiędzy panelami | `8` |
| Grubość obramowania | `1` |
| Domyślny promień narożnika | `2` |
| Dopuszczalny zakres promienia | `0–4` |

Mocniejsza rama = inny token koloru, nie większa grubość. Brak ujemnych marginesów do korekty konstrukcji shellu.

## Shell

| Element | Wymiar |
| --- | ---: |
| Topbar | `52` wysokości |
| Sidebar pełny | `224` szerokości |
| Sidebar kompaktowy | `56` szerokości |
| Statusbar | `24` wysokości |
| Nagłówek workspace'u | `68` wysokości |
| Akcja topbara | komórka `52 × 52`, ikona `16 × 16` |
| Slot marki aplikacji | `32 × 32` |
| Znak `D` wewnątrz slotu | `24 × 24` |

Sekcja marki w topbarze odpowiada szerokości bieżącego wariantu sidebara — pionowa linia konstrukcji przechodzi przez całe okno. Topbar, sidebar, statusbar nie rosną po maksymalizacji; dodatkową przestrzeń przejmuje workspace.

## Sidebar

| Element | Wymiar |
| --- | ---: |
| Wiersz nawigacji | `42` wysokości |
| Slot ikony | `24 × 24` |
| Właściwa ikona SVG | `16 × 16` |
| Etykieta grupy | `24` wysokości |
| Padding poziomy | `12` |
| Odstęp ikona–tekst | `8` |

**Warianty:** pełny `216` (domyślny), kompaktowy `56` (wyłącznie jawną akcją użytkownika), wybór zapamiętywany. Zmiana szerokości natychmiastowa, bez animacji geometrii. Zmiana danych/modułu/zaznaczenia nie zwija sidebara samodzielnie. Minimalne okno obsługuje pełny sidebar — brak automatycznego zwijania.

**Treść:** etykieta jednolinijkowa, nadmiar skracany wielokropkiem, pełna nazwa w tooltipie. Aktywny element nie zmienia wymiarów. Badge liczbowe ma zarezerwowany slot. W trybie kompaktowym: ikony i tooltipy zostają, nagłówki grup zastępują separatory. Kolejność grup stabilna, niezależna od danych domenowych.

## Typografia

| Zastosowanie | Krój | Rozmiar i wariant |
| --- | --- | --- |
| Tekst podstawowy UI | Inter | `15`, Regular |
| Nawigacja | Inter | `15`, Medium |
| Tekst pomocniczy | Inter | `13`, Regular |
| Etykieta grupy | Inter | `11`, Medium, uppercase |
| Nagłówek panelu | Inter | `13`, Medium |
| Tytuł workspace'u | Alegreya | `26`, SemiBold |
| Nazwa Entity | docelowy serif | `28` |
| Wartość mechaniczna | Inter | `16–20`, cyfry tablicowe |

Tekst podstawowy nie schodzi poniżej `12`, poza krótkimi etykietami technicznymi.

### Profile wielkości UI

Brak dowolnego suwaka zmieniającego font czy transformacji skalującej drzewo — trzy skoordynowane zestawy tokenów:

| Profil | Tekst bazowy | Kontrolka | Nawigacja | Wiersz kampanii | Sidebar |
| --- | ---: | ---: | ---: | ---: | ---: |
| `Small` | `14` | `36` | `40` | `72` | `216` |
| `Medium` | `15` | `38` | `42` | `76` | `224` |
| `Large` | `17` | `42` | `46` | `84` | `248` |

`Medium` domyślny. Profil niezależny od DPI systemowego: DPI utrzymuje fizyczną czytelność, profil wyraża preferencję użytkownika. Jawna zmiana profilu może przeorganizować layout — to dopuszczalna zmiana geometrii.

Pierwsza implementacja: parametr `--ui-scale=small|medium|large`. Wybór w ustawieniach i zapis — faza trwałego stanu shellu. Rozmiar tekstu dokumentowego (notatki, statblocki) pozostaje niezależny.

## Kontrolki i powtarzalne wiersze

| Komponent | Wymiar |
| --- | ---: |
| Zwykły przycisk | `38` wysokości |
| Przycisk ikonowy | `38 × 38` |
| Pole jednowierszowe | `38` wysokości |
| Wiersz zwartej tabeli | `38` wysokości |
| Standardowy wiersz danych | `42` wysokości |
| Wiersz z tytułem i opisem | `48` wysokości |
| Nagłówek panelu | `36` wysokości |
| Stopka panelu z akcjami | `40` wysokości |

Stany normal/hover/focus/pressed/disabled/loading nie zmieniają zewnętrznego wymiaru. Spinner zastępuje zawartość albo zajmuje zarezerwowany slot.

## Zasada maksymalizacji

Brak globalnego `MaxWidth` ani centrowania workspace'u. Po maksymalizacji: shell zachowuje stałe wymiary; workspace rozciąga się do całej pozostałej przestrzeni; fonty/przyciski/ikony/paddingi nie rosną proporcjonalnie; panele danych mogą pokazać więcej informacji; panele pomocnicze respektują własne `MinWidth`/`MaxWidth`; ekran może przejść do innej kompozycji po przekroczeniu breakpointu; brak pustych marginesów tylko po to, by utrzymać szerokość mockupu. Zmiana wariantu layoutu po zwiększeniu okna to jawna akcja użytkownika — nie niedozwolony layout shift.

## Breakpointy workspace'u

Liczone dla właściwego obszaru workspace'u po odjęciu sidebara, chrome i paddingu — nie dla monitora ani całego okna.

| Wariant | Szerokość workspace'u | Zastosowanie |
| --- | ---: | --- |
| `Compact` | `760–959` | zwarta kompozycja dwukolumnowa |
| `Standard` | `960–1359` | dwie kolumny z większymi viewportami |
| `Wide` | `1360+` | jawna kompozycja do trzech głównych kolumn |

Brak dodatkowych breakpointów bez rzeczywistej zmiany architektury informacji — żadnego osobnego wariantu per fizyczna rozdzielczość. W obrębie wariantu panele mogą płynnie zmieniać szerokość przez star sizing; położenie i rola paneli pozostają stabilne.

## Strategie wzrostu paneli

Każdy panel deklaruje jedną politykę:

- **`FixedMetric`** — stałe wymiary: topbar, statusbar, nawigacja, przyciski, ikonografia, nagłówki paneli.
- **`Bounded`** — rośnie w użytecznym zakresie, nadmiar przestrzeni trafia do innego panelu. Limity startowe:

  | Panel | MinWidth | MaxWidth |
  | --- | ---: | ---: |
  | Czas świata | `240` | `360` |
  | Inspektor | `280` | `400` |
  | Szybkie akcje | `240` | `320` |

- **`FluidData`** — przejmuje dostępną przestrzeń: drużyna, historia, zdarzenia, tabele, tracker inicjatywy, konsola, mapa, edytor wiedzy.
- **`Document`** — kolumna tekstu z lokalnym ograniczeniem czytelnej długości linii; pozostała przestrzeń dla outline'u/backlinków/inspektora. Ograniczenie tekstu nie staje się globalnym ograniczeniem workspace'u.

## Dashboard aktywnej sesji

Jawny grid semantyczny — nie masonry ani auto-tiling.

**`Compact`/`Standard`** (dwanaście logicznych kolumn):

```text
┌─────────────────────────┬──────────────┐
│ Drużyna                 │ Czas         │
├─────────────────────┬───┴──────────────┤
│ Zdarzenia           │ Notatka          │
├─────────────────────┴──────────────────┤
│ Historia zmian                          │
└─────────────────────────────────────────┘
```

Drużyna `8/12`, czas `4/12`, zdarzenia `7/12`, notatka `5/12`, historia `12/12`, odstępy `8`, górny rząd `160`, dalsze rzędy `*` z minimami. `Standard` może ujawniać dodatkowe dane wewnątrz paneli bez zmiany ich roli.

**`Wide`:**

```text
┌──────────────────┬────────────┬──────────────────┐
│ Drużyna          │ Czas       │ Zdarzenia        │
├──────────────────┼────────────┴──────────────────┤
│ Notatka sesyjna  │ Historia / konsola            │
└──────────────────┴───────────────────────────────┘
```

Maksymalnie trzy główne kolumny; górny rząd `160–176`; dolny obszar przejmuje pozostałą wysokość; historia/konsola ma największy elastyczny viewport; brak czwartej kolumny tylko dlatego, że jest miejsce; panel czasu nie rośnie do szerokości głównego panelu danych.

Zmiana kompozycji wyłącznie po przekroczeniu breakpointu workspace'u.

## Polityka wysokości

Nie rozciągamy równomiernie wszystkich paneli na wysokich monitorach. Panele orientacyjne — stała/ograniczona wysokość. Listy, historia, konsola, notatki, mapa — `*`, pokazują więcej treści. Panel czasu nie rośnie pionowo tylko dlatego, że ekran ma większą fizyczną wysokość. Wewnętrzne listy przewijają się dopiero po wykorzystaniu przyznanego viewportu — bez pustych dekoracyjnych powierzchni wypełniających wysokość.

## Strategie poszczególnych ekranów

**Biblioteka kampanii:** `Compact` — lista wg makiety. `Standard` — lista wykorzystuje szerokość do `960`, potem wyrównana do lewej. `Wide` — master–detail: master `760` w profilu `Medium`, szczegóły przejmują resztę. Viewport listy do ośmiu wierszy `Medium`; mała liczba rekordów nie tworzy ramy wokół pustej wysokości ani nie rozciąga sztucznie wierszy.

**Rejestry i tabele:** wypełniają workspace; większa szerokość ujawnia kolumny wg progów; wartości kluczowe nie znikają w żadnym wariancie.

**Baza wiedzy:** preferowany układ `drzewo dokumentów | dokument | inspektor / backlinki`. Dokument zachowuje czytelną długość linii, sąsiednia przestrzeń pozostaje funkcjonalna. Zamknięcie panelu pomocniczego — jawna akcja użytkownika.

**Statblock:** jedna kolumna w wąskim kontenerze, dwie–trzy w szerszym. Akapit nie rozciąga się na pełną szerokość monitora.

**Mapa, graf, powierzchnie przestrzenne:** wypełniają cały przyznany obszar, bez globalnego `MaxWidth`.

**Kreator kampanii:** formularz lokalnie ograniczony, pozostała przestrzeń dla podsumowania/walidacji/podglądu paczek i modułów.

## Przepełnienie treści

| Typ treści | Polityka |
| --- | --- |
| Nawigacja | jedna linia, ellipsis, tooltip |
| Tytuł panelu | jedna linia, ellipsis |
| Nazwa Entity | maks. dwie linie w zarezerwowanym nagłówku |
| Wartość mechaniczna | bez zawijania, stały slot, cyfry tablicowe |
| Opis | zawijanie w przewijanym viewporcie |
| Lista | stałe wysokości wierszy, scrolling |
| Status | jedna linia; szczegóły w nakładce |
| Walidacja | zarezerwowany slot albo panel podsumowania |

Brak nieograniczonego `Auto` dla ścieżek zależnych od tekstu/danych użytkownika. Lokalny element może użyć `Auto`, jeśli kontener ma formalne `Min`/`Max`, a przepełnienie nie wpływa na sąsiednie regiony.

## Stany asynchroniczne

Każdy panel zachowuje ten sam zewnętrzny wymiar w stanach `Loading` / `Empty` / `Ready` / `Error`. Loading — placeholdery odpowiadające docelowym slotom. Empty — wypełnia przyznany viewport, panel się nie zwija. Błąd — krótki komunikat + akcja ponowienia, szczegóły w nakładce/osobnym widoku. Ikony mają zarezerwowany slot przed załadowaniem. Brak chwilowego empty state przed zakończeniem operacji asynchronicznej.

## Narzędzia layoutu Avalonia

**Stosujemy:** `Grid` dla shellu/dashboardów/podziałów strukturalnych; fixed sizing dla chrome i kontrolek; star sizing dla pozostałej przestrzeni; `MinWidth`/`MaxWidth`/`MinHeight`/`MaxHeight` dla kontraktów paneli; container queries dla lokalnej adaptacji; `ItemsRepeater` + `UniformGridLayout` dla równorzędnych kolekcji kafli; `ScrollViewer` w formalnie ograniczonym viewporcie; `UseLayoutRounding="True"` w głównym drzewie.

**Nie stosujemy w v1:** globalnego `MaxWidth` workspace'u; `Viewbox`/transformacji skalującej całe UI; automatycznego masonry; nieograniczonego `WrapPanel` dla narzędzi sesyjnych; swobodnego dokowania/przemieszczania paneli; płynnego animowania wymiarów głównych regionów; pomiarów zależnych od długości bieżącej treści.

`GridSplitter` dopuszczalny wyłącznie tam, gdzie model pracy naturalnie tego wymaga (np. drzewo dokumentów–edytor–inspektor); musi respektować `Min`/`Max`, krok zgodny z siatką i zapisywać rozmiar wybrany przez użytkownika.

## Macierz weryfikacji

Każdy kluczowy ekran sprawdzamy co najmniej w:

| Rozmiar | Cel |
| --- | --- |
| `1024 × 680` | minimalne wspierane okno |
| `1280 × 800` | rozmiar referencyjny |
| `1536 × 864` | typowy efektywny obszar 1080p przy skalowaniu systemowym |
| `1920 × 1080` | szeroki wariant przy 100% |
| `2048 × 1152` | typowy efektywny obszar 1440p przy podwyższonym skalowaniu |
| `2560 × 1440` | duży wariant przy 100% |

Dodatkowo: pełny i kompaktowy sidebar; najkrótsze/najdłuższe etykiety; wartości jedno- do czterocyfrowe; pustą i przewijaną listę; loading/empty/ready/error; brak ikony/assetu; zmianę rozmiaru przez każdy breakpoint w obu kierunkach; profile `Small`/`Medium`/`Large` przy skalowaniu `100%`/`125%`/`150%`/`200%`; maksymalizację, przywrócenie, zmianę monitora o innym DPI; fokus klawiatury i kolejność nawigacji.

Weryfikacja kończy się niepowodzeniem, jeśli zmiana danych bez zmiany rozmiaru okna przesuwa główne regiony, zmienia wysokość panelu albo reorganizuje dashboard.

## Decyzje odłożone poza v1

Swobodne dokowanie paneli · zapisywane dowolne układy dashboardu · czwarta lub kolejne główne kolumny pulpitu · automatyczny masonry · tryb sesji ukrywający chrome systemowy · osobne profile layoutu dla wielu monitorów.

Każda wymaga osobnego uzasadnienia UX i kontraktu trwałości/stabilności.
