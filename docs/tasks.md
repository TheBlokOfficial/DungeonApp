# DungeonApp — kolejka pracy

**Status: stan na 2026-09-12.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe cztery dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono.

Kolejność w sekcji „Następne" jest wiążąca tam, gdzie to zapisano. Reszta jest listą, nie planem.

Gałąź robocza: `feat/content-registry`, niescalona do `master`. Build i 216 testów zielone.

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

---

## Następne — domknięcie przejścia

Te trzy kończą to, co sesja 2026-09-12 zaczęła, i **nie powinny czekać**: dopóki stary
mechanizm leży w repozytorium bez konsumenta, czytający nie wie, który z dwóch jest prawdą.

### 1. `code-map.md` opisuje stan sprzed przejścia

Największa zaległość. Dokument opisuje warstwę treści, która już nie istnieje — szablony
z paczek, katalog elementów karty, walidację wartości wobec deklaracji pól. Wymaga pełnego
przejścia, nie łatania akapitów.

### 2. Zero zestawów treści ma być awarią głośną, nie cichą

`src/DungeonApp.Desktop/App.axaml.cs` ma bezparametrowy konstruktor `App() : this([])`, dodany
dla podglądacza XAML. Buduje aplikację z **pustym katalogiem typów** — wtedy każdy wpis jest
nierozwiązany, rejestr pusty, i **bez jednego komunikatu**. Dziś jest to nieszkodliwe przez
przypadek (narzędzia projektowe nie wołają `OnFrameworkInitializationCompleted`), a nie przez
konstrukcję.

Do zrobienia: warunek w tym konstruktorze — gdy `Avalonia.Controls.Design.IsDesignMode` jest
`false`, rzucić z jasnym komunikatem. To zasada „nic nie znika po cichu" zastosowana do samego
uruchomienia aplikacji.

### 3. Rozbiórka starego mechanizmu

Bez konsumenta po przejściu zostały:

| Gdzie | Co |
|---|---|
| `Desktop/Features/Registry/` | `CardViewModel`, `Elements/FieldValueText`, `Elements/StatblockElementViewModel`, `Elements/StatblockRowViewModel`, `Elements/ProseElementViewModel` |
| `Core/Content/` | `Template`, `CardElement`, `StatblockTrait`, `FieldDeclaration`, `FieldName`, `FieldType`, `FieldValue` |

Mają jeszcze referencje — z martwych DTO w `ContentPackLoader` i z testów starego formatu.
Usuwać razem z nimi. **Jeśli którakolwiek referencja okaże się żywa, zatrzymać się i zgłosić:**
to znaczy, że przepięcie czegoś nie domknęło, i przepisywanie żywego kodu pod rozbiórkę
zamaskowałoby ten fakt.

---

## Dalej wg dokumentu architektury

Kroki 5–9 z sekcji „Kolejność prac", w tej kolejności:

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

## Znane problemy

**Plik wykonywalny nie został potwierdzony jako linkujący się.** `dotnet build src/DungeonApp.App`
pada na `MSB3021`/`MSB3027`: podglądacz XAML z IDE (proces .NET Host) trzyma jego katalog `bin`.
**To nie jest błąd kodu** — silnik, powłoka, zestaw i wszystkie projekty testowe kompilują się
czysto. Zamknąć podgląd w IDE i zbudować ponownie. To jedyna rzecz z sesji 2026-09-12
niesprawdzona do końca.

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
