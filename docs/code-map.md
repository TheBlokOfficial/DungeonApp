# Mapa kodu

Stan na commit 3571de6 (2026-09-05).

Dokument opisuje stan faktyczny (kto od kogo zależy, którędy płyną dane).
Reguły normatywne żyją w `docs/architecture.md`.

## 1. Szkielet katalogów

| Katalog | Po co istnieje |
|---|---|
| `src/DungeonApp.Core` | Logika domenowa i persystencja, bez referencji do Avalonia/UI. |
| `src/DungeonApp.Core/Campaigns` | Tożsamość i tworzenie kampanii (`Campaign`, `CampaignId`, `CreateCampaign`, `ICampaignRepository`). |
| `src/DungeonApp.Core/Events` | Magistrala zdarzeń wewnątrz kampanii (`CampaignEvents`, `ICampaignEvent`) — dziś bez żadnego zdefiniowanego typu zdarzenia w `src/`. |
| `src/DungeonApp.Core/Modules` | Kontrakt modułu, katalog modułów, kolejność aktywacji (`ICampaignModule`, `ModuleCatalog`, `CampaignModules`) — dziś bez żadnego modułu domenowego zarejestrowanego w kompozycji. |
| `src/DungeonApp.Core/Persistence` | Jedyne miejsce z `System.IO` w Core — zapis/odczyt kampanii jako JSON na dysku. |
| `src/DungeonApp.Desktop` | Aplikacja Avalonia — UI, ViewModele, kompozycja aplikacji (`App.axaml.cs`). |
| `src/DungeonApp.Desktop/Assets` | Fonty (Alegreya), ikony SVG (Lucide), licencje. |
| `src/DungeonApp.Desktop/Controls` | Kontrolki wielokrotnego użytku (`Controls/Workspace`). |
| `src/DungeonApp.Desktop/Features/CampaignLibrary` | Ekran wyboru/tworzenia kampanii — bez sekcji wyboru modułów: tworzenie kampanii zawsze woła `CreateCampaign` z pustą listą modułów. |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace` | Widok roboczy otwartej kampanii: talia paneli, układ, cache przygotowania. |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Deck` | Widok talii paneli (`PanelDeckView`). |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Layout` | Zapis/odczyt układu workspace'u na dysku (`WorkspaceLayoutStore`, `WorkspaceLayoutSession`). |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Panels` | Ogólny kontrakt panelu i pusty katalog paneli (`PanelCatalog`, `WorkspacePanelDescriptor`, `WorkspacePanelViewModel`, `PanelActionViewModel`) — żadnego panelu per moduł Core dziś nie ma. |
| `src/DungeonApp.Desktop/Settings` | Ustawienia aplikacji na dysku (`AppSettingsStore`). |
| `src/DungeonApp.Desktop/Shell` | Powłoka okna: pasek boczny, pasek statusu, pasek górny, sesja kampanii. |
| `src/DungeonApp.Desktop/Shell/Sidebars` | Globalny pasek boczny nawigacji. |
| `src/DungeonApp.Desktop/Shell/StatusBar` | Pasek statusu. |
| `src/DungeonApp.Desktop/Shell/TopBar` | Pasek górny. |
| `src/DungeonApp.Desktop/Shell/Workspace` | Placeholder workspace'u (brak otwartej kampanii). |
| `src/DungeonApp.Desktop/Startup` | Sekwencja kroków startu aplikacji (`IStartupStep`, cztery kroki w kompozycji, `VisualWarmupHost`, `StartupUiContext`). Zawiera też `WarmPanelVisualStep` — ogólny mechanizm rozgrzewki panelu, dziś w `App.axaml.cs` nieużyty (brak paneli do rozgrzania). |
| `src/DungeonApp.Desktop/Themes` | Skala UI, tokeny, style kontrolek, ikony (`Tokens.axaml`, `Icons.axaml`, `UiScaleProfiles.cs`). |
| `src/DungeonApp.Desktop/ViewModels` | Bazowe klasy ViewModel (`ObservableObject`, `AsyncCommand`). |
| `tests/DungeonApp.Core.Tests` | Testy Core: architektura, kampanie, moduły (na atrapie `StubModule`), zdarzenia, persystencja. |
| `tests/DungeonApp.Desktop.Tests` | Testy Desktop: cache przygotowania, magazyn układu. |
| `tools/MockupRenderer` | Narzędzie deweloperskie poza `DungeonApp.sln`: renderuje `.axaml` z `design/mockups/` do PNG headless (Skia), z atrapą danych z JSON. Własny `README.md`. |

