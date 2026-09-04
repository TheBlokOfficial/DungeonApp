# Mapa kodu

Stan na commit 57a5d3a (2026-08-27).

Dokument opisuje stan faktyczny (kto od kogo zależy, którędy płyną dane).
Reguły normatywne żyją w `docs/architecture.md`.

## 1. Szkielet katalogów

| Katalog | Po co istnieje |
|---|---|
| `src/DungeonApp.Core` | Logika domenowa i persystencja, bez referencji do Avalonia/UI. |
| `src/DungeonApp.Core/Campaigns` | Tożsamość i tworzenie kampanii (`Campaign`, `CampaignId`, `CreateCampaign`, `ICampaignRepository`). |
| `src/DungeonApp.Core/Events` | Magistrala zdarzeń wewnątrz kampanii (`CampaignEvents`, `ICampaignEvent`). |
| `src/DungeonApp.Core/Journal` | Kronika kampanii — wpisy i interfejs magazynu (`CampaignJournal`, `JournalEntry`, `ICampaignJournalStore`). |
| `src/DungeonApp.Core/Modules` | Kontrakt modułu, katalog modułów, kolejność aktywacji (`ICampaignModule`, `ModuleCatalog`, `CampaignModules`). |
| `src/DungeonApp.Core/Modules/Clock` | Moduł zegara światowego. |
| `src/DungeonApp.Core/Modules/Dice` | Moduł rzutów kośćmi. |
| `src/DungeonApp.Core/Modules/Party` | Moduł drużyny. |
| `src/DungeonApp.Core/Modules/Scheduler` | Moduł harmonogramu zdarzeń świata. |
| `src/DungeonApp.Core/Persistence` | Jedyne miejsce z `System.IO` — zapis/odczyt kampanii i kroniki jako JSON na dysku. |
| `src/DungeonApp.Desktop` | Aplikacja Avalonia — UI, ViewModele, kompozycja aplikacji (`App.axaml.cs`). |
| `src/DungeonApp.Desktop/Assets` | Fonty (Alegreya), ikony SVG (Lucide), licencje. |
| `src/DungeonApp.Desktop/Controls` | Kontrolki wielokrotnego użytku (`Controls/Workspace`). |
| `src/DungeonApp.Desktop/Features/CampaignLibrary` | Ekran wyboru/tworzenia kampanii. |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace` | Widok roboczy otwartej kampanii: talia paneli, układ, cache przygotowania. |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Deck` | Widok talii paneli (`PanelDeckView`). |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Layout` | Zapis/odczyt układu workspace'u na dysku (`WorkspaceLayoutStore`, `WorkspaceLayoutSession`). |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Panels` | Panele per moduł Core (Clock, Dice, History, Party, Scheduler) + katalog paneli. |
| `src/DungeonApp.Desktop/Settings` | Ustawienia aplikacji na dysku (`AppSettingsStore`). |
| `src/DungeonApp.Desktop/Shell` | Powłoka okna: pasek boczny, pasek statusu, pasek górny, sesja kampanii. |
| `src/DungeonApp.Desktop/Shell/Sidebars` | Globalny pasek boczny nawigacji. |
| `src/DungeonApp.Desktop/Shell/StatusBar` | Pasek statusu. |
| `src/DungeonApp.Desktop/Shell/TopBar` | Pasek górny. |
| `src/DungeonApp.Desktop/Shell/Workspace` | Placeholder workspace'u (brak otwartej kampanii). |
| `src/DungeonApp.Desktop/Themes` | Skala UI, tokeny, style kontrolek, ikony (`Tokens.axaml`, `Icons.axaml`, `UiScaleProfiles.cs`). |
| `src/DungeonApp.Desktop/ViewModels` | Bazowe klasy ViewModel (`ObservableObject`, `AsyncCommand`). |
| `tests/DungeonApp.Core.Tests` | Testy Core: architektura, kampanie, moduły, zdarzenia, persystencja. |
| `tests/DungeonApp.Desktop.Tests` | Testy Desktop: wybór modułów w bibliotece kampanii, cache przygotowania, magazyn układu. |

## 2. Graf modułów

