# Kontrakt UI v1

> **Status:** zaakceptowany kontrakt implementacyjny  
> **Wersja:** 1.1  
> **Data:** 2026-08-24  
> **Cel:** jednoznaczne określenie geometrii, responsywności i zachowania interfejsu przed pierwszą fazą implementacji Avalonia.

## 1. Zakres i pierwszeństwo

Dokument definiuje normatywne wartości dla pierwszej wersji shellu oraz podstawowych komponentów Desktop. Jeżeli makieta, istniejący PoC albo lokalny styl XAML są z nim sprzeczne, pierwszeństwo ma ten kontrakt.

Kontrakt nie zamraża zawartości przyszłych modułów. Określa wspólny układ współrzędnych, w którym moduły muszą działać.

## 2. Jednostki i skalowanie systemowe

Wszystkie wymiary są podawane w logicznych, niezależnych od DPI jednostkach Avalonia. Nie projektujemy bezpośrednio pod fizyczne piksele monitora ani samą etykietę rozdzielczości `1080p` lub `1440p`.

Breakpointy reagują na rzeczywisty logiczny rozmiar kontenera po uwzględnieniu systemowego skalowania, a nie na rozdzielczość urządzenia.

Obowiązujące zasady:

- layout respektuje skalowanie DPI systemu operacyjnego;
- na Windows aplikacja jawnie deklaruje `PerMonitorV2`, a Avalonia przelicza DIPs na piksele urządzenia dla bieżącego monitora;
- nie mnożymy tokenów ręcznie przez `Screen.Scaling`, ponieważ powodowałoby to podwójne skalowanie;
- główna siatka używa wielokrotności `4`;
- włączamy zaokrąglanie layoutu do granic pikseli urządzenia;
- nie stosujemy transformacji skalującej całe drzewo UI;
- większy monitor pokazuje więcej danych, a nie większe kontrolki;
- ustawienie wielkości tekstu nie może samodzielnie zwiększać zawartości wewnątrz niezmienionego slotu.

## 3. Okno aplikacji

| Parametr | Wartość v1 |
| --- | ---: |
| Rozmiar referencyjny obszaru klienta | `1280 × 800` |
| Minimalny obszar klienta | `1024 × 680` dla `Small` i `Medium`; `1180 × 760` dla `Large` |
| Stan początkowy po pierwszym uruchomieniu | `1280 × 800`, ograniczony do dostępnego obszaru roboczego ekranu |
| Kolejne uruchomienia | ostatni poprawnie zapisany rozmiar, pozycja i stan okna |
| Maksymalizacja globalna | nie jest wymuszana |
| Aktywna sesja | projektowana przede wszystkim do pracy w oknie zmaksymalizowanym |

Aplikacja nie obsługuje mobilnego ani jednokolumnowego wariantu poniżej minimalnego rozmiaru. System nie może przywrócić zapisanego okna poza aktualnie dostępnym obszarem monitorów.

W przyszłości może pojawić się jawny „Tryb sesji”, który maksymalizuje okno i ogranicza drugorzędny chrome. Nie jest częścią pierwszej implementacji shellu.

## 4. Bazowa siatka i geometria

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

Mocniejsza rama używa innego tokenu koloru, a nie większej grubości. Nie stosujemy ujemnych marginesów do korygowania konstrukcji shellu.

## 5. Shell

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

Sekcja marki w topbarze zawsze odpowiada szerokości bieżącego wariantu sidebara, dzięki czemu pionowa linia konstrukcji przechodzi przez całe okno.

Topbar, sidebar i statusbar nie rosną po maksymalizacji. Całą dodatkową przestrzeń przejmuje workspace.

## 6. Sidebar

| Element | Wymiar |
| --- | ---: |
| Wiersz nawigacji | `42` wysokości |
| Slot ikony | `24 × 24` |
| Właściwa ikona SVG | `16 × 16` |
| Etykieta grupy | `24` wysokości |
| Padding poziomy | `12` |
| Odstęp ikona–tekst | `8` |

