# DungeonApp — kolejka pracy

**Status: stan na 2026-09-13.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

Kolejność w sekcji „Następne" jest wiążąca tam, gdzie to zapisano. Reszta jest listą, nie planem.

Gałąź: `master`. Build i 262 testy zielone.

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

**Domknięte 2026-09-13 (czwarta sesja):**

* **Jeden prymityw zapisu atomowego.** `AtomicWrite` w silniku zbiera pliki obok ich miejsc
  docelowych i przenosi je dopiero wtedy, gdy wszystkie zserializowały się czysto; przerwanie
  w połowie zostawia każde miejsce docelowe nietknięte, a po sobie nie zostawia pliku roboczego.
  Obsługuje oba potrzebne przypadki — jeden plik i całą generację naraz — bo `Commit` przenosi
  w kolejności dodania. Trzy ręcznie pisane ścieżki (`JsonCampaignRepository` ×2 plus
  `WorkspaceLayoutStore`) korzystają teraz z niego. Wydzielone **przed** magazynem instancji
  właśnie po to, żeby nie dołożył czwartej.
* **Kolejność zatwierdzania jest zamrożona testem.** Na niej stoi „manifest ląduje ostatni", czyli
  wykrywalność rozdartego zapisu — a nie sprawdzał jej dotąd żaden test, bo wzorzec był powtarzany
  trzy razy i weryfikowany wyłącznie pośrednio.
* **Magazyn układu biurka trzymany za słowo.** Obiecuje, że odczyt nigdy nie rzuca, a czego nie
  da się zrozumieć, zamienia w układ domyślny — i tej obietnicy nie sprawdzał żaden test. Teraz
  sprawdza ją dwadzieścia jeden, każdy po obu ścieżkach odczytu, bo mapowanie jest w magazynie
  napisane dwa razy i obie kopie muszą się zgadzać. Kod produkcyjny nietknięty.
* **Powód odrzucenia mówi co, nie który plik.** Ekran rejestru pokazywał nazwę zepsutego pliku dwa
  razy — jako tytuł wiersza i ponownie w treści komunikatu. Naprawione u źródła: `Location` mówi
  który plik, `Reason` mówi co jest nie tak. Odrzucenie paczki nadal nazywa plik samo, bo tam
  żaden tytuł wiersza tej roli nie przejmuje. Wyjątkiem pozostaje kolizja id, gdzie komunikat
  wymienia pliki kolidujące — ale już nie ten własny.
* **Nazwa pliku roboczego ujednolicona** na `.writing.tmp` w obu magazynach. Układ biurka używał
  wcześniej nazwy z `Guid`, która po awarii aplikacji zostawiała śmieć na zawsze; nazwa
  deterministyczna jest nadpisywana przy następnym zapisie. To jedyna zmiana zachowania
  w całym kroku.

**Domknięte 2026-09-12 (trzecia sesja):**

* **Odrzucanie per plik wpisu.** Defekt jednego pliku oznacza od teraz ten jeden plik; reszta paczki
  wczytuje się normalnie. Na poziomie paczki zostaje tylko to, co jest własnością paczki jako
  całości: manifest, niedający się wylistować katalog wpisów, limit liczby plików, kolizja id
  z inną paczką. Zduplikowany id wewnątrz paczki odrzuca **wszystkie** kolidujące pliki — żaden nie
  wygrywa po cichu, a rejestr nie dostaje dwóch wpisów pod jednym adresem.
* **Pliki niewczytane widoczne w rejestrze.** Lądują na końcu tej samej listy co wpisy, pod
  nagłówkiem `NIE WCZYTANE`, z komunikatem oprawiającym diagnostykę loadera zamiast karty.
  Rejestr złożony wyłącznie z zepsutych plików nie jest już „pusty".

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

Krok 5 z sekcji „Kolejność prac" ([architecture.md](architecture.md)) jest domknięty, więc kolejka
wchodzi w kroki 6–9. Kolejność poniżej jest wiążąca:

1. **Magazyn instancji i nakładki** — pierwszy realny stan kampanii. Zapis ma iść przez
   `AtomicWrite`, a nie przez czwartą własną kopię wzorca.
2. **Pierwsze prawdziwe narzędzie biurka** — i razem z nim usunięcie licznika, który jest
   celowym rusztowaniem, oraz weryfikacja, czy `DataBlockShape` nadal zarabia na siebie.
3. **Formuły, sloty, dokument** — kolejność do ustalenia osobno.

---

## Odłożone, poza kolejnością

Cztery pozycje świadomie odłożone i niezależne od kolejki powyżej. Dwie pierwsze pochodzą z sesji
2026-09-05 i nie mają wyzwalacza — można je wziąć, kiedy pasują.