| ModuleId | Typ i ścieżka | StateVersion | Requires |
|---|---|---|---|
| `core.clock` | `ClockModule`, `Core/Modules/Clock/ClockModule.cs` | 1 | `[]` |
| `core.scheduler` | `SchedulerModule`, `Core/Modules/Scheduler/SchedulerModule.cs` | 1 | `[core.clock]` |
| `core.party` | `PartyModule`, `Core/Modules/Party/PartyModule.cs` | 1 | `[]` |
| `core.dice` | `DiceModule`, `Core/Modules/Dice/DiceModule.cs` | 1 | `[]` |

Rejestracja w katalogu (`App.axaml.cs`): Clock, Scheduler, Party, Dice — w tej kolejności.

### Wywołania `Get<T>()` / `TryGet<T>()`

| Wołający (plik) | Sięga po | Pokryte w `Manifest.Requires`? |
|---|---|---|
| `Core/Modules/Scheduler/SchedulerModule.cs:52` — `context.Modules.Get<ClockModule>()` | `ClockModule` | Tak — `SchedulerModule.Manifest.Requires = [ClockModule.Id]`. |

Innych wywołań `Get<T>()`/`TryGet<T>()` w `src/` brak — Desktop nie sięga do `CampaignModules.Get<T>()`, panele dostają moduł wprost przez konstruktor ViewModelu (patrz sekcja 6).

## 3. Zdarzenia

| Typ zdarzenia | Definicja | Publikuje (plik) | Konsumuje (pliki) |
|---|---|---|---|
| `WorldTimeAdvanced` | `Core/Modules/Clock/ClockModule.cs:81` | `ClockModule.cs:66` (`AdvanceTime`) | `SchedulerModule.cs:55` (`OnWorldTimeAdvanced`) |
| `ScheduledWorldEventDue` | `Core/Modules/Scheduler/SchedulerModule.cs:12` | `SchedulerModule.cs:122` (po przejściu due) | **brak konsumenta w `src/`** |

## 4. Granica Core / Desktop

Referencja projektu: `DungeonApp.Desktop.csproj` → `ProjectReference` na `DungeonApp.Core.csproj` (jedyna referencja międzyprojektowa w repo). `Core` nie referencjonuje `DungeonApp.Desktop` — brak trafień `using DungeonApp.Desktop` w `src/DungeonApp.Core`.

| Warstwa Desktop | Używane pojęcie Core |
|---|---|
| `Panels/Clock/ClockPanelViewModel.cs` | `Core.Modules.Clock` (`ClockModule`, `CampaignTime`) |
| `Panels/Dice/DicePanelViewModel.cs` | `Core.Modules.Dice` (`DiceModule`) |
| `Panels/Party/PartyPanelViewModel.cs` | `Core.Modules.Party` (`PartyModule`) |
| `Panels/Scheduler/SchedulerPanelViewModel.cs` | `Core.Modules.Clock`, `Core.Modules.Scheduler` |
| `Panels/History/HistoryPanelViewModel.cs` | `Core.Journal` (`CampaignJournal`/`JournalEntry`) |
| `App.axaml.cs`, `Shell/*`, `Features/CampaignLibrary/*` | `Core.Campaigns` (`Campaign`, `CreateCampaign`, `ICampaignRepository`), `Core.Modules` (`ModuleCatalog`), `Core.Persistence` (`JsonCampaignRepository`, `JsonCampaignJournalStore`), `Core.Journal` |

## 5. Przepływ zapisu

Kompozycja (`App.axaml.cs`, `Initialize()`):
`libraryPath = MyDocuments/DungeonApp/Campaigns` → `JsonCampaignJournalStore(libraryPath)` i `JsonCampaignRepository(libraryPath, modules, journal, TimeProvider.System)`, wstrzyknięte do `AppShellViewModel`.