### 6.1. Warianty

- Pełny sidebar `216` jest wariantem domyślnym.
- Kompaktowy sidebar `56` jest włączany wyłącznie jawną akcją użytkownika.
- Wybrany wariant jest zapamiętywany.
- W v1 zmiana szerokości następuje natychmiast, bez animacji geometrii.
- Zmiana danych, modułu lub zaznaczenia nie może samodzielnie zwinąć sidebara.
- Minimalne okno obsługuje pełny sidebar, dlatego nie potrzebujemy automatycznego zwijania.

### 6.2. Treść

- Etykieta pozycji zajmuje jedną linię.
- Nadmiar jest skracany wielokropkiem.
- Pełna nazwa jest dostępna w tooltipie.
- Aktywny element nie zmienia wymiarów.
- Badge liczbowe ma zarezerwowany slot.
- W trybie kompaktowym pozostają ikony i tooltipy, a nagłówki grup są zastępowane separatorami.
- Kolejność grup pozostaje stabilna i nie zależy od bieżących danych domenowych.

## 7. Typografia

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

Tekst podstawowy nie schodzi poniżej `12`, poza krótkimi etykietami technicznymi. Długie teksty dokumentowe mogą używać własnej skali wewnątrz przewijanego viewportu.

### 7.1. Profile wielkości UI

Nie implementujemy dowolnego suwaka zmieniającego sam font ani transformacji skalującej całe drzewo. Obowiązują trzy skoordynowane zestawy tokenów:

| Profil | Tekst bazowy | Kontrolka | Nawigacja | Wiersz kampanii | Sidebar |
| --- | ---: | ---: | ---: | ---: | ---: |
| `Small` | `14` | `36` | `40` | `72` | `216` |
| `Medium` | `15` | `38` | `42` | `76` | `224` |
| `Large` | `17` | `42` | `46` | `84` | `248` |

`Medium` jest profilem domyślnym. Profil pozostaje niezależny od systemowego DPI: DPI utrzymuje fizyczną czytelność na danym monitorze, a profil wyraża preferencję użytkownika. Jawna zmiana profilu może przeorganizować layout i jest dopuszczalną zmianą geometrii.

Pierwsza implementacja pozwala testować profile parametrem `--ui-scale=small|medium|large`. Docelowy wybór w ustawieniach i jego zapis należą do fazy trwałego stanu shellu. Rozmiar tekstu dokumentowego dla notatek i statblocków pozostanie niezależny.

## 8. Kontrolki i powtarzalne wiersze

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

Stan normalny, hover, focus, pressed, disabled i loading nie zmienia zewnętrznego wymiaru kontrolki. Spinner zastępuje istniejącą zawartość albo zajmuje uprzednio zarezerwowany slot.

## 9. Zasada maksymalizacji

Nie stosujemy globalnego `MaxWidth` ani centrowania głównego workspace'u. Po maksymalizacji:

- shell zachowuje stałe wymiary;
- workspace rozciąga się do całej pozostałej szerokości i wysokości;
- fonty, przyciski, ikony i paddingi nie rosną proporcjonalnie;
- panele danych mogą pokazać więcej informacji;
- panele pomocnicze respektują własne limity `MinWidth` i `MaxWidth`;
- ekran może przejść do innej jawnej kompozycji po przekroczeniu breakpointu;
- nie tworzymy pustych zewnętrznych marginesów tylko po to, aby utrzymać szerokość mockupu.

Zwiększenie rozmiaru okna jest jawną akcją użytkownika, dlatego wynikająca z niego zmiana wariantu layoutu nie jest traktowana jako niedozwolony layout shift.

## 10. Breakpointy workspace'u

Breakpoint jest obliczany dla właściwego obszaru workspace'u po odjęciu sidebara, chrome i paddingu, a nie dla całego monitora ani nawet całego okna.

