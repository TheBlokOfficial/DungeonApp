# DungeonApp — mapa kodu

**Status: opis stanu faktycznego. Nie rozstrzyga niczego.** Ten dokument mówi, **co jest w kodzie
dzisiaj** i gdzie co leży. Przy rozbieżności z kodem prawdą jest kod, a dokument jest do poprawienia.

Projekt docelowy — to, do czego kod ma dojść — opisuje [architecture.md](architecture.md). **Ten
dokument i tamten rozjeżdżają się celowo** i będą rozjeżdżać się coraz bardziej, dopóki nie zostaną
wykonane kolejne kroki z sekcji *Kolejność prac* tamtego. Rozbieżność jest planem, nie usterką; lista
różnic jest tam, nie tu.

**Dla kogo to jest — i co z tego wynika dla objętości.** Ten dokument czyta się, żeby **podjąć
decyzję**: co istnieje, czego brakuje, co jest rusztowaniem, gdzie jest dług. Nie czyta się go, żeby
**edytować plik** — do tego otwiera się źródło, gdzie uzasadnienia stoją przy kodzie i są pełniejsze
niż jakiekolwiek ich streszczenie tutaj.

> **Reguła objętości:** zostaje to, co zmienia decyzję. Wypada to, co ma znaczenie dopiero
> w chwili edytowania pliku. Kiedy dopisujesz tu akapit, sprawdź, po której stronie tej linii stoi —
> jeśli po drugiej, jego miejsce jest w komentarzu przy kodzie.

> **Aktualność: 2026-09-15.** Zaktualizowany po rozbiórce licznika i całej warstwy bloków danych —
> oba mechanizmy zniknęły z kodu, jedynym magazynem stanu kampanii są dziś instancje.

---

## Od czego zacząć czytanie kodu

1. `src/DungeonApp.App/Program.cs` — korzeń kompozycji i jedyne miejsce wymieniające zestaw treści
   z nazwy.
2. `src/DungeonApp.Desktop/App.axaml.cs` — dalszy ciąg kompozycji: repozytoria, loader treści,
   agregaty nad zestawami, cache przygotowania, tablica kroków startowych. Zestawy treści przyjmuje
   przez konstruktor, nigdy ich nie odkrywa.
3. `Shell/AppShellViewModel.cs` — przełącza między biblioteką kampanii, rejestrem treści i biurkiem.
4. `DungeonApp.Core`: `Campaign`, `CampaignInstances`, `JsonCampaignRepository` — stan, jego zmiana
   i trwały zapis.

Dalej są **dwie kompletne ścieżki pionowe**, każda od gestu do dysku. Warto przejść je w tej
kolejności, bo każda kolejna zakłada poprzednią:

| # | Ścieżka | Od czego do czego | Po co ją czytać |
|---|---|---|---|
| 1 | treść | `Core/Content/*` → `Desktop/Content/*` → `Content.Dnd5e/*` | niesie dziś największą część architektury |
| 2 | instancje | `CampaignInstances` → `InstanceResolver` → `CampaignToolContext` → `CampaignInstancesToolViewModel` | jedyny dziś pełny przekrój od gestu do dysku; odpowiada zarazem na pytanie, co z tego widzi MG przy stole |

Testy w `tests/` są zarazem wykonywalną specyfikacją opisanych tu zachowań.

---

## 1. Projekty i granice

Cztery projekty produkcyjne i cztery testowe; `DungeonApp.sln` nie niesie nic poza nimi.
`tools/MockupRenderer` i `design/mockups/` nie istnieją. **`docs/images/` jest katalogiem roboczym
autora na mockupy** — leżą tam luzem, bez odsyłaczy z dokumentów, i tak ma zostać.

```
DungeonApp.Core            — nie referencuje niczego z repozytorium
DungeonApp.Desktop         — referencuje Core
DungeonApp.Content.Dnd5e   — referencuje Core i Desktop
DungeonApp.App             — referencuje Desktop i Content.Dnd5e
```

**Ta odwrotność jest sednem, nie szczegółem.** Zestaw treści zależy od powłoki, a nie odwrotnie —
i dlatego powłoka nigdy nie widzi żadnego zestawu po nazwie. Gdyby `Desktop` referencjonował
`Content.Dnd5e`, granica „powłoka nie zna zestawów" byłaby konwencją, nie faktem. `Desktop` nie jest
plikiem wykonywalnym (brak `OutputType`); jedynym `WinExe` jest `DungeonApp.App`.

