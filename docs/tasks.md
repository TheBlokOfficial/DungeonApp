# DungeonApp — kolejka pracy

**Status: stan na 2026-09-12.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

Kolejność w sekcji „Następne" jest wiążąca tam, gdzie to zapisano. Reszta jest listą, nie planem.

Gałąź robocza: `feat/content-registry`, niescalona do `master`. Build i 217 testów zielone.

---

## Gdzie jesteśmy

Kroki 1–4 z sekcji „Kolejność prac" [architecture.md](architecture.md) są przejściem
z szablonów jako plików danych na skompilowane typy treści. **Zrobione:**

* `DungeonApp.App` jako korzeń kompozycji — powłoka przestała być plikiem wykonywalnym, więc
  granica „powłoka nie zna żadnego zestawu" jest sprawdzalna testem po referencjach.
* `DungeonApp.Content.Dnd5e` z typami `Monster` i `Gear` na wymaganych polach nazwanych.
* `tests/DungeonApp.Architecture.Tests` — skan słownictwa (silnik i powłoka, `.cs` i `.axaml`,
  słownik z refleksji po zestawie) plus testy po referencjach.
* Wpisy biorą kształt z zestawu: koperta `ContentValues`, `IContentTypeCatalog`,
  `IContentPresentation`, pierwsze zaprojektowane `MonsterCardView` i `GearCardView`.
* `TreatWarningsAsErrors` — kompilator jest walidatorem treści, więc jego ostrzeżenia są
  ostrzeżeniami o treści.

**Domknięte 2026-09-12 (druga sesja):**

* **Zero zestawów treści jest awarią głośną.** Strażnik w `App.Initialize()` sprawdza sam warunek
  (pusta lista), nie pośrednika (który konstruktor zadziałał), więc łapie także korzeń kompozycji
  przekazujący `[]` jawnie. Stoi przed ładowaniem XAML-a, co czyni go sprawdzalnym testem bez
  uruchamiania Avalonii.
* **Stary mechanizm treści rozebrany** — dwanaście typów bez konsumenta zniknęło z repozytorium.
  Żadna referencja nie okazała się żywa; kompilator jest tego dowodem.
* **`code-map.md` przepisany od zera** wobec kodu, sekcja po sekcji. Przy okazji obalił własne
  twierdzenie z poprzedniej wersji: pole `Ruleset` w manifeście kampanii nigdy nie zniknęło,
  a `PackReference` nigdy nie istniał.

---

## Następne — wg dokumentu architektury

Przejście na skompilowane typy treści jest domknięte, więc kolejka wraca do kroków 5–9
z sekcji „Kolejność prac". Kolejność poniżej jest wiążąca:

1. **Odrzucanie per plik wpisu** — dziś wadliwy manifest odrzuca paczkę, ale pozycje
   nierozwiązane nie są jeszcze widoczne w rejestrze tak, jak wymaga sekcja
   „Co się dzieje, gdy treść jest zepsuta".
2. **Jeden prymityw zapisu atomowego** — trzy własne implementacje tego samego. Wydzielić
   **przed** magazynem instancji, nie po.
3. **Magazyn instancji i nakładki** — pierwszy realny stan kampanii.
4. **Pierwsze prawdziwe narzędzie biurka** — i razem z nim usunięcie licznika, który jest
   celowym rusztowaniem, oraz weryfikacja, czy `DataBlockShape` nadal zarabia na siebie.
5. **Formuły, sloty, dokument** — kolejność do ustalenia osobno.

---

## Odłożone, poza kolejnością

Dwie pozycje z sesji 2026-09-05, świadomie odłożone i niezależne od przejścia powyżej.
Nie mają wyzwalacza — można je wziąć, kiedy pasują.