| Wariant | Szerokość workspace'u | Zastosowanie |
| --- | ---: | --- |
| `Compact` | `760–959` | zwarta kompozycja dwukolumnowa |
| `Standard` | `960–1359` | dwie kolumny z większymi viewportami i dodatkowymi danymi |
| `Wide` | `1360+` | jawna kompozycja do trzech głównych kolumn |

Nie wprowadzamy dodatkowych breakpointów bez rzeczywistej zmiany architektury informacji. W szczególności nie tworzymy osobnego wariantu dla każdej fizycznej rozdzielczości.

W obrębie wariantu panele mogą płynnie zmieniać szerokość przez star sizing. Położenie i semantyczna rola paneli pozostają stabilne.

## 11. Strategie wzrostu paneli

Każdy panel deklaruje jedną z poniższych polityk.

### 11.1. `FixedMetric`

Kontrolki i chrome zachowujące stałe wymiary: topbar, statusbar, nawigacja, przyciski, ikonografia i nagłówki paneli.

### 11.2. `Bounded`

Panel rośnie tylko w użytecznym zakresie, po czym dodatkowa przestrzeń trafia do innego panelu.

Początkowe limity:

| Panel | MinWidth | MaxWidth |
| --- | ---: | ---: |
| Czas świata | `240` | `360` |
| Inspektor | `280` | `400` |
| Szybkie akcje | `240` | `320` |

Wartości są częścią kontraktu startowego i mogą zostać doprecyzowane na realistycznej zawartości bez zmiany ogólnej polityki.

### 11.3. `FluidData`

Panel przejmuje dostępną przestrzeń i zamienia ją na więcej użytecznej informacji. Dotyczy drużyny, historii, zdarzeń, tabel, trackera inicjatywy, konsoli, mapy i edytora wiedzy.

### 11.4. `Document`

Kolumna ciągłego tekstu może mieć lokalne ograniczenie czytelnej długości linii. Pozostała przestrzeń należy do outline'u, backlinków, inspektora Entity, podglądu relacji albo innej funkcji dokumentowej. Ograniczenie tekstu nie może stać się globalnym ograniczeniem całego workspace'u.

## 12. Dashboard aktywnej sesji

Dashboard używa jawnego gridu semantycznego, nie automatycznego masonry ani swobodnego auto-tilingu.

### 12.1. `Compact` i `Standard`

```text
┌─────────────────────────┬──────────────┐
│ Drużyna                 │ Czas         │
├─────────────────────┬───┴──────────────┤
│ Zdarzenia           │ Notatka          │
├─────────────────────┴──────────────────┤
│ Historia zmian                          │
└─────────────────────────────────────────┘
```

Implementacja bazuje na dwunastu logicznych kolumnach:

- drużyna: `8/12`, czas: `4/12`;
- zdarzenia: `7/12`, notatka: `5/12`;
- historia: `12/12`;
- odstępy: `8`;
- górny rząd orientacyjny: `160` wysokości;
- dalsze rzędy: proporcjonalne `*` z wartościami minimalnymi;
- wariant `Standard` może ujawniać dodatkowe dane wewnątrz paneli bez zmiany ich roli.

### 12.2. `Wide`

```text
┌──────────────────┬────────────┬──────────────────┐
│ Drużyna          │ Czas       │ Zdarzenia        │
├──────────────────┼────────────┴──────────────────┤
│ Notatka sesyjna  │ Historia / konsola            │
└──────────────────┴───────────────────────────────┘
```

- maksymalnie trzy główne kolumny;
- górny rząd `160–176`;
- dolny obszar przejmuje pozostałą wysokość;
- historia lub konsola otrzymuje największy elastyczny viewport;
- nie dodajemy czwartej i kolejnych kolumn tylko dlatego, że jest dostępne miejsce;
- panel czasu pozostaje ograniczony i nie rośnie do szerokości głównego panelu danych.