| Element | Wartość |
|---|---|
| Język / TFM | C#, `<LangVersion>latest</LangVersion>`, `net10.0` we wszystkich projektach |
| `Nullable` | `enable` we wszystkich ośmiu `.csproj` |
| `TreatWarningsAsErrors` | **`true`** globalnie — ostrzeżenie zatrzymuje build; kompilator jest walidatorem treści |
| `EnforceCodeStyleInBuild` | `true` globalnie |
| UI | Avalonia **12.0.5**, compiled bindings domyślnie |
| Diagnostyka | `AvaloniaUI.DiagnosticsSupport 2.2.3`, tylko w `Debug` |
| Publish | `PublishReadyToRun = true` w `DungeonApp.App` |
| Testy | xUnit 2.9.3; **brak Moq/NSubstitute/FluentAssertions** — podwójne pisane ręcznie |
| Kontener DI | **brak** — kompozycja ręczna, constructor injection |
| Serializacja | `System.Text.Json`, camelCase; **strict** (`UnmappedMemberHandling.Disallow`) w warstwie treści, **pobłażliwy** w manifeście kampanii |

Rozdział „strict w treści, pobłażliwy w manifeście" nie jest niekonsekwencją: to on pozwolił usunąć
dwa pola z manifestu bez podbicia wersji formatu.

---

## 2. Mapa modułów

### `src/DungeonApp.Core` — domena, zero Avalonii

| Ścieżka | Odpowiedzialność |
|---|---|
| `Campaigns/Campaign.cs` | Korzeń agregatu: tożsamość, nazwa, data, **instancje**, magistrala zdarzeń. `Create` vs `Restore` — jawne rozróżnienie „nowa" od „odtworzona z dysku". |
| `Campaigns/CampaignId.cs`, `CampaignName.cs`, `CampaignSummary.cs` | Typy wartości: trwałe ID, zwalidowana nazwa (≤100 znaków), lekki DTO do listowania. |
| `Campaigns/CreateCampaign.cs` | Jedyny use case tworzenia kampanii. |
| `Campaigns/ICampaignRepository.cs` | Port. Jedyna implementacja: `JsonCampaignRepository`. |
| `Events/CampaignEvents.cs` | Synchroniczna magistrala **per kampania** (nigdy statyczna), z limitem kaskady. |
| `Events/ICampaignEvent.cs`, `EventCascadeException.cs` | Kontrakt zdarzenia i wyjątek pętli. |
| `Content/*` — wpisy | `Pack`, `Entry`, `ContentValues` (koperta + arytmetyka nakładki), `ContentId`, `ContentTypeReference`, `ContentTypeDescriptor`, `IContentTypeCatalog`, `ContentRegistry`, `RegisteredEntry`, `EntryAddress`, `EntryUnresolvedReason`, `RejectedPack`, `RejectedEntry`, `PackVersion`, `ContentPackLoader`. |
| `Content/*` — instancje | `CampaignInstance`, `InstanceId`, `CampaignInstances`, `CampaignInstanceEvents`, `InstanceResolver`, `ResolvedInstance`, `InstanceUnresolvedReason`. |
| `Persistence/JsonCampaignRepository.cs` | Format na dysku, transakcyjność, błędy. |
| `Persistence/AtomicWrite.cs` | Zapis „obok, potem podmiana" jako **jeden** prymityw. Używają go `JsonCampaignRepository` i `WorkspaceLayoutStore`. |
| `Persistence/CampaignStoreException.cs` | `Unreadable`, `UnsupportedFormatVersion`, `Invalid`, `TornSave`. |
| `CampaignRuleException.cs` | „Reguła kampanii odmówiła" — komunikat czytany wprost przez MG. |

### `src/DungeonApp.Desktop` — powłoka, biurko, panele, motyw