## 2. Graf modułów

Katalog modułów (`ModuleCatalog`) istnieje, ale kompozycja aplikacji
(`App.axaml.cs`) tworzy `new ModuleCatalog()` i nie rejestruje w nim ani
jednego modułu — `Register` nie jest wołane. Żaden moduł domenowy (zegar,
kości, drużyna, harmonogram) nie istnieje dziś w `src/`; jedyny typ
implementujący `ICampaignModule` w repozytorium to `StubModule` w
`tests/DungeonApp.Core.Tests/Fakes`, używany wyłącznie przez testy.

Konsekwencja: `CreateCampaign` zawsze dostaje pustą listę modułów
(`CampaignLibraryViewModel.CreateAsync` woła `_createCampaign.ExecuteAsync(NewCampaignName, [])`),
a `PanelCatalog.For(...)` zawsze zwraca pustą listę deskryptorów — patrz
sekcja 6.

### Wywołania `Get<T>()` / `TryGet<T>()`

Brak wywołań `CampaignModules.Get<T>()`/`TryGet<T>()` w `src/`. Jedyne
użycia (`Modules.Get<StubModule>()`) leżą w
`tests/DungeonApp.Core.Tests/Persistence/ModuleStatePersistenceTests.cs`.

## 3. Zdarzenia

Magistrala zdarzeń (`CampaignEvents`, `ICampaignEvent`) istnieje w
`Core/Events`, ale w `src/` nie ma dziś żadnego typu implementującego
`ICampaignEvent` — bez modułów domenowych nie ma nic, co publikowałoby albo
konsumowało zdarzenie. Tabela typ/publikujący/konsument jest dziś pusta.

`tests/DungeonApp.Core.Tests/Events/CampaignEventsTests.cs` ćwiczy magistralę
na zdarzeniach zdefiniowanych lokalnie w pliku testowym, nie na typach z `src/`.

## 4. Granica Core / Desktop

Referencja projektu: `DungeonApp.Desktop.csproj` → `ProjectReference` na `DungeonApp.Core.csproj` (jedyna referencja międzyprojektowa w repo). `Core` nie referencjonuje `DungeonApp.Desktop` — brak trafień `using DungeonApp.Desktop` w `src/DungeonApp.Core`.

| Warstwa Desktop | Używane pojęcie Core |
|---|---|
| `App.axaml.cs`, `Shell/*`, `Features/CampaignLibrary/*` | `Core.Campaigns` (`Campaign`, `CreateCampaign`, `ICampaignRepository`), `Core.Modules` (`ModuleCatalog`), `Core.Persistence` (`JsonCampaignRepository`) |
| `Startup/*` | `Core.Campaigns` (`CampaignId`, `ICampaignRepository`), do zbudowania `CampaignSession` na potrzeby rozgrzewki (`WarmCampaignWorkspaceVisualStep`) |

Żaden plik pod `Features/CampaignWorkspace/Panels` nie sięga dziś po
konkretny moduł Core — katalog paneli jest pusty (sekcja 6), więc nie ma
panelu, który mógłby to zrobić.

## 5. Przepływ zapisu

Kompozycja (`App.axaml.cs`, `Initialize()`):
`libraryPath = MyDocuments/DungeonApp/Campaigns` → `JsonCampaignRepository(libraryPath, modules)`, wstrzyknięty do `AppShellViewModel`. `ModuleCatalog` przekazany do repozytorium jest pusty (sekcja 2) — repozytorium odmówi otwarcia każdego zapisu, który wymienia jakikolwiek moduł.

Ścieżka zapisu jednej kampanii (`JsonCampaignRepository`, `Core/Persistence/JsonCampaignRepository.cs`):
- katalog kampanii: `<libraryPath>/<CampaignId:D>/`
- `campaign.json` — manifest kampanii (zapis atomowy: plik tymczasowy → `File.Move(overwrite:true)`)
- `<katalog>/modules/<ModuleId>.json` — stan każdego aktywnego modułu (`CaptureState()` → JSON), analogicznie atomowo; dziś zawsze pusty zbiór, bo żaden moduł nie jest aktywny
- kopii zapasowych repozytorium nie tworzy — mechanizm rolujących backupów (`<katalog>/backups/...`) został usunięty razem z resztą warstwy domenowej
- kroniki kampanii nie ma — `Core/Journal` (moduł, wpisy, magazyn) zniknął w całości; nie ma dziś nic w `src/`, co czytałoby albo pisało dziennik sesji

