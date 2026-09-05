# Mapa kodu

Stan katalogu roboczego po blokach 3–4 (2026-09-05).

Dokument opisuje stan faktyczny (kto od kogo zależy, którędy płyną dane).
Reguły normatywne żyją w `docs/architecture.md`.

## 1. Szkielet katalogów

| Katalog | Po co istnieje |
|---|---|
| `src/DungeonApp.Core` | Logika domenowa i persystencja, bez referencji do Avalonia/UI. |
| `src/DungeonApp.Core/Campaigns` | Tożsamość, tworzenie i port zapisu kampanii (`Campaign`, `CampaignId`, `CreateCampaign`, `ICampaignRepository`). |
| `src/DungeonApp.Core/DataBlocks` | Rejestr wersjonowanych bloków, ich kształty, wartości kampanii i zdarzenie `DataBlockChanged`; także reprezentacja bloku nieczytelnego. |
| `src/DungeonApp.Core/Events` | Magistrala zdarzeń wewnątrz jednej kampanii (`CampaignEvents`, `ICampaignEvent`). |
| `src/DungeonApp.Core/Persistence` | Jedyny dostęp do dysku w Core: zapis/odczyt kampanii i wartości bloków danych jako JSON. |
| `src/DungeonApp.Core/Tools` | Kontrakt bezstanowego narzędzia (`ITool`) i pierwsze narzędzie, `Tools/Counter/CounterTool`. |
| `src/DungeonApp.Desktop` | Aplikacja Avalonia — UI, ViewModele, kompozycja aplikacji (`App.axaml.cs`). |
| `src/DungeonApp.Desktop/Assets` | Fonty (Alegreya), ikony SVG (Lucide), licencje. |
| `src/DungeonApp.Desktop/Controls` | Kontrolki wielokrotnego użytku (`Controls/Workspace`). |
| `src/DungeonApp.Desktop/Features/CampaignLibrary` | Ekran wyboru i tworzenia kampanii. |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace` | Widok roboczy otwartej kampanii: talia paneli, układ, cache przygotowania. |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Deck` | Widok talii paneli (`PanelDeckView`). |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Layout` | Zapis/odczyt układu workspace'u na dysku (`WorkspaceLayoutStore`, `WorkspaceLayoutSession`). |
| `src/DungeonApp.Desktop/Features/CampaignWorkspace/Panels` | Kontrakt i katalog paneli oraz pierwszy panel `CounterPanelView` z jego ViewModelem. |
| `src/DungeonApp.Desktop/Settings` | Ustawienia aplikacji na dysku (`AppSettingsStore`). |
| `src/DungeonApp.Desktop/Shell` | Powłoka okna: pasek boczny, pasek statusu, pasek górny, sesja kampanii. |
| `src/DungeonApp.Desktop/Shell/Sidebars` | Globalny pasek boczny nawigacji. |
| `src/DungeonApp.Desktop/Shell/StatusBar` | Pasek statusu. |
| `src/DungeonApp.Desktop/Shell/TopBar` | Pasek górny. |
| `src/DungeonApp.Desktop/Shell/Workspace` | Placeholder workspace'u (brak otwartej kampanii). |
| `src/DungeonApp.Desktop/Startup` | Sekwencja kroków startu aplikacji (`IStartupStep`, cztery kroki w kompozycji, `VisualWarmupHost`, `StartupUiContext`). Zawiera też `WarmPanelVisualStep` — ogólny mechanizm rozgrzewki pojedynczego panelu, dziś nieinstancjonowany w `App.axaml.cs`. |
| `src/DungeonApp.Desktop/Themes` | Skala UI, tokeny, style kontrolek, ikony (`Tokens.axaml`, `Icons.axaml`, `UiScaleProfiles.cs`). |
| `src/DungeonApp.Desktop/ViewModels` | Bazowe klasy ViewModel (`ObservableObject`, `AsyncCommand`). |
| `tests/DungeonApp.Core.Tests` | Testy Core: architektura, kampanie, bloki danych, zdarzenia, persystencja i narzędzie licznika. |
| `tests/DungeonApp.Desktop.Tests` | Testy Desktop: cache przygotowania, magazyn układu i ViewModel panelu licznika. |
| `tools/MockupRenderer` | Narzędzie deweloperskie poza `DungeonApp.sln`: renderuje `.axaml` z `design/mockups/` do PNG headless (Skia), z atrapą danych z JSON. Własny `README.md`. |

## 2. Graf bloków danych i narzędzi

`Campaign` tworzy własne `CampaignEvents` i `CampaignDataBlocks` przez
`Create`, a przy odczycie odtwarza je przez `Restore`. `CampaignDataBlocks`
zna `DataBlockRegistry` i magistralę: `Apply` sprawdza wynik transformacji
według kształtu, zapisuje zamrożoną wartość i publikuje `DataBlockChanged`.
`Core/Persistence` używa kontraktów kampanii i bloków danych, lecz typy z
`Core/DataBlocks` nie znają JSON.

`ITool` wymaga wyłącznie `Uses`. `CounterTool` deklaruje blok `counter`,
wersję 1 i kształt obiektu z całkowitym polem `count`; zwraca transformacje
zwiększenia lub zmniejszenia. Nie zapisuje kampanii ani nie zna innego
narzędzia.

Korzeń kompozycji (`Desktop/App.axaml.cs`) tworzy `CounterTool`, rejestruje
jego blok w `DataBlockRegistry` i sprawdza każdy identyfikator z `Uses`.
Rejestr oraz repozytorium trafiają następnie do tworzenia i otwierania
kampanii. Nie ma katalogu narzędzi, automatycznego skanowania ani aktywacji.

## 3. Zdarzenia

| Typ | Publikujący | Konsument w `src/` |
|---|---|---|
| `DataBlockChanged` | `CampaignDataBlocks.Apply` | `CounterPanelViewModel`, wyłącznie dla identyfikatora `counter` |

`CampaignEvents` jest synchroniczna i przypisana do jednej kampanii. Dopasowuje
dokładny typ zdarzenia, zachowuje kolejność subskrypcji, pracuje z limitem
kaskady i publikuje po migawce listy subskrypcji. `Subscribe` zwraca
idempotentne `IDisposable`, którego odpięcie usuwa tę konkretną rejestrację.
`DataBlockChanged` niesie tylko `DataBlockId`; panel po nim odczytuje wartość
ponownie, zamiast pobierać ją ze zdarzenia.

`Core.Tests/Events/CampaignEventsTests.cs` sprawdza mechanikę magistrali na
lokalnych typach zdarzeń. Zachowanie `DataBlockChanged` przy `Apply` pokrywają
`Core.Tests/DataBlocks/CampaignDataBlocksTests.cs` i
`Desktop.Tests/CounterPanelViewModelTests.cs`.

## 4. Granica Core / Desktop

Referencja produkcyjna: `DungeonApp.Desktop.csproj` → `ProjectReference` na
`DungeonApp.Core.csproj`. `Core` nie referencjonuje `DungeonApp.Desktop` —
brak trafień `using DungeonApp.Desktop` w `src/DungeonApp.Core`.

| Warstwa Desktop | Używane pojęcie Core |
|---|---|
| `App.axaml.cs` | `Core.Campaigns`, `Core.DataBlocks`, `Core.Persistence`, `Core.Tools.Counter`; tworzy rejestr, repozytorium i kompozycję aplikacji. |
| `Shell/*`, `Features/CampaignLibrary/*` | `Core.Campaigns` i `Core.Persistence`; `CampaignSession` przechowuje otwartą kampanię i repozytorium. |
| `Features/CampaignWorkspace/Panels/*` | `Core.DataBlocks` oraz `Core.Tools.Counter`; `CounterPanelViewModel` działa przez `CampaignSession`. |
| `Startup/*` | `Core.Campaigns` i `Core.Persistence`, aby przygotować kampanię oraz jej układ przed otwarciem. |

`CampaignWorkspaceView.axaml` wiąże `CounterPanelViewModel` z
`CounterPanelView` przez lokalny `DataTemplate`; sam widok nie odwołuje się do
typów Core.

## 5. Przepływ zapisu

Kompozycja (`App.axaml.cs`, `Initialize()`): `libraryPath =
MyDocuments/DungeonApp/Campaigns` → `JsonCampaignRepository(libraryPath,
dataBlocks)`, wstrzyknięty do `AppShellViewModel`. Ta sama instancja
`DataBlockRegistry` służy repozytorium oraz tworzeniu kampanii.

Ścieżka zapisu jednej kampanii (`JsonCampaignRepository`, `Core/Persistence/JsonCampaignRepository.cs`):
- katalog kampanii: `<libraryPath>/<CampaignId:D>/`
- `campaign.json` — manifest kampanii (zapis atomowy: plik tymczasowy → `File.Move(overwrite:true)`)
- `<katalog>/datablocks/<DataBlockId>.json` — wartość każdego zapisanego bloku, z jego wersją i numerem pokolenia; blok bez wartości nie ma pliku
- kopii zapasowych repozytorium nie tworzy — mechanizm rolujących backupów (`<katalog>/backups/...`) został usunięty wraz ze starym mechanizmem modułów
- kroniki kampanii nie ma — nie ma dziś nic w `src/`, co czytałoby albo pisało dziennik sesji

Repozytorium zapisuje najpierw pliki bloków, a manifest na końcu; wspólny numer
pokolenia wykrywa przerwany zapis. Przy odczycie buduje wartości przez
`DataBlockValueSerializer` według zarejestrowanego kształtu i przekazuje je do
`Campaign.Restore`. Blok o nieznanym identyfikatorze albo nieobsługiwanej
wersji jest oznaczany jako nieczytelny; przy kolejnym zapisie jego dotychczasowy
plik i wpis manifestu są zachowywane.

Odrębnie w warstwie Desktop:
- `Settings/AppSettingsStore.cs` — `settings.json` w `%LocalAppData%/DungeonApp`
- `Features/CampaignWorkspace/Layout/WorkspaceLayoutStore.cs` — `%LocalAppData%/DungeonApp/layouts/<workspaceId>.json`, zapis atomowy

### Wystąpienia `System.IO` w `src/`

| Plik | Charakter użycia |
|---|---|
| `Core/Persistence/JsonCampaignRepository.cs` | Pełny odczyt/zapis kampanii i bloków danych na dysku (bez backupu) — zgodne z regułą. |
| `Desktop/Settings/AppSettingsStore.cs` | Pełny odczyt/zapis `settings.json` — poza `Core/Persistence`. |
| `Desktop/Features/CampaignWorkspace/Layout/WorkspaceLayoutStore.cs` | Pełny odczyt/zapis layoutu — poza `Core/Persistence`. |
| `Desktop/App.axaml.cs` | Tylko `Path.Combine` do zbudowania ścieżek przekazywanych dalej do Core; brak `File.`/`Directory.`. |
| `Desktop/Shell/CampaignSession.cs` | Tylko `catch (IOException ...)` — brak bezpośredniego dostępu do dysku. |
| `Desktop/Features/CampaignWorkspace/Layout/WorkspaceLayoutSession.cs` | Tylko `catch (IOException ...)`. |
| `Desktop/Features/CampaignLibrary/CampaignLibraryViewModel.cs` | Tylko `catch (... or IOException ...)`. |
| `Desktop/Features/CampaignWorkspace/CampaignWorkspacePreparationCache.cs` | Tylko `catch (... or IOException ...)`. |

## 6. Warstwa Desktop

Struktura: `Shell` (powłoka okna, nawigacja, pasek statusu/góry) → `Features/CampaignLibrary` (wybór/tworzenie kampanii) → `Features/CampaignWorkspace` (talia paneli otwartej kampanii).

`PanelCatalog.For(CampaignSession)` zwraca jeden singletonowy deskryptor
`counter`. Tworzy on `CounterPanelViewModel` dla aktualnej sesji; nowy pusty
układ otwiera go na blacie, a układ zapisany przed jego dodaniem umieszcza go
w talii zminimalizowanych paneli. `CampaignWorkspaceView.axaml` ma szablon
danych, który wiąże ten ViewModel z `CounterPanelView`.

`CounterPanelViewModel` czyta blok `counter` bezpośrednio z kampanii i
subskrybuje wyłącznie `DataBlockChanged` tego identyfikatora. Dla wartości
niezapisanej pokazuje zero, a dla bloku nieczytelnego ukrywa wartość i
wyłącza oba polecenia. Oba przyciski kierują transformację z `CounterTool`
przez `CampaignSession.ExecuteAsync`, więc zapis przechodzi przez
repozytorium. Wspólny stan zajętości blokuje oba polecenia; przepełnienie nie
zmienia stanu, a błąd dysku zostawia zmianę w pamięci i komunikat. `Dispose`
odpina subskrypcję i blokuje polecenia.

Zasoby motywu: `Themes/Tokens.axaml` (skala/kolory/odstępy, w tym skala `DungeonSpacingXs..Xxxl`/`DungeonPaddingXs..Xxxl`), `Themes/Icons.axaml` (ikony SVG), `Themes/BuiltInControls.axaml` i `Themes/DungeonControls.axaml` (style kontrolek, w tym styl `ProgressBar`), `Themes/UiScaleProfiles.cs`/`UiScaleProfile.cs` (profile skalowania UI).

### Start aplikacji

Korzeń kompozycji: `App.Initialize()` buduje `CampaignWorkspacePreparationCache`, `CampaignLibraryViewModel` i jawną tablicę `IStartupStep[]` (kolejność w tablicy = kolejność wykonania); `OnFrameworkInitializationCompleted` dopiero wtedy konstruuje `AppShellViewModel`, przyjmujący te trzy jako parametry — powłoka nic z tego sama nie tworzy.

Cztery kroki w `Startup/` (kontrakt `IStartupStep`, opisany w `docs/architecture.md`): `LoadCampaignLibraryStep`, `WarmCampaignDataStep`, `WarmCampaignWorkspaceVisualStep`, `WarmWorkspacePlaceholderStep` — w tej kolejności w tablicy. Klasa `WarmPanelVisualStep` (rozgrzewka jednego typu panelu deski) istnieje w `Startup/`, ale `App.axaml.cs` nie tworzy z niej instancji. Wspólna mechanika rozgrzewki wizualnej: `VisualWarmupHost.AttachAndWaitAsync`.

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
| `Core.Tests/DataBlocks/CampaignDataBlocksTests.cs` | Odczyt i `Apply`: kształty, mrożenie, normalizację, zdarzenie oraz odmowę dla bloku nieczytelnego. |
| `Core.Tests/DataBlocks/DataBlockIdTests.cs` | Walidację i porównywanie identyfikatorów bloków. |
| `Core.Tests/DataBlocks/DataBlockRegistryTests.cs` | Rejestrację, opis, brak i duplikat bloku. |
| `Core.Tests/DataBlocks/DataBlockShapeTests.cs` | Dopasowanie kształtów pierwotnych i obiektowych. |
| `Core.Tests/Events/CampaignEventsTests.cs` | Magistralę zdarzeń: kolejność, kaskadę, izolację instancji, odpinanie i migawkę subskrypcji — na zdarzeniach zdefiniowanych lokalnie w teście. |
| `Core.Tests/Fakes/FixedTimeProvider.cs` | Test double: zegar zamrożony na jednej chwili. |
| `Core.Tests/Fakes/InMemoryCampaignRepository.cs` | Test double: repozytorium kampanii w pamięci, zastępujące dysk w testach przypadków użycia. |
| `Core.Tests/Fakes/TemporaryLibrary.cs` | Test double: tymczasowa biblioteka kampanii na prawdziwym systemie plików. |
| `Core.Tests/Persistence/DataBlockPersistenceTests.cs` | Pliki wartości bloków, pokolenia, odczyt według kształtu oraz zachowanie bloku nieczytelnego przy ponownym zapisie. |
| `Core.Tests/Persistence/JsonCampaignRepositoryTests.cs` | Manifest kampanii, listowanie, odmowy i numery pokoleń. |
| `Core.Tests/Tools/Counter/CounterToolTests.cs` | Transformacje licznika, jego deklarację `Uses` i przejście transformacji przez `Apply`. |
| `Desktop.Tests/CampaignWorkspacePreparationCacheTests.cs` | `CampaignWorkspacePreparationCache`. |
| `Desktop.Tests/CounterPanelViewModelTests.cs` | Odczyt, zdarzenie, zapis, błąd dysku, przepełnienie, blok nieczytelny, wspólne blokowanie komend i `Dispose` panelu licznika. |
| `Desktop.Tests/WorkspaceLayoutStoreTests.cs` | `WorkspaceLayoutStore`: zapis/odczyt układu workspace'u na dysku. |