| Ścieżka | Odpowiedzialność |
|---|---|
| `App.axaml.cs` | Kompozycja: repozytoria, loader, agregaty treści, cache przygotowania, kroki startowe, `AppShellViewModel`. |
| `Content/IContentSet.cs`, `IContentPresentation.cs` | Kontrakt zestawu widziany od strony powłoki: typy, karty, **pas narzędzi**. |
| `Content/CampaignToolContext.cs`, `CampaignToolProvider.cs` | Wąskie okno, jakie narzędzie zestawu dostaje na otwartą kampanię, i miejsce zszywające narzędzia wszystkich zestawów. |
| `Content/ContentTypeCatalogAggregate.cs`, `ContentPresentationAggregate.cs` | Agregują listę zestawów do pojedynczego katalogu/prezentacji. |
| `Controls/Content/TraitListView`, `ProseBlockView`, `TraitRow` | Współdzielone kontrolki karty — jedyne, z czego zestaw komponuje wygląd wpisu. |
| `Features/Registry/*` | Ekran rejestru: lista wierszy, karta wybranego wpisu jako gotowy `Control`. |
| `Shell/AppShellViewModel.cs` | Właściciel „gdzie jest MG": biblioteka / rejestr / biurko, sekwencja startowa. |
| `Shell/CampaignSession.cs` | **Jedyna droga zmiany otwartej kampanii.** |
| `Shell/Sidebars/*`, `TopBar/*`, `StatusBar/*` | Szyna nawigacji, pasek kontekstu, pasek stanu. |
| `Shell/Workspace/WorkspacePlaceholderView(Model)` | Dosłowny placeholder dla sekcji poza „Kampanie" i „Rejestr". |
| `Startup/*` | Jawna, kolejnościowa sekwencja pięciu kroków startowych. |
| `Features/CampaignLibrary/*` | Półka kampanii: lista, tworzenie, usuwanie, otwieranie. |
| `Features/CampaignWorkspace/*` | Biurko: panele i ich stan, przygotowanie danych, układ, katalog paneli, pasek zminimalizowanych. |
| `Controls/Workspace/*` | Framework pływających okien: `PanelWindow`, `WorkspaceSurface`, arytmetyka geometrii. |
| `Controls/AnchoredContentHost.cs`, `ResourceKeyToImageConverter.cs` | Ograniczenie szerokości treści; konwerter ikon. |
| `Themes/*.axaml` | `Tokens` (183), `BuiltInControls` (163), `DungeonControls` (280), `Icons` (110). Realny system wizualny, nie prowizorka. |
| `ViewModels/ObservableObject.cs`, `AsyncCommand.cs` | Własna, minimalna infrastruktura MVVM — bez bibliotek. |

### `src/DungeonApp.App` — korzeń kompozycji

`Program.cs` — `Main` + `BuildAvaloniaApp`. **Jedyne miejsce w aplikacji wymieniające zestaw treści
z nazwy** (`new Dnd5eContentSet()`), przez referencję projektu, nigdy przez odkrywanie w czasie
działania.

### `src/DungeonApp.Content.Dnd5e` — zestaw treści D&D 5e

| Ścieżka | Odpowiedzialność |
|---|---|
| `Dnd5eContentSet.cs` | **Jedyne miejsce, któremu wolno wiedzieć, czym jest potwór.** Deklaruje `monster` i `gear` (oba w wersji 1), rysuje ich karty, wnosi jedno okno biurka. |
| `Monster.cs`, `Gear.cs` | Rekordy z właściwościami nazwanymi i `required` — deserializator jest **jedynym** walidatorem, zero ręcznej walidacji. `Monster.CurrentHp` wypełnia wyłącznie nakładka instancji. |
| `MonsterCardView`, `GearCardView` | Zaprojektowane karty z współdzielonych kontrolek powłoki. |
| `CampaignInstancesToolView(Model)`, `InstanceRowViewModel`, `AddableEntryOption` | Okno „Świat kampanii". Pierwszy konsument instancji i resolvera w repozytorium. |

### `tests/`

| Projekt | Zakres |
|---|---|
| `DungeonApp.Core.Tests` | Kampanie, zdarzenia, persystencja, silnik treści, instancje. |
| `DungeonApp.Desktop.Tests` | Cache przygotowania, rejestr, krok wczytania paczek, pas narzędzi, fragment geometrii, magazyn układu, guard na pusty katalog treści. |
| `DungeonApp.Architecture.Tests` | Granice między warstwami. Osobny projekt, bo test widzący wszystkie warstwy naraz nie może mieszkać w warstwie, którą ogranicza. |
| `DungeonApp.Content.Dnd5e.Tests` | Zestaw na prawdziwych plikach paczki; okno „Świat kampanii" na prawdziwej kampanii. |