Ścieżka zapisu jednej kampanii (`JsonCampaignRepository`, `Core/Persistence/JsonCampaignRepository.cs`):
- katalog kampanii: `<libraryPath>/<CampaignId:D>/`
- `campaign.json` — manifest kampanii (zapis atomowy: plik tymczasowy → `File.Move(overwrite:true)`)
- `<katalog>/modules/<ModuleId>.json` — stan każdego aktywnego modułu (`CaptureState()` → JSON), analogicznie atomowo
- `<katalog>/backups/<znacznik>/` — kopia `campaign.json` i `modules/*.json` przy operacji backupu
- kronika osobno, przez `JsonCampaignJournalStore` (`Core/Persistence/JsonCampaignJournalStore.cs`): `<katalog>/journal/<rok-miesiąc>.jsonl`, dopisywana (`File.AppendAllLinesAsync`), nigdy nadpisywana

Odczyt: `JsonCampaignRepository` czyta `campaign.json`, tworzy moduły przez `ModuleCatalog.Create`, wywołuje `RestoreState(object, version)` z danymi z `modules/<id>.json`.

Odrębnie w warstwie Desktop:
- `Settings/AppSettingsStore.cs` — `settings.json` w `%LocalAppData%/DungeonApp`
- `Features/CampaignWorkspace/Layout/WorkspaceLayoutStore.cs` — `<katalog>/layouts/<workspaceId>.json`, zapis atomowy analogiczny do repozytorium Core

### Wystąpienia `System.IO` w `src/`

| Plik | Charakter użycia |
|---|---|
| `Core/Persistence/JsonCampaignRepository.cs` | Pełny odczyt/zapis/backup na dysku — zgodne z regułą. |
| `Core/Persistence/JsonCampaignJournalStore.cs` | Pełny odczyt/dopisywanie plików kroniki — zgodne z regułą. |
| `Desktop/Settings/AppSettingsStore.cs` | Pełny odczyt/zapis `settings.json` — poza `Core/Persistence`. |
| `Desktop/Features/CampaignWorkspace/Layout/WorkspaceLayoutStore.cs` | Pełny odczyt/zapis layoutu — poza `Core/Persistence`. |
| `Desktop/App.axaml.cs` | Tylko `Path.Combine` do zbudowania ścieżek przekazywanych dalej do Core; brak `File.`/`Directory.`. |
| `Desktop/Shell/CampaignSession.cs` | Tylko `catch (IOException ...)` — brak bezpośredniego dostępu do dysku. |
| `Desktop/Features/CampaignWorkspace/Layout/WorkspaceLayoutSession.cs` | Tylko `catch (IOException ...)`. |
| `Desktop/Features/CampaignLibrary/CampaignLibraryViewModel.cs` | Tylko `catch (... or IOException ...)`. |
| `Desktop/Features/CampaignWorkspace/CampaignWorkspacePreparationCache.cs` | Tylko `catch (... or IOException ...)`. |

## 6. Warstwa Desktop

Struktura: `Shell` (powłoka okna, nawigacja, pasek statusu/góry) → `Features/CampaignLibrary` (wybór/tworzenie kampanii) → `Features/CampaignWorkspace` (talia paneli otwartej kampanii).

| Panel | ViewModel (plik) | Moduł/pojęcie Core |
|---|---|---|
| Zegar | `Panels/Clock/ClockPanelViewModel.cs` | `ClockModule` |
| Kości | `Panels/Dice/DicePanelViewModel.cs` | `DiceModule` |
| Drużyna | `Panels/Party/PartyPanelViewModel.cs` | `PartyModule` |
| Harmonogram | `Panels/Scheduler/SchedulerPanelViewModel.cs` | `SchedulerModule`, `ClockModule` |
| Historia | `Panels/History/HistoryPanelViewModel.cs` | `CampaignJournal` (`Core.Journal`) |

Katalog paneli: `Panels/PanelCatalog.cs` + `Panels/WorkspacePanelDescriptor.cs` (deskryptor: panel ↔ moduł); rejestrowanie i widoczność paneli w talii: `CampaignWorkspace/Deck/PanelDeckView.axaml.cs`.

Zasoby motywu: `Themes/Tokens.axaml` (skala/kolory/odstępy), `Themes/Icons.axaml` (ikony SVG), `Themes/BuiltInControls.axaml` i `Themes/DungeonControls.axaml` (style kontrolek), `Themes/UiScaleProfiles.cs`/`UiScaleProfile.cs` (profile skalowania UI).

## 7. Testy

