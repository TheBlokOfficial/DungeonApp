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

> **Aktualność: 2026-09-22.** Dogoniony do etapu 1 (ekran wyboru systemu, pasek z trzema kategoriami,
> biurko i rejestr jako zakładki systemu, strona kampanii, rozgrzewka przy starcie) i etapu 2 (biurko,
> system okien, kontrolki kart i widok listy z kartą wyniesione do `DungeonApp.Library.Desktop`). Co
> dalej — [tasks.md](tasks.md), sekcja „Następne" (etap 3, zapisujące modele stanu systemu, jeszcze
> nie w kodzie).

---

## Od czego zacząć czytanie kodu

1. `src/DungeonApp.App/Program.cs` — korzeń kompozycji i jedyne miejsce wymieniające system
   z nazwy.
2. `src/DungeonApp.Desktop/App.axaml.cs` — dalszy ciąg kompozycji: repozytoria, loader treści,
   agregat katalogu typów, cache przygotowania, jawna tablica pięciu kroków startowych. Systemy
   przyjmuje przez konstruktor, nigdy ich nie odkrywa.
3. `Shell/AppShellViewModel.cs` — ekran wyboru systemu kontra pasek boczny + treść; deleguje cykl
   życia zakładek do `Shell/ActiveSystemSession.cs`.
4. `Shell/ActiveSystemSession.cs` — jedyna droga, którą zakładka systemu (i kampanii) powstaje,
   zostaje w pamięci i jest zwalniana. Bez Avalonii, testowalna bez okna.
5. `DungeonApp.Core`: `Campaign`, `CampaignInstances`, `JsonCampaignRepository` — stan, jego zmiana
   i trwały zapis.

Dalej są **dwie kompletne ścieżki pionowe**, każda od gestu do dysku. Warto przejść je w tej
kolejności, bo każda kolejna zakłada poprzednią:

| # | Ścieżka | Od czego do czego | Po co ją czytać |
|---|---|---|---|
| 1 | treść | `Core/Content/*` → `Desktop/Content/*` → `Content.Dnd5e/*` | niesie dziś największą część architektury |
| 2 | instancje | `CampaignInstances` → `InstanceResolver` → `Library.Desktop/Content/CampaignToolContext.cs` → `CampaignInstancesToolViewModel` | jedyny dziś pełny przekrój od gestu do dysku; odpowiada zarazem na pytanie, co z tego widzi MG przy stole |

Testy w `tests/` są zarazem wykonywalną specyfikacją opisanych tu zachowań.

---

## 1. Projekty i granice

Pięć projektów produkcyjnych i sześć testowych; `DungeonApp.sln` nie niesie nic poza nimi.
`tools/MockupRenderer` i `design/mockups/` nie istnieją. **`docs/images/` jest katalogiem roboczym
autora na mockupy** — leżą tam luzem, bez odsyłaczy z dokumentów, i tak ma zostać.

```
DungeonApp.Core              — nie referencuje niczego z repozytorium
DungeonApp.Desktop           — referencuje Core
DungeonApp.Library.Desktop   — referencuje Core i Desktop
DungeonApp.Content.Dnd5e     — referencuje Core, Desktop i Library.Desktop
DungeonApp.App               — referencuje Desktop i Content.Dnd5e
```

**Ta odwrotność jest sednem, nie szczegółem.** System zależy od ramy i od biblioteki, a nie
odwrotnie — i dlatego ani rama, ani biblioteka nigdy nie widzą żadnego systemu po nazwie. Gdyby
`Desktop` albo `Library.Desktop` referencjonował `Content.Dnd5e`, granica „nikt poniżej nie zna
systemów" byłaby konwencją, nie faktem. Ta sama strzałka w dół obowiązuje między `Desktop`
a `Library.Desktop`: biblioteka referencuje ramę (kontrakty zakładek, `CampaignTabContext`), rama
biblioteki nie — inaczej system, jedyne co dziś stoi nad biblioteką, mógłby wejść do ramy tylnymi
drzwiami. `Desktop` i `Library.Desktop` nie są plikami wykonywalnymi (brak `OutputType`); jedynym
`WinExe` jest `DungeonApp.App`.