Zmiana pomiędzy kompozycjami następuje wyłącznie po przekroczeniu breakpointu workspace'u. W typowym środowisku użytkownika wariant pozostaje stabilny przez całą sesję.

## 13. Polityka wysokości

Nie rozciągamy równomiernie wszystkich paneli na wysokich monitorach.

- Panele orientacyjne i podsumowujące mają wysokość stałą albo ograniczoną.
- Listy, historia, konsola, notatki i mapa używają `*` i pokazują więcej treści.
- Panel czasu nie powinien rosnąć pionowo tylko dlatego, że ekran ma 1440 pikseli fizycznej wysokości.
- Wewnętrzne listy przewijają się dopiero po wykorzystaniu przyznanego viewportu.
- Nie dodajemy pustych dekoracyjnych powierzchni w celu wypełnienia wysokości.

## 14. Strategie poszczególnych ekranów

### 14.1. Biblioteka kampanii

- W `Compact` działa jako lista zgodna z zaakceptowaną makietą.
- W `Standard` lista wykorzystuje całą dostępną szerokość do `960`, po czym pozostaje wyrównana do lewej.
- W `Wide` działa jako układ master–detail: master ma `760` szerokości w profilu `Medium`, a panel szczegółów przejmuje pozostałą przestrzeń.
- Lista ma maksymalny viewport odpowiadający ośmiu wierszom `Medium`; mała liczba rekordów nie tworzy ramy wokół całej pustej wysokości workspace'u.
- Mała liczba kampanii może pozostawić pustą przestrzeń poniżej listy; nie rozciągamy sztucznie wierszy.

### 14.2. Rejestry i tabele

- Wypełniają dostępny workspace.
- Większa szerokość ujawnia dodatkowe kolumny według jawnych progów.
- Wartości kluczowe nie znikają w żadnym wariancie.

### 14.3. Baza wiedzy

Preferowany szeroki układ:

```text
drzewo dokumentów | dokument | inspektor / backlinki
```

Dokument zachowuje czytelną długość linii, lecz sąsiednia przestrzeń pozostaje funkcjonalna. Zamknięcie panelu pomocniczego jest jawną akcją użytkownika.

### 14.4. Statblock

- W wąskim kontenerze używa jednej kolumny.
- W szerszym kontenerze przechodzi do dwóch lub trzech kolumn sekcji.
- Akapit nie rozciąga się do pełnej szerokości monitora.

### 14.5. Mapa, graf i powierzchnie przestrzenne

Wypełniają cały przyznany obszar i nie używają globalnego `MaxWidth`.

### 14.6. Kreator kampanii

Formularz może być lokalnie ograniczony, ale pozostała przestrzeń służy podsumowaniu, walidacji konfiguracji albo podglądowi wybranych paczek i modułów.

## 15. Przepełnienie treści

| Typ treści | Polityka |
| --- | --- |
| Nawigacja | jedna linia, ellipsis, tooltip |
| Tytuł panelu | jedna linia, ellipsis |
| Nazwa Entity | maksymalnie dwie linie w zarezerwowanym nagłówku |
| Wartość mechaniczna | bez zawijania, stały slot, cyfry tablicowe |
| Opis | zawijanie wewnątrz przewijanego viewportu |
| Lista | stałe wysokości wierszy i scrolling |
| Status | jedna linia; szczegóły w nakładce |
| Walidacja | zarezerwowany slot albo panel podsumowania |

Nie używamy nieograniczonego `Auto` dla ścieżek zależnych od tekstu lub danych użytkownika. Lokalny element może używać `Auto`, jeśli jego kontener posiada formalne ograniczenie `Min`/`Max` i przepełnienie nie wpływa na sąsiednie regiony.

## 16. Stany asynchroniczne

Każdy panel zachowuje ten sam zewnętrzny wymiar w stanach:

```text
Loading
Empty
Ready
Error
```

- loading używa placeholderów odpowiadających docelowym slotom;
- empty state wypełnia przyznany viewport i nie zwija panelu;
- błąd pokazuje krótki komunikat oraz akcję ponowienia;
- szczegóły błędu są nakładką albo osobnym widokiem;
- ikony mają zarezerwowany slot przed załadowaniem;
- nie pokazujemy chwilowego empty state przed zakończeniem operacji asynchronicznej.

## 17. Narzędzia layoutu Avalonia

### Stosujemy

- `Grid` dla shellu, dashboardów i strukturalnych podziałów;
- fixed sizing dla chrome i kontrolek;
- star sizing dla pozostałej przestrzeni;
- `MinWidth`, `MaxWidth`, `MinHeight` i `MaxHeight` dla kontraktów paneli;
- container queries dla lokalnej adaptacji wnętrza komponentu;
- `ItemsRepeater` z `UniformGridLayout` dla równorzędnych kolekcji kafli;
- `ScrollViewer` wewnątrz formalnie ograniczonego viewportu;
- `UseLayoutRounding="True"` w głównym drzewie wizualnym.

### Nie stosujemy w v1

- globalnego `MaxWidth` workspace'u;
- `Viewbox` albo transformacji skalującej całe UI;
- automatycznego masonry;
- nieograniczonego `WrapPanel` dla narzędzi sesyjnych;
- swobodnego dokowania i przemieszczania paneli;
- płynnego animowania wymiarów głównych regionów;
- pomiarów zależnych od długości bieżącej treści.

`GridSplitter` jest dopuszczalny wyłącznie w ekranach, których model pracy naturalnie wymaga podziału, np. drzewo dokumentów–edytor–inspektor. Musi respektować `Min`/`Max`, krok zgodny z siatką i zapisywać rozmiar wybrany przez użytkownika.

## 18. Macierz weryfikacji

Każdy kluczowy ekran musi zostać sprawdzony przynajmniej w poniższych logicznych rozmiarach obszaru klienta:

| Rozmiar | Cel |
| --- | --- |
| `1024 × 680` | minimalne wspierane okno |
| `1280 × 800` | rozmiar referencyjny |
| `1536 × 864` | typowy efektywny obszar 1080p przy skalowaniu systemowym |
| `1920 × 1080` | szeroki wariant przy 100% |
| `2048 × 1152` | typowy efektywny obszar 1440p przy podwyższonym skalowaniu |
| `2560 × 1440` | duży wariant przy 100% |

Dodatkowo testujemy:

- pełny i kompaktowy sidebar;
- najkrótsze oraz najdłuższe dopuszczalne etykiety;
- wartości jedno-, dwu-, trzy- i czterocyfrowe;
- pustą listę oraz listę wymagającą przewijania;
- loading, empty, ready i error;
- brak ikony albo assetu;
- zmianę rozmiaru przez każdy breakpoint w obu kierunkach;
- profile `Small`, `Medium` i `Large` przy skalowaniu systemowym `100%`, `125%`, `150%` i `200%`;
- maksymalizację, przywrócenie oraz zmianę monitora o innym DPI;
- fokus klawiatury i kolejność nawigacji.

Weryfikacja kończy się niepowodzeniem, jeżeli zmiana danych bez zmiany rozmiaru okna przesuwa główne regiony, zmienia wysokość panelu albo reorganizuje dashboard.

## 19. Decyzje odłożone poza v1

- Swobodne dokowanie paneli.
- Zapisywane, dowolne układy dashboardu użytkownika.
- Czwarta lub kolejne główne kolumny pulpitu.
- Automatyczny układ masonry.
- Tryb sesji ukrywający chrome systemowy.
- Osobne profile layoutu dla wielu monitorów.

Każda z tych funkcji wymaga osobnego uzasadnienia UX oraz kontraktu trwałości i stabilności.