| Plik testowy | Co pokrywa |
|---|---|
| `Core.Tests/Architecture/CoreIndependenceTests.cs` | Że zestaw `DungeonApp.Core` nie referencjonuje żadnego assembly Avalonia. |
| `Core.Tests/Campaigns/CampaignNameTests.cs` | Walidację `CampaignName`. |
| `Core.Tests/Campaigns/CampaignTests.cs` | Zachowanie encji `Campaign` i jej tożsamości. |
| `Core.Tests/Campaigns/CreateCampaignTests.cs` | Przypadek użycia tworzenia kampanii (`CreateCampaign`). |
| `Core.Tests/Events/CampaignEventsTests.cs` | Magistralę zdarzeń: kolejność, kaskadę, izolację między instancjami. |
| `Core.Tests/Fakes/FixedTimeProvider.cs` | Test double: zegar zamrożony na jednej chwili. |
| `Core.Tests/Fakes/InMemoryCampaignRepository.cs` | Test double: repozytorium kampanii w pamięci, zastępujące dysk w testach przypadków użycia. |
| `Core.Tests/Fakes/StubModule.cs` | Test double: moduł bez zachowania, do testów montażu zestawu modułów. |
| `Core.Tests/Fakes/TemporaryLibrary.cs` | Test double: tymczasowa biblioteka kampanii na prawdziwym systemie plików. |
| `Core.Tests/Modules/CampaignModulesTests.cs` | Aktywację zestawu modułów: kolejność topologiczna, cykl, brakująca zależność. |
| `Core.Tests/Modules/ClockModuleTests.cs` | Zachowanie `ClockModule` (upływ czasu, publikacja `WorldTimeAdvanced`). |
| `Core.Tests/Modules/DiceModuleTests.cs` | Zachowanie `DiceModule` przy ustalonym seedzie. |
| `Core.Tests/Modules/DiceNotationTests.cs` | Parsowanie notacji kości. |
| `Core.Tests/Modules/ModuleCatalogTests.cs` | `ModuleCatalog`: rejestrację, domknięcie zależności, kolejność. |
| `Core.Tests/Modules/ModuleIdTests.cs` | Walidację `ModuleId` (bezpieczeństwo jako nazwa pliku). |
| `Core.Tests/Modules/PartyModuleTests.cs` | Zachowanie `PartyModule`. |
| `Core.Tests/Modules/SchedulerModuleTests.cs` | Zachowanie `SchedulerModule`, w tym `Get<ClockModule>()` i subskrypcję `WorldTimeAdvanced`. |
| `Core.Tests/Persistence/CampaignJournalTests.cs` | Zachowanie `CampaignJournal` (dodawanie wpisów). |
| `Core.Tests/Persistence/ClockRoundTripTests.cs` | Pełny cykl `ClockModule`: utworzenie, zapis, ponowne otwarcie. |
| `Core.Tests/Persistence/DiceRoundTripTests.cs` | Pełny cykl `DiceModule` z pustym stanem przez zapis/odczyt. |
| `Core.Tests/Persistence/JsonCampaignRepositoryTests.cs` | `JsonCampaignRepository`: zapis atomowy, listowanie, backup, odmowy. |
| `Core.Tests/Persistence/ModuleStatePersistenceTests.cs` | Serializację/deserializację stanu modułu do i z JSON. |
| `Core.Tests/Persistence/PartyRoundTripTests.cs` | Pełny cykl `PartyModule` przez zapis/odczyt. |
| `Core.Tests/Persistence/SchedulerRoundTripTests.cs` | Pełny cykl dwóch współpracujących modułów (`SchedulerModule`+`ClockModule`) przez zapis/odczyt. |
| `Desktop.Tests/CampaignLibraryModuleChoiceTests.cs` | Że wybór modułów na ekranie biblioteki nigdy nie zleca Core zestawu odrzucanego przez `ModuleCatalog`. |
| `Desktop.Tests/CampaignWorkspacePreparationCacheTests.cs` | `CampaignWorkspacePreparationCache`. |
| `Desktop.Tests/WorkspaceLayoutStoreTests.cs` | `WorkspaceLayoutStore`: zapis/odczyt układu workspace'u na dysku. |