| Element | Wartość |
|---|---|
| Język / TFM | C#, `<LangVersion>latest</LangVersion>`, `net10.0` we wszystkich projektach |
| `Nullable` | `enable` we wszystkich jedenastu `.csproj` |
| `TreatWarningsAsErrors` | **`true`** globalnie — ostrzeżenie zatrzymuje build; kompilator jest walidatorem treści |
| `EnforceCodeStyleInBuild` | `true` globalnie |
| UI | Avalonia **12.0.5**, compiled bindings domyślnie |
| Diagnostyka | `AvaloniaUI.DiagnosticsSupport 2.2.3`, tylko w `Debug` |
| Publish | `PublishReadyToRun = true` w `DungeonApp.App` |
| Testy | xUnit 2.9.3 w pięciu projektach; `DungeonApp.Desktop.RenderingTests` sam na xUnit v3 + `Avalonia.Headless.XUnit` — rozdzielone, bo `Avalonia.Headless.XUnit` koliduje z xUnit 2.9.3 (`CS0433` na `FactAttribute`); **brak Moq/NSubstitute/FluentAssertions** — podwójne pisane ręcznie |
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

### `src/DungeonApp.Desktop` — rama: powłoka, kontrakt systemu, motyw

| Ścieżka | Odpowiedzialność |
|---|---|
| `App.axaml.cs` | Kompozycja: repozytoria, loader, agregat katalogu typów, cache przygotowania, jawna tablica pięciu kroków startowych, `AppShellViewModel`. |
| `Content/IGameSystem.cs` | Kontrakt systemu widziany od strony ramy: typy, karty, `DisplayName`, `SystemTabs` (kategoria System) i `CampaignTabs` (kategoria Kampania). |
| `Content/ITabContent.cs`, `DelegateTabContent.cs` | Gotowa zawartość zakładki: `Control` + `IDisposable`. `DelegateTabContent` — implementacja dla zakładek bez własnego sprzątania. |
| `Content/TabDeclarations.cs` | `SystemTabDeclaration` (fabryka synchroniczna, `SystemTabContext`) i `CampaignTabDeclaration` (fabryka asynchroniczna, `CampaignTabContext`) — stałe deklarowane raz, przy wyborze systemu. |
| `Content/SystemTabContext.cs` | Wąskie okno zakładki kategorii System: tylko rejestr treści, strukturalnie bez niczego nazywającego kampanię (pilnuje `SystemTabContextIndependenceTests`). |
| `Content/CampaignTabContext.cs` | Okno zakładki kategorii Kampania: sesja kampanii, rejestr, instancje, magistrala zdarzeń, jedne drzwi zapisu. |
| `Content/ContentTypeCatalogAggregate.cs` | Agreguje listę systemów do pojedynczego katalogu typów. |
| `Shell/AppShellViewModel.cs` | Ekran wyboru systemu kontra pasek boczny + treść; sekwencja startowa; „Zmień system". Deleguje cykl życia zakładek do `ActiveSystemSession`. |
| `Shell/ActiveSystemSession.cs` | Jedna instancja na wybrany system: buduje i cache'uje zawartość zakładek obu kategorii przy pierwszym pokazaniu, otwiera/zamyka kampanię, zwalnia wszystko na powrót do wyboru i wyjście. Bez Avalonii — testowalna bez okna. |
| `Shell/CampaignSession.cs` | **Jedyna droga zmiany otwartej kampanii.** |
| `Shell/Sidebars/*` | `GlobalSidebarViewModel` — trzy kategorie (Kampania, System, Aplikacja), stan zwinięcia przeżywający zmianę systemu. `TopBar/*`, `StatusBar/*` — pasek kontekstu (w kodzie, nie na ekranie — patrz „Ocena stanu"), pasek stanu. |
| `Shell/SystemSelection/*` | Pełnoekranowy ekran wyboru systemu — pierwszy widok po starcie. |
| `Features/CampaignLibrary/CampaignLibraryViewModel` i `View` | Półka kampanii: lista, tworzenie, usuwanie, otwieranie. |
| `Features/CampaignLibrary/CampaignPageViewModel` i `View` | Strona otwartej kampanii (dawna pozycja na pasku po otwarciu) — nazwa, data, „Zamknij kampanię". |
| `Features/CampaignLibrary/CampaignPreparationCache.cs` | Cache przygotowania kampanii, dzielony przez rozgrzewkę startową i prawdziwe otwarcie. |
| `Startup/*` | Jawna, kolejnościowa tablica pięciu kroków startowych (patrz „Warstwa desktopowa"). |
| `Controls/AnchoredContentHost.cs`, `ResourceKeyToImageConverter.cs` | Ograniczenie szerokości treści; konwerter ikon — zostały w ramie, bo używa ich też ona sama, nie tylko biblioteka. |
| `Themes/*.axaml` | `Tokens` (183), `BuiltInControls` (163), `Icons` (119). `DungeonControls` skurczył się do 33 linii — style biurka i okien przeszły dosłownie do `Library.Desktop/Themes/WorkspaceControls.axaml` (251). |
| `ViewModels/ObservableObject.cs`, `AsyncCommand.cs` | Własna, minimalna infrastruktura MVVM — bez bibliotek. |

### `src/DungeonApp.Library.Desktop` — biblioteka: biurko, system okien, kontrolki kart

Nowy projekt (etap 2). Referencuje `Core` i `Desktop`; nie referencuje żadnego systemu
(`LibraryAssemblyReferenceTests`, `ContentAssemblyReferenceTests`). Zna `Entry`, nie zna `Monster`.

| Ścieżka | Odpowiedzialność |
|---|---|
| `Content/CampaignToolContext.cs` | Wąskie okno, jakie narzędzie systemu dostaje na otwartą kampanię: instancje, rejestr, resolver, magistrala zdarzeń, jedne drzwi zapisu. Budowane przez system samodzielnie z `CampaignTabContext` (ramy) + jego własnego `IContentTypeCatalog`. |
| `Features/CampaignWorkspace/CampaignDesk.cs` | **Jedyne publiczne wejście biurka.** System oddaje kontekst kampanii, magazyn układu i własną listę narzędzi; dostaje gotowe `ITabContent` z już wczytanym zapisanym układem. Nic nad tym wejściem nie czyta ani nie pisze pliku układu bezpośrednio. |
| `Features/CampaignWorkspace/CampaignWorkspaceViewModel`, `Layout/*`, `Panels/*`, `Deck/*` | Panele i ich stan, przygotowanie danych, układ (`desired`/`effective`), katalog paneli, pasek zminimalizowanych — przeniesione z ramy bez zmiany zachowania. |
| `Controls/Workspace/*` | Framework pływających okien: `PanelWindow`, `WorkspaceSurface`, arytmetyka geometrii — przeniesione z ramy. |
| `Controls/Content/TraitListView`, `ProseBlockView`, `TraitRow` | Współdzielone kontrolki karty — jedyne, z czego system komponuje wygląd wpisu. Przeniesione z ramy w całości (rama ich dziś nie ma). |
| `Features/Registry/*` | Ekran rejestru: lista wierszy, karta wybranego wpisu jako gotowy `Control` — przeniesiony z ramy; dziś jedyny konsument jest zakładką systemu D&D (patrz „Znane ograniczenie" w tasks.md). |
| `Themes/WorkspaceControls.axaml` | Style biurka i okien, wyniesione dosłownie z motywu ramy; włącza je sama biblioteka. |

### `src/DungeonApp.App` — korzeń kompozycji

`Program.cs` — `Main` + `BuildAvaloniaApp`. **Jedyne miejsce w aplikacji wymieniające system
z nazwy** (`new Dnd5eSystem()`), przez referencję projektu, nigdy przez odkrywanie w czasie
działania.

### `src/DungeonApp.Content.Dnd5e` — system D&D 5e

| Ścieżka | Odpowiedzialność |
|---|---|
| `Dnd5eSystem.cs` | **Jedyne miejsce, któremu wolno wiedzieć, czym jest potwór.** Deklaruje `monster` i `gear` (oba w wersji 1), rysuje ich karty, deklaruje `SystemTabs` („Rejestr") i `CampaignTabs` („Biurko"). Zakładka „Biurko" sama buduje `CampaignToolContext` i woła `CampaignDesk.CreateAsync` — nie ma już osobnej metody `CreateTools`, którą rama wywoływałaby za system. |
| `Monster.cs`, `Gear.cs` | Rekordy z właściwościami nazwanymi i `required` — deserializator jest **jedynym** walidatorem, zero ręcznej walidacji. `Monster.CurrentHp` wypełnia wyłącznie nakładka instancji. |
| `MonsterCardView`, `GearCardView` | Zaprojektowane karty z kontrolek biblioteki. |
| `CampaignInstancesToolView(Model)`, `InstanceRowViewModel`, `AddableEntryOption` | Okno „Świat kampanii" — jedyne dziś narzędzie biurka tego systemu, wnoszone jako panel biurka z `CampaignToolContext`. |

### `tests/`

| Projekt | Zakres |
|---|---|
| `DungeonApp.Core.Tests` | Kampanie, zdarzenia, persystencja, silnik treści, instancje. |
| `DungeonApp.Desktop.Tests` | Cache przygotowania, cykl życia zakładek (`ActiveSystemSession`), `AppShellViewModel` (część testowalna bez okna), pasek boczny (`GlobalSidebarViewModel`), krok wczytania paczek, guard na pusty katalog treści. |
| `DungeonApp.Desktop.RenderingTests` | Jedyny projekt z prawdziwym oknem: headless Avalonia (`Avalonia.Headless.XUnit`, xUnit v3), przez prawdziwy `App`. Sprawdza, że każdy wiersz paska bocznego faktycznie się rysuje (niezerowe granice) i że zwinięty pasek nie zostawia przerw między kategoriami — błędy, których żaden test na samym ViewModelu nie widzi. |
| `DungeonApp.Library.Desktop.Tests` | Biurko (`CampaignDesk`), geometria paneli, magazyn układu, widok listy rejestru — przeniesione z ramy bez zmiany zachowania; własne kopie pomocniczych klas testowych. |
| `DungeonApp.Architecture.Tests` | Granice między warstwami. Osobny projekt, bo test widzący wszystkie warstwy naraz nie może mieszkać w warstwie, którą ogranicza. |
| `DungeonApp.Content.Dnd5e.Tests` | System na prawdziwych plikach paczki; okno „Świat kampanii" na prawdziwej kampanii. |

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
nieprzejrzaną kopertę wartości; jedynym kodem, który tę kopertę otwiera, jest system nazwany
w referencji typu.

```
Core/Content            — nosi kopertę, nigdy jej nie otwiera
Desktop/Content         — agreguje wiele systemów, nadal nie otwiera
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

`IContentTypeCatalog` jest **jedynym oknem silnika na typy treści**: pyta, czy system istnieje, po
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
injection. Zero systemów jest awarią głośną: `App.Initialize()` rzuca, nazywając korzeń
kompozycji jako miejsce naprawy.

**Sekwencja startowa** to pięć kroków, w jawnej tablicy (`App.axaml.cs`), gdzie kolejność w tablicy
jest kolejnością wykonania:

1. `LoadContentPacksStep` — wczytanie i walidacja paczek treści.
2. `LoadCampaignShelfStep` — wczytanie półki kampanii.
3. `WarmCampaignDataStep` — odczyt danych pierwszej kampanii z półki (czysta praca w tle, bez UI);
   zapamiętuje jej id dla dwóch kroków niżej.
4. `WarmFrameChromeStep` — rozgrzewka wszystkiego, co GM widzi niezależnie od wybranego systemu:
   ekran wyboru, półka, pasek boczny w obu stanach zwinięcia dla **każdego** wkompilowanego systemu,
   strona kampanii (jeśli półka nie jest pusta).
5. `WarmSystemContentStep` — rozgrzewka zawartości **każdego** wkompilowanego systemu: jego
   zakładki kategorii System (z kartą pierwszego rozwiązanego wpisu każdego typu treści), jego
   zakładki kategorii Kampania (biurko wraz z narzędziami) wobec pierwszej kampanii z półki.

**Treść jest sprawdzana przed kampaniami**, a błąd dowolnego kroku **nie blokuje wejścia** —
degraduje do leniwego wczytywania z ostrzeżeniem na pasku. Każda rozgrzana kontrolka jest
egzemplarzem rzucanym, budowanym przez tymczasowy kontekst — nic z kroków 4–5 nie trafia do pamięci
podręcznej, którą później czyta prawdziwy wybór systemu (`ActiveSystemSession` buduje swój własny,
prawdziwy egzemplarz przy pierwszym pokazaniu). Cała sekwencja biegnie za pełnoekranową kurtyną
startową (`MainWindow.axaml`, `StartupCurtain`) — kurtyna znika dopiero po ostatnim kroku, więc
rozgrzewka kroków 4–5 nigdy nie jest widoczna jako zacięcie na już pokazanym oknie.

**Cykl życia zakładek — `ActiveSystemSession`.** Jedna instancja na wybrany system (tworzona
w `AppShellViewModel.ChooseSystemAsync`), właściciel dwóch słowników zawartości (System-category,
Campaign-category) i otwartej sesji kampanii:

* Zawartość zakładki powstaje **przy pierwszym pokazaniu** (`GetOrCreateSystemTab`,
  `GetOrCreateCampaignTabAsync`) i żyje w cache'u aż do zwolnienia.
* `CloseCampaign` zwalnia wszystkie zakładki kategorii Kampania i zamyka sesję — wywoływane przy
  zamknięciu kampanii i jako pierwszy krok `OpenCampaign` (otwarcie kolejnej kampanii zamyka
  poprzednią).
* `ReleaseAll` zwalnia też zakładki kategorii System i woła `CloseCampaign` — wywoływane na powrót
  do ekranu wyboru systemu („Zmień system") i przy wyjściu z programu
  (`AppShellViewModel.FlushPendingState`, spięte z `ShutdownRequested` i `MainWindow.Closing`).
* Bez Avalonii i bez maszynerii rozgrzewki — testowalne bez okna
  (`ActiveSystemSessionTests`), tak samo jak `CampaignSession`.

**Pasek boczny — trzy kategorie.** `GlobalSidebarViewModel` buduje się od nowa przy każdym wyborze
systemu (fresh instance, nie rebind — warunek animacji zwijania) z trzech list:

* **Kampania** — pozycja kampanii (półka albo strona kampanii, zależnie od `SetCampaignOpen`) plus
  zakładki `CampaignTabs` systemu, zablokowane kłódką dopóki żadna kampania nie jest otwarta.
* **System** — zakładki `SystemTabs` systemu, zawsze odblokowane.
* **Aplikacja** — dziś jeden wiersz, „Zmień system".

Stan zwinięcia (`IsCollapsed`) przeżywa zmianę systemu — trzyma go rama
(`AppShellViewModel._sidebarCollapsed`), nie żadna instancja paska, i tylko w pamięci (nic tego nie
zapisuje na dysk).

**System paneli (biurko, `Library.Desktop`).** `WorkspaceSurface` hostuje panele na płótnie;
`PanelGeometry` to cała nietrywialna arytmetyka jako czyste funkcje bez typów Avalonii.
`CampaignWorkspaceViewModel` trzyma rozdział „desired" (jedyne persystowane) i „effective" (po
dopasowaniu do rozmiaru).

**Siatka blatu.** `WorkspaceGridSettings` jest jedynym źródłem prawdy o siatce — z niej korzystają
i rysowane tło (`WorkspaceGridBackground`), i arytmetyka geometrii, więc zmiana widocznej komórki
nie może po cichu rozjechać się ze snapowaniem. Jest też modyfikator precyzji, zagęszczający krok
snapowania.

**Jak system wnosi narzędzie biurka — dziś, bez `CreateTools`.** Ten wpis w `IGameSystem` nie
istnieje; mechanizm jest jawny w fabryce zakładki:

* Zakładka kategorii Kampania systemu (`Dnd5eSystem.CreateDeskTabAsync`) sama buduje
  `CampaignToolContext` z otrzymanego `CampaignTabContext` i własnego `IContentTypeCatalog`, sama
  składa listę `WorkspacePanelDescriptor` (`BuildTools`), i sama woła **jedyne publiczne wejście**
  biurka — `CampaignDesk.CreateAsync(context, layoutStore, tools)` — które oddaje gotowe `ITabContent`
  z już wczytanym zapisanym układem.
* `PanelCatalog.For(tools)` zachowuje kształt „najpierw panele ramy, potem narzędzia systemu" jako
  krok w kodzie, mimo że lista paneli ramy jest dziś pusta — system może **tylko dołożyć**, nigdy
  wyprzeć tego, co rama oferuje sama.
* `CampaignToolContext` to **wąskie okno**, nie sesja i nie kampania: instancje, rejestr, resolver,
  magistrala zdarzeń i jedne drzwi zapisu — i nic poza tym stąd osiągalnego. Mieszka dziś
  w `Library.Desktop`, nie w ramie.
* **Narzędzie oddaje gotową kontrolkę, nie ViewModel.** Dzięki temu w bibliotece nie ma i nie musi
  być żadnego szablonu znającego typ z systemu — ta sama sztuczka, co karta zwracana jako `Control`.
* Nie ma już `CampaignToolProvider` zszywającego narzędzia kilku systemów naraz — dziś każdy system
  składa swoją własną listę narzędzi dla swojej własnej zakładki biurka.

---

## 6. Punkty rozszerzeń

### Nowy typ treści (bez zmiany silnika)

Wzorzec: `Dnd5eSystem` / `Monster` / `MonsterCardView`.

1. **Rekord wartości** w projekcie systemu: właściwości nazwane, `required` na obowiązkowych —
   deserializator jest walidatorem.
2. **Deklaracja typu** w trzech metodach systemu. To **jedyne legalne miejsce** rozgałęziania po
   identyfikatorze typu treści w całej aplikacji.
3. **Widok karty** z kontrolek karty w `DungeonApp.Library.Desktop` (`Controls/Content/*`); odstęp
   ustawia host.
4. **Testy** wzorem `Dnd5eSystemTests` — bez linijki ręcznej walidacji w kodzie produkcyjnym.
5. **Granica słownictwa poszerza się sama** — zakazane słowa są czerpane z publicznych nazw typów
   każdego załadowanego systemu, więc nowy typ dopisuje swoją nazwę bez żadnej rejestracji; skan
   dziś obejmuje `Core`, `Desktop` **i** `Library.Desktop`.

### Nowa zakładka systemu

Wzorzec: `Dnd5eSystem`'s `SystemTabs`/`CampaignTabs` w konstruktorze → `CreateRegistryTab` /
`CreateDeskTabAsync`. Osobny punkt styku od etapu 1 — każdy system deklaruje własne zakładki, rama
nigdy ich nie wybiera za niego.

1. **Deklaracja** — `SystemTabDeclaration` (kategoria System, fabryka synchroniczna, dostaje
   `SystemTabContext`: tylko rejestr) albo `CampaignTabDeclaration` (kategoria Kampania, fabryka
   asynchroniczna, dostaje `CampaignTabContext`: sesja, rejestr, instancje, zdarzenia, zapis) — id,
   tytuł, klucz ikony **z istniejącego motywu**, fabryka.
2. **Fabryka oddaje `ITabContent`** — gotowy `Control` plus `IDisposable`. Zawartość bez własnego
   sprzątania idzie przez `DelegateTabContent`; zawartość trzymająca subskrypcje implementuje
   `ITabContent` sama.
3. **Zakładka kategorii System nie dostaje kampanii** — strukturalnie, nie umownie:
   `SystemTabContext` nie ma pola, którym dałoby się ją przemycić (pilnuje
   `SystemTabContextIndependenceTests`).
4. Zwolnienie zakładki (kategoria Kampania — przy zamknięciu kampanii; obie kategorie — na powrót do
   wyboru systemu i wyjście) jest sprawą `ActiveSystemSession`, nie systemu.

### Narzędzie biurka wnoszone przez system

Wzorzec: `Dnd5eSystem.CreateDeskTabAsync` → `BuildTools` → `CampaignDesk.CreateAsync` →
`CampaignInstancesToolView(Model)`. To ścieżka dla narzędzia, które **czyta pola własnej treści po
nazwie** — i dlatego nie może mieszkać w bibliotece. Nie ma dziś osobnej metody kontraktu
(`CreateTools` nie istnieje) — system buduje narzędzia i woła `CampaignDesk` sam, wewnątrz własnej
fabryki zakładki „Biurko" (patrz „Nowa zakładka systemu" wyżej).

1. ViewModel w projekcie systemu, bez ani jednej referencji do kontrolki Avalonii, przyjmujący
   `CampaignToolContext` (`DungeonApp.Library.Desktop.Content`). Każdy zapis przez kontekst, nigdy
   przez repozytorium wprost.
2. Subskrypcja zdarzeń instancji i `IDisposable`, który je zwalnia.
3. **Widok też musi implementować `IDisposable`** i dispose'ować swój ViewModel — biurko sprząta
   ciało panelu tylko wtedy, gdy jest ono `IDisposable`, a goły `Control` nim nie jest.
4. Deskryptor z id prefiksowanym nazwą systemu, ikoną **z istniejącego motywu** i rozmiarem liczonym
   z tych samych tokenów co panele biblioteki.
5. **Żadnego wpisu w szablonach biurka** — system zwraca kontrolkę, więc biblioteka nie ma czego
   rozwiązywać.

Narzędzie potrzebujące stanu niezwiązanego z żadnym wpisem nie ma dziś gdzie go trzymać. Kształt,
w jakim taki magazyn wróci, jest zapisany w [decisions.md](decisions.md), pozycja *Utrzymanie
warstwy bloków danych po odejściu jej jedynego konsumenta*.

**Uwaga o zakresie:** katalog paneli buduje się per sesja, ale jego zawartość **nie zależy od
kampanii** — każde narzędzie każdego wkompilowanego systemu trafia na biurko każdej kampanii. Nie ma
mechanizmu włączania per kampania i manifest nie ma pola, które by go obsługiwało. Docelowo katalog
okien składa się z tego, co deklaruje wybrany system wraz z dodatkami — [architecture.md](architecture.md),
*Narzędzia biurka i system okien*.

---

## 7. Testy

**263 testy, wszystkie zielone** — Core 159, Desktop 24, Desktop.RenderingTests 5,
Library.Desktop 39, Content.Dnd5e 18, Architecture 18.

> Liczby per plik **nie są tu wypisywane celowo.** Poprzednia wersja tego dokumentu prowadziła taką
> tabelę; rozjechała się po cichu i kosztowała sesję na odtworzenie. Runner podaje je w sekundę,
> a dokument nie ma jak ich pilnować.

**Sześć granic pilnowanych mechanicznie** (sześć plików testowych plus jeden pomocniczy i jego
własny test, w `DungeonApp.Architecture.Tests/Architecture/`):

1. `Core` bez Avalonii.
2. `Core`, `Desktop` i `Library.Desktop` bez referencji do jakiegokolwiek systemu.
3. Systemy nigdy nie referencjonują się nawzajem.
4. `Core`, `Desktop` i `Library.Desktop` (`.cs` **i** `.axaml`) nie nazywają żadnego rodzaju wpisu —
   słownik zakazanych słów budowany z refleksji po publicznych typach zainstalowanych systemów, więc
   **poszerza się sam** wraz z przybywającą treścią.
5. **Nowe.** `Core` i `Desktop` (rama) nie referencują `Library.Desktop` — strzałka idzie tylko
   w dół, tak jak od systemu do ramy.
6. **Nowe.** `SystemTabContext` (zakładka kategorii System) nie wystawia niczego nazywającego
   kampanię — ani przez własność, ani przez parametr konstruktora.

To nie są testy funkcjonalności, tylko testy granic. Bez nich każda z sześciu byłaby deklaracją
w dokumentacji, a nie czymś wymuszonym przez build.

**Nowe od etapu 1–2: testy renderujące.** `DungeonApp.Desktop.RenderingTests` (headless Avalonia,
przez prawdziwy `App`) to jedyny projekt, który faktycznie stawia okno — zamknięty na dwóch rzeczach,
które żaden test na samym ViewModelu by nie złapał: że każdy wiersz paska bocznego rysuje się
z niezerowymi granicami po wyborze systemu, i że zwinięty pasek nie zostawia przerw między trzema
kategoriami. Poza tym oknem nic w warstwie Desktop / Library.Desktop nie ma testów headless.

**Czego nie pokrywa nic:** sekwencja startowa jako całość (`AppShellViewModel.RunStartupAsync`,
łącznie z tym, co robią cztery z pięciu kroków — pokryty testem jest tylko `LoadContentPacksStep`),
faktyczne przełączanie zakładek po kliknięciu (`ShowSystemTab`, `ShowCampaignTabAsync` — routing bez
Avalonii jest pokryty przez `AppShellViewModelTests` tylko dla wyboru systemu i powrotu do wyboru),
`CampaignWorkspaceViewModel` (restauracja układu, fitowanie, maksymalizacja, z/order),
`WorkspaceLayoutSession` (debounce, flush), `PanelWindow` (cała logika gestów), `TopBar` (w kodzie,
nie na ekranie — nic go nie renderuje). Z geometrii paneli pokryte jest **tylko** dopasowanie do
min/max — snapowanie, ograniczanie ruchu i maksymalizacja nie mają dedykowanych testów, mimo że to
najbardziej złożona czysta logika w bibliotece.

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
  biblioteki (choć, patrz wyżej, słabo pokryty).
* **Granice między ramą, biblioteką i systemem są dziś pilnowane mechanicznie w obie strony** —
  system nie referencjonuje ramy ani biblioteki po nazwie (stare granice), rama nie referencjonuje
  biblioteki, biblioteka nie referencjonuje systemu (nowe, etap 2). Sześć testów, nie sześć zdań
  w dokumentacji.
* **Rozgrzewka przy starcie jest dziś wyczerpująca, nie próbkowa** — obejmuje chrom ramy w obu
  stanach zwinięcia paska i zawartość każdego wkompilowanego systemu, za pełnoekranową kurtyną
  startową, która znika dopiero po ostatnim kroku. Obawa z poprzedniej sesji („czy ekran pokazuje się
  przed końcem rozgrzewki") jest w kodzie zaadresowana strukturalnie: `StartupCurtain` w
  `MainWindow.axaml` chowa całe okno, dopóki `AppShellViewModel.IsReady` nie stanie się prawdą, a to
  ustawia dopiero ostatni z pięciu kroków.

### Rusztowanie / celowo tymczasowe

* **`AllowsMultipleInstances`** — reprezentowalne w zapisanym układzie i czytane przy odtwarzaniu,
  ale żaden panel go nie ustawia. Rusztowanie opisane jako rusztowanie, nie rezerwacja: kod, który je
  czyta, istnieje i działa.
* **Katalog paneli składa się dziś w całości z narzędzi wnoszonych przez system** — biblioteka
  nie wnosi własnego. Dwustopniowy szew `PanelCatalog.For` (najpierw panele biblioteki, potem
  narzędzia systemu) zostaje w kodzie mimo pustej pierwszej listy. Rusztowanie opisane jako
  rusztowanie, nie rezerwacja: kod, który je czyta, istnieje i działa.
* **`DungeonApp.Library.Desktop` łączy dziś dwie biblioteki** — biurko z systemem okien i interfejs
  wpisów (kontrolki kart, lista z kartą), a kontekst okna narzędzia podaje instancje i rejestr, czyli
  biurko zna wpisy. Model docelowy ma osobne biblioteki, które się nie znają; rozdział w etapie 4.
* **Kontrolki karty (`Controls/Content/*`) istnieją dziś tylko w bibliotece** — poprawny stan po
  etapie 2, ale jeszcze bez drugiego systemu, który by potwierdził, że biblioteka jest dla nich
  właściwym domem, a nie tylko miejscem, do którego akurat przeniósł je jedyny istniejący system.

### Dług i luki

* **Odrzucone paczki nie docierają do użytkownika.** Loader je odnotowuje, ekran rejestru ich nie
  pokazuje — jedyna pozycja z tabeli „co się dzieje, gdy treść jest zepsuta", o której MG się nie
  dowie. Świadoma decyzja autora, ale dług zostaje długiem.
* **Pokrycie warstwy Desktop i Library.Desktop jest bardzo nierówne** — patrz sekcja „Testy" wyżej.
  Najbardziej złożona logika stanu UI jest dziś weryfikowana wyłącznie ręcznie; testy renderujące
  łapią tylko to, co ich dwa scenariusze celowo sprawdzają.
* **Brak migracji** formatu manifestu i typów treści. Strukturalnie przygotowane, ścieżki nie ma.
* **Operacja silnika bez konsumenta: zmiana nazwy własnej instancji.** Istnieje, jest przetestowana
  i **nikt jej nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, ale nie
  umie nazwać okazu, mimo że lista pokazuje właśnie nazwę własną, gdy jest. Ten sam kształt, którego
  zwyczaj tego repozytorium każe pilnować, tyle że konsument jest jednym polem tekstowym stąd.
* **Górny pasek jest w kodzie, ale nie na ekranie.** Jego model powstaje i jest podpięty do powłoki,
  widok istnieje, ale nic go nie wyświetla — okno ma tylko pasek boczny, obszar treści i pasek stanu.
  Zamknięcie kampanii żyje dziś w stronie kampanii (`CampaignPageViewModel.CloseCampaignCommand`),
  nie na pasku bocznym.
* **Rejestr rysuje karty prezentacją swojego jedynego konsumenta.** `RegistryViewModel` w bibliotece
  dostaje `IContentPresentation` jednego systemu (dziś zawsze tego samego, co go wywołał) — z drugim
  wkompilowanym systemem wpis cudzego systemu nie dostałby karty. Znane ograniczenie etapu 1, wraca
  jako pytanie „Paczka a system" w architekturze.
* **Brak obsługi błędu we/wy przy operacjach na kampanii** poza cichym połknięciem — znany, nazwany
  komentarzem w kodzie, nie zaadresowany.
* **Kontrakt „widok narzędzia musi sprzątać po sobie" nie jest zapisany w punkcie styku.** Fabryka
  treści panelu (`WorkspacePanelDescriptor.CreateContent`) oddaje `object`, a wymóg `IDisposable` dla
  widoku trzymającego subskrypcje stoi dziś wyłącznie w implementacji jednego systemu — drugi system
  pozna go dopiero przez wyciek.