Odczyt: `JsonCampaignRepository` czyta `campaign.json`, tworzy moduły przez `ModuleCatalog.Create`, wywołuje `RestoreState(object, version)` z danymi z `modules/<id>.json`.

Odrębnie w warstwie Desktop:
- `Settings/AppSettingsStore.cs` — `settings.json` w `%LocalAppData%/DungeonApp`
- `Features/CampaignWorkspace/Layout/WorkspaceLayoutStore.cs` — `<katalog>/layouts/<workspaceId>.json`, zapis atomowy analogiczny do repozytorium Core

### Wystąpienia `System.IO` w `src/`

| Plik | Charakter użycia |
|---|---|
| `Core/Persistence/JsonCampaignRepository.cs` | Pełny odczyt/zapis kampanii i stanu modułów na dysku (bez backupu) — zgodne z regułą. |
| `Desktop/Settings/AppSettingsStore.cs` | Pełny odczyt/zapis `settings.json` — poza `Core/Persistence`. |
| `Desktop/Features/CampaignWorkspace/Layout/WorkspaceLayoutStore.cs` | Pełny odczyt/zapis layoutu — poza `Core/Persistence`. |
| `Desktop/App.axaml.cs` | Tylko `Path.Combine` do zbudowania ścieżek przekazywanych dalej do Core; brak `File.`/`Directory.`. |
| `Desktop/Shell/CampaignSession.cs` | Tylko `catch (IOException ...)` — brak bezpośredniego dostępu do dysku. |
| `Desktop/Features/CampaignWorkspace/Layout/WorkspaceLayoutSession.cs` | Tylko `catch (IOException ...)`. |
| `Desktop/Features/CampaignLibrary/CampaignLibraryViewModel.cs` | Tylko `catch (... or IOException ...)`. |
| `Desktop/Features/CampaignWorkspace/CampaignWorkspacePreparationCache.cs` | Tylko `catch (... or IOException ...)`. |

## 6. Warstwa Desktop

Struktura: `Shell` (powłoka okna, nawigacja, pasek statusu/góry) → `Features/CampaignLibrary` (wybór/tworzenie kampanii) → `Features/CampaignWorkspace` (talia paneli otwartej kampanii).

Katalog paneli (`PanelCatalog.For(CampaignSession)`) zawsze zwraca pustą
listę — komentarz w kodzie mówi to wprost: „This build ships no panels: the
descriptor set is always empty, and every campaign opens onto a bare desk
until a panel is added back.” Nie ma dziś w repozytorium ani jednego panelu
per moduł (Zegar/Kości/Drużyna/Harmonogram/Historia zniknęły wraz z modułami
i kroniką) — tabela panel↔ViewModel↔moduł jest więc pusta. Kontrakt panelu
(`WorkspacePanelDescriptor`, `WorkspacePanelViewModel`, `PanelActionViewModel`)
i widok talii (`CampaignWorkspace/Deck/PanelDeckView.axaml.cs`) zostały, ale
bez treści do wyświetlenia — kampania otwiera się na pustym biurku.

Zasoby motywu: `Themes/Tokens.axaml` (skala/kolory/odstępy, w tym skala `DungeonSpacingXs..Xxxl`/`DungeonPaddingXs..Xxxl`), `Themes/Icons.axaml` (ikony SVG), `Themes/BuiltInControls.axaml` i `Themes/DungeonControls.axaml` (style kontrolek, w tym styl `ProgressBar`), `Themes/UiScaleProfiles.cs`/`UiScaleProfile.cs` (profile skalowania UI). Istniejące widoki nie są dziś przepięte na nową skalę odstępów — liczby wpisane wprost zostały jak były.

### Start aplikacji

Korzeń kompozycji: `App.Initialize()` buduje `CampaignWorkspacePreparationCache`, `CampaignLibraryViewModel` i jawną tablicę `IStartupStep[]` (kolejność w tablicy = kolejność wykonania); `OnFrameworkInitializationCompleted` dopiero wtedy konstruuje `AppShellViewModel`, przyjmujący te trzy jako parametry — powłoka nic z tego sama nie tworzy.