1. **Testy dla `Startup/*`.** Sekwencja startowa ma pięć kroków; pokrycie ma tylko
   `LoadContentPacksStep`. Warte zamrożenia: kolejność kroków, degradacja przy błędzie
   (awaria kroku nie blokuje wejścia) i zgodność `TotalSteps` z długością tablicy.
   Mechanizm jest jawny, więc łatwy do przetestowania — i łatwy do zepsucia niezauważenie
   przy dołożeniu kolejnego kroku.

2. **Dokończenie przepięcia widoków na skalę odstępów.** Klucze `DungeonSpacingXs..Xxxl`
   i `DungeonPaddingXs..Xxxl` istnieją od 2026-09-05. Stan na 2026-09-12: siedem widoków
   bierze odstępy ze skali, sześć ma nadal liczby wpisane wprost. Mechaniczny diff,
   dlatego osobno.

Trzecia pozycja z tamtej sesji — usunięcie domknięcia nad `_shell` w `App.Initialize()` —
jest **zamknięta jako nie-dług**: stoi nadal, ale w kodzie jest opisana jako świadoma
decyzja z uzasadnieniem.

---

## Znane problemy

**Blokada katalogu `bin` projektu wykonywalnego.** `dotnet build src/DungeonApp.App` pada czasem na
`MSB3021`/`MSB3027` — „plik jest zablokowany przez .NET Host". Rozpoznane 2026-09-12: to
`Avalonia.Designer.HostApp` (podglądacz XAML), którego uruchamia Rider, gdy otwarta jest zakładka
z podglądem `.axaml`; rodzicem procesu jest `rider64.exe`. **To nie jest błąd kodu.** Rozwiązanie:
zamknąć w IDE zakładkę z podglądem.

Weryfikacja mimo blokady jest pełna: cztery projekty testowe nie zależą od `DungeonApp.App`, więc
build i testy na nich dają potwierdzenie kompilatora dla silnika, powłoki i zestawu treści.
Niepotwierdzone zostaje wtedy wyłącznie to, że sam plik wykonywalny się linkuje — a to zostało
potwierdzone osobno 2026-09-12, przy zwolnionym katalogu.

---

## Pytania otwarte z wyzwalaczami

Pełne uzasadnienia są w [architecture.md](architecture.md), sekcja „Pytania otwarte", oraz
w [decisions.md](decisions.md). Tu jest tylko lista z warunkiem powrotu — **nie ruszać
wcześniej**, bo wtedy decyzja jest zgadywaniem, nie rozstrzygnięciem.

| Pytanie | Wyzwalacz |
|---|---|
| Zagnieżdżenie wartości **razem z plikiem wpisu** | jawnie niezamknięte; `decisions.md` poz. 7, trzecia runda — tam spis argumentów, które padły ze starym formatem i których nie wolno już przytaczać |
| `ArmorClass(Value, Source)`, `HitPoints(Value, Formula)` jako pojęcia domenowe | magazyn instancji i nakładki — jedyny konsument, który rozstrzygnie to na dowodach |
| Format pliku wpisu (front-matter plus treść) | po wejściu slotów, nie przed |
| Czy `DataBlockShape` nadal zarabia na siebie | pierwsze prawdziwe narzędzie biurka |
| Widok domyślny karty | pierwszy rodzaj treści, którego nie chce się zaprojektować |
| Filtrowanie narzędzi per kampania | drugi zestaw treści |
| Autorstwo treści w aplikacji | otwarte dla wpisów; obejście przez wpisy lokalne dla kampanii pozostaje odrzucone |

---

## Archiwum

`archive/pre-pivot-content-layer` (`f5d5b7a`) — **nigdy nie scalana**. Praca sprzed
przeprojektowania warstwy treści: `SummaryContract`, `ContractFill`,
`SummaryContractResolution`, grupowanie rejestru, rozbudowane testy loadera i fixture'y.
Trzy fixture'y wpisów zostały stamtąd przywrócone; resztę trzyma się tylko po to, żeby nic
nie zginęło.