---

## 3. Model domenowy

### Kampania, zdarzenia

`Campaign` trzyma `Id`, `Name`, `CreatedAt` (ze wstrzykniętego `TimeProvider`), `Instances`
i `Events` (jedna magistrala na kampanię, z której korzysta `CampaignInstances`).

**Cykl zmiany stanu, jedyny w aplikacji:** gest → `CampaignSession.ExecuteAsync` → mutacja
w agregacie → zapis przez repozytorium → zdarzenie `Committed` → widoki odczytują stan na nowo.
Odmowa reguły i błąd zapisu na dysk są rozróżnione: pierwsza nic nie zmienia, druga zostawia zmianę
w pamięci i ostrzega MG, że nie trafiła na dysk.

### Silnik treści

Rzecz do zrozumienia przed wszystkim innym: **silnik nie wie, czym jest potwór.** Wpis niesie
nieprzejrzaną kopertę wartości; jedynym kodem, który tę kopertę otwiera, jest zestaw nazwany
w referencji typu.

```
Core/Content            — nosi kopertę, nigdy jej nie otwiera
Desktop/Content         — agreguje wiele zestawów, nadal nie otwiera
Content.Dnd5e           — jedyny kod, któremu wolno ją otworzyć
```

Paczka na dysku to katalog z `pack.json` i `entries/*.json`. Wczytywanie jest **jednoprzebiegowe** —
paczka niesie tylko wpisy, więc nie ma kolizji szablon/wpis do wykrywania.

**Trzy zakresy odrzucenia** odpowiadają tabeli z [architecture.md](architecture.md), *Co się dzieje,
gdy treść jest zepsuta*:

| Zakres | Kiedy | Gdzie ląduje |
|---|---|---|
| cała paczka | wadliwy manifest; katalog wpisów nie do wylistowania; przekroczony limit liczby plików; kolizja id z inną paczką | `RejectedPacks` |
| jeden plik | plik nie daje się przeczytać jako wpis | `RejectedEntries` |
| jeden wpis | plik poprawny, ale nie wiąże się z typem treści (cztery powody) | `Entries`, jako nierozwiązany |

`IContentTypeCatalog` jest **jedynym oknem silnika na typy treści**: pyta, czy zestaw istnieje, po
metadane typu i o werdykt walidacji — nigdy o kształt i nigdy o zmaterializowany rekord, bo silnik
nie ma typu, żeby go przyjąć.

**Karta jest gotowym `Control`-em, nie listą elementów do zinterpretowania.** Powłoka nie ma czego
przełączać po typie wpisu, bo fizycznie nie ma czego introspekcjonować. To strukturalna, nie
konwencjonalna strona zakazu introspekcji.

### Instancje w kampanii

**Wpis to esencja, instancja to okaz.** `CampaignInstance` niesie własne id, adres wpisu, nazwę
własną MG i rzadką łatkę. Instancja jest **łączem do wpisu, nie kopią jego wartości** — dlatego
poprawka wydana w paczce dociera do kampanii, które już jej używają.

`CampaignInstances` trzyma stan w prywatnym słowniku, bez settera i indeksera. Ma cztery operacje
— `Add`, `Remove`, `Relabel`, `ReplacePatch` — z których każda publikuje dokładnie jedno
zdarzenie, po zmianie stanu. **Odtwarzanie z dysku idzie osobnymi drzwiami (`Hydrate`) i nie ogłasza niczego** — wczytanie
zapisu nie jest zmianą, którą ktoś wykonał.

**Arytmetyka nakładki żyje w `ContentValues`** — nałożenie łatki, wyliczenie różnicy, zapieczętowanie
rekordu z powrotem w kopertę. Żadna z tych operacji nie zapisuje nazwy pola, nie pyta, co ta nazwa
znaczy, ani nie czyta wartości; to jest cały argument, na którym stoi to, że silnikowi wolno w ogóle
trzymać łatkę.

`InstanceResolver` jest **osobną warstwą odczytu**, pytaną od nowa przy każdym odczycie — i to
właśnie sprawia, że zainstalowanie brakującej paczki naprawia instancję bez przepisywania kampanii.
Ma cztery powody nierozwiązania, świadomie inne niż powody dla wpisu.

---

## 4. Persystencja