Cztery kroki w `Startup/` (kontrakt `IStartupStep`, opisany w `docs/architecture.md`): `LoadCampaignLibraryStep`, `WarmCampaignDataStep`, `WarmCampaignWorkspaceVisualStep`, `WarmWorkspacePlaceholderStep` — w tej kolejności w tablicy. Klasa `WarmPanelVisualStep` (rozgrzewka jednego typu panelu deski) istnieje w `Startup/`, ale `App.axaml.cs` nie tworzy z niej żadnej instancji: bez paneli (sekcja 6) nie ma czego rozgrzewać per typ. Wspólna mechanika rozgrzewki wizualnej: `VisualWarmupHost.AttachAndWaitAsync`.

Runner: `AppShellViewModel.RunStartupAsync(StartupUiContext)`; wywołanie: `AppShellView.OnLoaded` (jeden `await`, jednorazowo, strzeżone flagą `_startupStarted`). Postęp startu (`CompletedSteps`/`TotalSteps`) liczony krokami, bez wag.

Zasłona startowa (`StartupCurtain`) mieszka w `MainWindow.axaml`, jako drugie dziecko `Panel`-a nad `shell:AppShellView` — przykrywa cały interfejs, nie tylko komórkę workspace'u. `AppShellView.axaml` trzyma już tylko niewidoczny `WarmupHost`, do którego kroki wizualne podpinają swoje kontrolki.

Dług: `CampaignLibraryViewModel` przyjmuje `Func<CampaignId, Task>` jako callback otwarcia kampanii; w `App.Initialize()` domyka się on nad polem `_shell` (`id => _shell!.OpenCampaignAsync(id)`), bo powłoka jeszcze nie istnieje w tym momencie. `AppShellViewModel.OpenCampaignAsync` jest z tego powodu `internal`, nie `private`. Brak dziś testu pokrywającego `Startup/*` w `Desktop.Tests`.

## 7. Testy

| Plik testowy | Co pokrywa |
|---|---|
| `Core.Tests/Architecture/CoreIndependenceTests.cs` | Że zestaw `DungeonApp.Core` nie referencjonuje żadnego assembly Avalonia. |
| `Core.Tests/Campaigns/CampaignNameTests.cs` | Walidację `CampaignName`. |
| `Core.Tests/Campaigns/CampaignTests.cs` | Zachowanie encji `Campaign` i jej tożsamości. |
| `Core.Tests/Campaigns/CreateCampaignTests.cs` | Przypadek użycia tworzenia kampanii (`CreateCampaign`). |
| `Core.Tests/Events/CampaignEventsTests.cs` | Magistralę zdarzeń: kolejność, kaskadę, izolację między instancjami — na zdarzeniach zdefiniowanych lokalnie w teście, nie na typach z `src/`. |
| `Core.Tests/Fakes/FixedTimeProvider.cs` | Test double: zegar zamrożony na jednej chwili. |
| `Core.Tests/Fakes/InMemoryCampaignRepository.cs` | Test double: repozytorium kampanii w pamięci, zastępujące dysk w testach przypadków użycia. |
| `Core.Tests/Fakes/StubModule.cs` | Test double: moduł bez zachowania, do testów montażu zestawu modułów i persystencji — jedyna implementacja `ICampaignModule` w repozytorium poza `src/`. |
| `Core.Tests/Fakes/TemporaryLibrary.cs` | Test double: tymczasowa biblioteka kampanii na prawdziwym systemie plików. |
| `Core.Tests/Modules/CampaignModulesTests.cs` | Aktywację zestawu modułów: kolejność topologiczna, cykl, brakująca zależność (na `StubModule`). |
| `Core.Tests/Modules/ModuleCatalogTests.cs` | `ModuleCatalog`: rejestrację, domknięcie zależności, kolejność. |
| `Core.Tests/Modules/ModuleIdTests.cs` | Walidację `ModuleId` (bezpieczeństwo jako nazwa pliku). |
| `Core.Tests/Persistence/JsonCampaignRepositoryTests.cs` | `JsonCampaignRepository`: zapis atomowy, listowanie, odmowy. |
| `Core.Tests/Persistence/ModuleStatePersistenceTests.cs` | Serializację/deserializację stanu modułu do i z JSON (na `StubModule`). |
| `Desktop.Tests/CampaignWorkspacePreparationCacheTests.cs` | `CampaignWorkspacePreparationCache`. |
| `Desktop.Tests/WorkspaceLayoutStoreTests.cs` | `WorkspaceLayoutStore`: zapis/odczyt układu workspace'u na dysku. |