1. **Testy dla `Startup/*` — zablokowane, czekają na jedną decyzję.** Sprawdzone w kodzie
   2026-09-13. Z trzech rzeczy, które ta pozycja chciała zamrozić, jedna okazała się niepotrzebna,
   a dwie nieosiągalne:

   * **Zgodność `TotalSteps` z długością tablicy** — nie ma czego zamrażać. `TotalSteps` jest
     wyliczane z długości tablicy, więc rozjechać się nie może. Pozycja wpisywała regułę, której
     złamanie nie jest dziś możliwe.
   * **Kolejność kroków** żyje w korzeniu kompozycji, a **degradacja przy błędzie** w pętli, która
     potrzebuje żywego dyspozytora Avalonii i kontrolki-gospodarza. Projekt testowy nigdy Avalonii
     nie stawiał i nie ma do tego pakietu.

   **Decyzja do podjęcia:** czy dołożyć do `tests/DungeonApp.Desktop.Tests` pakiet
   `Avalonia.Headless.XUnit` (w wersji zgodnej z resztą, 12.0.5). To standardowe narzędzie do
   testowania kodu Avalonii bez okna. Bez niego ta pozycja jest niewykonalna i lepiej ją zamknąć
   jako nierealizowalną niż zostawić jako dług, którego nikt nie może spłacić.

2. **Skala odstępów — zrobione, ile się dało; reszta czeka na decyzję o samej skali.**
   2026-09-13 przepięto osiem miejsc w trzech widokach: wszystkie, w których liczba była
   pojedyncza i trafiała dokładnie w krok skali. Wygląd nie zmienił się o piksel.

   Przegląd pozostałych trzydziestu kilku liczb dał wynik, który zmienia charakter tej pozycji:
   **nie da się ich przepiąć istniejącymi tokenami, i nie jest to kwestia staranności.** Skala ma
   wyłącznie grubości **jednorodne** (ta sama wartość na czterech krawędziach), a praktycznie każdy
   odstęp w tych widokach jest niesymetryczny — `12,0`, `28,20,28,24`, `0,0,8,0`, `14,12`. Dochodzą
   do tego wartości, których w skali po prostu nie ma: `0`, `2`, `3`, `10`, `14`, `36`, `64`
   i ujemne `-8`. Zero jest tu przypadkiem najczęstszym i najbardziej znaczącym — skala zaczyna się
   od 6, więc „brak odstępu" nie ma dziś swojego tokenu.

   Pozycja **nie jest już mechaniczna** i nie da się jej domknąć bez decyzji autora o tym, czy
   i jak skala ma się rozszerzyć — o grubości niesymetryczne, o zero, albo wcale. Dopóki ta decyzja
   nie zapadnie, liczby wpisane wprost w tych widokach są stanem docelowym, a nie długiem.
   Pełny spis pominięć z powodami jest w commicie, który tę pozycję domknął.

3. **Odrzucone paczki nigdzie się nie pokazują.** Loader je odnotowuje, ekran rejestru ich nie
   wyświetla — decyzja autora z 2026-09-12, podjęta świadomie przy poprzednim kroku. Zostaje tu
   jako jedyna pozycja z tabeli „Co się dzieje, gdy treść jest zepsuta", o której Mistrz Gry nie
   dowiaduje się z aplikacji: paczka odrzucona za literówkę w manifeście znika dziś po cichu.
   Wyzwalacz: moment, w którym autor zechce zaprojektować dla nich miejsce na ekranie.

4. **Ekran rejestru — jedna rzecz została.** Dwie z trzech rozstrzygnięte 2026-09-13.
   * **Diagnostyka loadera zostaje po angielsku** — decyzja autora. Polskie zdanie ramowe mówi
     Mistrzowi Gry, co się stało i którego pliku dotyczy; angielski szczegół zostaje śladem
     technicznym dla tego, kto pisał paczkę. Powód odrzucenia: tłumaczenie oznacza katalog
     komunikatów utrzymywany przy każdym nowym rodzaju błędu, dla tekstu widocznego wyłącznie
     wtedy, gdy treść jest zepsuta. To samo dotyczy wyjaśnienia zestawu treści przy odrzuconych
     wartościach.
   * **Nazwa pliku nie pojawia się już dwa razy** — naprawione u źródła, a nie przy wyświetlaniu.
     Diagnostyka wpisu mówi, co jest nie tak; który to plik, niesie `Location`, z którego ekran
     bierze tytuł wiersza. Odrzucenie paczki nadal nazywa plik samo, bo tam żaden tytuł wiersza
     tej roli nie przejmuje.
   * **Zostaje: nagłówek `NIE WCZYTANE` czyni pierwszy zepsuty wiersz wyższym od pozostałych**, bo
     niesie go ten wiersz, a nie prawdziwy nagłówek sekcji. Cena za „jedna lista, jeden szablon".
     Autor zostawił to świadomie 2026-09-13.

Była też z tamtej sesji trzecia pozycja — usunięcie domknięcia nad `_shell` w `App.Initialize()`.
Jest **zamknięta jako nie-dług**: stoi nadal, ale w kodzie jest opisana jako świadoma decyzja
z uzasadnieniem.

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