**Kampania** to własny katalog: manifest (`campaign.json`) i `instances/<instanceId>.json`.
Manifest niesie wersję formatu, tożsamość, datę utworzenia, licznik generacji i listę instancji —
**i ani jednego pola trzymanego otworem dla przyszłego**.

Zapis jest transakcyjny na poziomie plikowym i w całości idzie przez `AtomicWrite`: wszystko
najpierw obok, potem seria atomowych podmian, **manifest ostatni** — to on oznacza generację jako
zatwierdzoną i to na tym stoi wykrywalność rozdartego zapisu.

**Katalog `datablocks/` z kampanii założonej starszym buildem zostaje na dysku nietknięty** — ten
build go nie czyta, nie pisze i nie sprząta, bo kasowanie danych, których się już nie rozumie,
byłoby gorsze niż bezwładny katalog.

Dwie własności warte znajomości przy planowaniu:

* **Instancja nie ma trybu „nieodczytywalna"** — każdy defekt w jej pliku jest błędem magazynu.
* **Migracji nie ma.** Zbyt nowa wersja formatu manifestu jest odmową odczytu, a niezgodna wersja
  typu treści oznacza wpis jako nierozwiązany. Nic i nigdzie się nie migruje.

**Gdzie co leży:** kampanie i paczki w `Dokumenty\DungeonApp\` (dokument użytkownika — widoczny,
kopiowalny, przenośny), układy biurka w `%LocalAppData%\DungeonApp\` (stan aplikacji). Ścieżki
ustawia `App.axaml.cs`, nie `Core`.

**Układ biurka** zapisuje się przez ten sam `AtomicWrite`, z debounce'em, a jego odczyt **nigdy nie
rzuca** — czego nie da się zrozumieć, staje się układem domyślnym.

---

## 5. Warstwa desktopowa

Kompozycja biegnie przez dwa pliki i **nie ma kontenera DI** — wszystko ręcznie, constructor
injection. Zero zestawów treści jest awarią głośną: `App.Initialize()` rzuca, nazywając korzeń
kompozycji jako miejsce naprawy.

**Sekwencja startowa** to pięć kroków, w jawnej tablicy, gdzie kolejność w tablicy jest kolejnością
wykonania: wczytanie paczek → półka kampanii → rozgrzewka danych → rozgrzewka wizualna biurka →
rozgrzewka placeholdera. **Treść jest sprawdzana przed kampaniami**, a błąd dowolnego kroku **nie
blokuje wejścia** — degraduje do leniwego wczytywania z ostrzeżeniem na pasku.

**System paneli.** `WorkspaceSurface` hostuje panele na płótnie; `PanelGeometry` to cała nietrywialna
arytmetyka jako czyste funkcje bez typów Avalonii. `CampaignWorkspaceViewModel` trzyma rozdział
„desired" (jedyne persystowane) i „effective" (po dopasowaniu do rozmiaru).

**Siatka blatu.** `WorkspaceGridSettings` jest jedynym źródłem prawdy o siatce — z niej korzystają
i rysowane tło (`WorkspaceGridBackground`), i arytmetyka geometrii, więc zmiana widocznej komórki
nie może po cichu rozjechać się ze snapowaniem. Jest też modyfikator precyzji, zagęszczający krok
snapowania.

**Pas narzędzi zestawu treści** — mechanizm, którym zestaw wnosi własne okno, nie zmuszając powłoki
do poznania ani jednego swojego typu:

* `PanelCatalog` buduje się per otwarta sesja z dwustopniowym szwem: najpierw panele powłoki, za
  nimi narzędzia zestawów — kod ten kształt zachowuje, mimo że lista paneli powłoki jest dziś pusta.
  Zestaw może **tylko dołożyć**, nigdy nie wyprzeć tego, co powłoka oferuje sama.
* `CampaignToolContext` to **wąskie okno**, nie sesja i nie kampania: instancje, rejestr, resolver,
  magistrala zdarzeń i jedne drzwi zapisu — i nic poza tym stąd osiągalnego.
* **Narzędzie oddaje gotową kontrolkę, nie ViewModel.** Dzięki temu w powłoce nie ma i nie musi być
  żadnego szablonu znającego typ z zestawu — ta sama sztuczka, co karta zwracana jako `Control`.

---

## 6. Punkty rozszerzeń

### Nowy typ treści (bez zmiany silnika)

Wzorzec: `Dnd5eContentSet` / `Monster` / `MonsterCardView`.

1. **Rekord wartości** w projekcie zestawu: właściwości nazwane, `required` na obowiązkowych —
   deserializator jest walidatorem.
2. **Deklaracja typu** w trzech metodach zestawu. To **jedyne legalne miejsce** rozgałęziania po
   identyfikatorze typu treści w całej aplikacji.
3. **Widok karty** z współdzielonych kontrolek powłoki; odstęp ustawia host.
4. **Testy** wzorem `Dnd5eContentSetTests` — bez linijki ręcznej walidacji w kodzie produkcyjnym.
5. **Granica słownictwa poszerza się sama** — zakazane słowa są czerpane z publicznych nazw typów
   każdego załadowanego zestawu, więc nowy typ dopisuje swoją nazwę bez żadnej rejestracji.

### Narzędzie biurka wnoszone przez zestaw treści

Wzorzec: `Dnd5eContentSet.CreateTools` → `CampaignInstancesToolView(Model)`. To ścieżka dla
narzędzia, które **czyta pola własnej treści po nazwie** — i dlatego nie może mieszkać w powłoce.

1. ViewModel w projekcie zestawu, bez ani jednej referencji do kontrolki Avalonii, przyjmujący
   kontekst narzędzia. Każdy zapis przez kontekst, nigdy przez repozytorium wprost.
2. Subskrypcja zdarzeń instancji i `IDisposable`, który je zwalnia.
3. **Widok też musi implementować `IDisposable`** i dispose'ować swój ViewModel — biurko sprząta
   ciało panelu tylko wtedy, gdy jest ono `IDisposable`, a goły `Control` nim nie jest.
4. Deskryptor z id prefiksowanym nazwą zestawu, ikoną **z istniejącego motywu** i rozmiarem liczonym
   z tych samych tokenów co panele powłoki.
5. **Żadnego wpisu w szablonach biurka** — zestaw zwraca kontrolkę, więc powłoka nie ma czego
   rozwiązywać.

Narzędzie potrzebujące stanu niezwiązanego z żadnym wpisem nie ma dziś gdzie go trzymać. Kształt,
w jakim taki magazyn wróci, jest zapisany w [decisions.md](decisions.md), pozycja *Utrzymanie
warstwy bloków danych po odejściu jej jedynego konsumenta*.

**Uwaga o zakresie:** katalog paneli buduje się per sesja, ale jego zawartość **nie zależy od
kampanii** — każde narzędzie każdego wkompilowanego zestawu trafia na biurko każdej kampanii. Nie ma
mechanizmu włączania per kampania i manifest nie ma pola, które by go obsługiwało. To jest wprost
przesłanka pozycji „kampania wybiera zestawy przy zakładaniu" z [tasks.md](tasks.md).

---

## 7. Testy

**239 testów, wszystkie zielone** — Core 159, Desktop 49, Content.Dnd5e 18, Architecture 13.

> Liczby per plik **nie są tu wypisywane celowo.** Poprzednia wersja tego dokumentu prowadziła taką
> tabelę; rozjechała się po cichu i kosztowała sesję na odtworzenie. Runner podaje je w sekundę,
> a dokument nie ma jak ich pilnować.

**Cztery granice pilnowane mechanicznie** (pięć plików testowych plus jeden pomocniczy,
w `DungeonApp.Architecture.Tests/Architecture/`):

1. `Core` bez Avalonii.
2. `Core` i `Desktop` bez referencji do jakiegokolwiek zestawu treści.
3. Zestawy treści nigdy nie referencjonują się nawzajem.
4. `Core` i `Desktop` (`.cs` **i** `.axaml`) nie nazywają żadnego rodzaju wpisu — słownik zakazanych
   słów budowany z refleksji po publicznych typach zainstalowanych zestawów, więc **poszerza się sam**
   wraz z przybywającą treścią.

To nie są testy funkcjonalności, tylko testy granic. Bez nich każda z czterech byłaby deklaracją
w dokumentacji, a nie czymś wymuszonym przez build.

**Czego nie pokrywa nic:** `AppShellViewModel` (sekwencja startowa, przełączanie sekcji),
`CampaignWorkspaceViewModel` (restauracja układu, fitowanie, maksymalizacja, z/order),
`WorkspaceLayoutSession` (debounce, flush), `PanelWindow` (cała logika gestów — wymagałaby testów
headless, których nie ma), sidebary i paski, cztery z pięciu kroków startowych, oba agregaty nad
zestawami. Z geometrii paneli pokryte jest **tylko** dopasowanie do min/max — snapowanie,
ograniczanie ruchu i maksymalizacja nie mają dedykowanych testów, mimo że to najbardziej złożona
czysta logika w warstwie Desktop.

---

## 8. Ocena stanu

### Dojrzałe

* **Domena** — jasne rozróżnienie tworzenia, odtwarzania i hydratacji; immutability przez zamrażanie
  i normalizację; spójny model błędów. Kod jest gęsto skomentowany uzasadnieniami, nie opisami, i ten
  standard utrzymuje się także w nowszych warstwach.
* **Silnik treści** — granica „silnik nie wie, czym jest potwór" jest dziś **strukturalna**, nie
  deklarowana, i pilnowana skanem, który poszerza się sam. Deserializator jako jedyny walidator
  usunął całą klasę ręcznej walidacji.
* **Persystencja** — realna transakcyjność przez jeden wspólny prymityw zamiast trzech kopii,
  wykrywanie przerwanego zapisu, celowo łagodna degradacja.
* **Instancje i nakładka** — najmłodsza warstwa, a najściślej uzasadniona. Dwa miejsca, które
  napisane odwrotnie wyglądają identycznie, mają własne testy.
* **Geometria paneli** — czysta, bezstanowa, wolna od Avalonii. Najbardziej inżynierski fragment
  warstwy Desktop (choć, patrz wyżej, słabo pokryty).

### Rusztowanie / celowo tymczasowe

* **Nawigacja boczna** — dwie realne sekcje, reszta to dosłowny placeholder. Metoda przełączająca
  sekcje nadal nosi komentarz o tymczasowym rusztowaniu; **ma tak zostać** do przeprojektowania
  nawigacji przez autora.
* **`AllowsMultipleInstances`** — reprezentowalne w zapisanym układzie i czytane przy odtwarzaniu,
  ale żaden panel go nie ustawia. Rusztowanie opisane jako rusztowanie, nie rezerwacja: kod, który je
  czyta, istnieje i działa.
* **Katalog paneli składa się dziś w całości z narzędzi wnoszonych przez zestawy treści** — powłoka
  nie wnosi własnego. Dwustopniowy szew `PanelCatalog` (najpierw panele powłoki, potem narzędzia
  zestawów) zostaje mimo pustej pierwszej listy. Rusztowanie opisane jako rusztowanie, nie
  rezerwacja: kod, który je czyta, istnieje i działa.

### Dług i luki

* **Odrzucone paczki nie docierają do użytkownika.** Loader je odnotowuje, ekran rejestru ich nie
  pokazuje — jedyna pozycja z tabeli „co się dzieje, gdy treść jest zepsuta", o której MG się nie
  dowie. Świadoma decyzja autora, ale dług zostaje długiem.
* **Pokrycie warstwy Desktop jest bardzo nierówne** — patrz sekcja wyżej. Najbardziej złożona logika
  stanu UI jest dziś weryfikowana wyłącznie ręcznie.
* **Brak migracji** formatu manifestu i typów treści. Strukturalnie przygotowane, ścieżki nie ma.
* **Operacja silnika bez konsumenta: zmiana nazwy własnej instancji.** Istnieje, jest przetestowana
  i **nikt jej nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, ale nie
  umie nazwać okazu, mimo że lista pokazuje właśnie nazwę własną, gdy jest. Ten sam kształt, którego
  zwyczaj tego repozytorium każe pilnować, tyle że konsument jest jednym polem tekstowym stąd.
* **Brak obsługi błędu we/wy przy operacjach na kampanii** poza cichym połknięciem — znany, nazwany
  komentarzem w kodzie, nie zaadresowany.
* **Kontrakt „widok narzędzia musi sprzątać po sobie" nie jest zapisany w punkcie styku.** Fabryka
  treści panelu oddaje `object`, a wymóg `IDisposable` dla widoku trzymającego subskrypcje stoi dziś
  wyłącznie w implementacji jednego zestawu — drugi zestaw pozna go dopiero przez wyciek.
