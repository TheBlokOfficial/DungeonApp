# DungeonApp — High-Level Design / Architecture Overview

Dokument opisuje **rzeczywisty** stan repozytorium na dzień jego napisania — nie plan, nie
intencję z README. Tam, gdzie kod i README się rozjeżdżają, jest to odnotowane wprost. Cel: dać
komuś (człowiekowi lub agentowi AI) możliwość oceny projektu bez czytania każdej linii kodu.

Zakres przejrzany w całości: `src/DungeonApp.Core`, `src/DungeonApp.Desktop`, `tests/`,
`tools/MockupRenderer`, pliki `.csproj`, `Directory.Build.props`, `DungeonApp.sln`.

> **Odświeżenie punktowe.** Po sesji, która dołożyła warstwę treści (paczki, rejestr, ekran
> rejestru), zaktualizowano wyłącznie fragmenty jej dotyczące: mapa modułów, sekwencja startowa,
> nawigacja i strategia testów. Reszta dokumentu nie została ponownie zaudytowana i opisuje stan
> sprzed tej sesji. Projekt docelowy warstwy treści opisuje
> [content-architecture.md](content-architecture.md).

---

## 1. Czym jest aplikacja i czym nie jest

DungeonApp to desktopowa aplikacja WPF-podobna (Avalonia) dla jednej osoby — Mistrza Gry
prowadzącego sesję RPG przy stole. Jej rolą jest utrzymywać stan jednej otwartej kampanii (na
razie: tylko licznik testowy) i zapisywać go na dysku po każdej zmianie, tak żeby nic nie
przepadło między sesjami. Działa lokalnie, na jednej maszynie, bez logowania, bez sieci, bez
wielu użytkowników — nie ma pojęcia konta, serwera ani synchronizacji.

Nie jest to: stół wirtualny (VTT) do grania online, generator treści, silnik reguł konkretnego
systemu RPG, ani aplikacja dla graczy — cały interfejs jest zaprojektowany pod jedną osobę
zarządzającą jedną kampanią naraz. W obecnym stanie to w praktyce **szkielet aplikacji desktopowej
z jednym kompletnym, ale celowo trywialnym narzędziem (licznikiem)**, który ma udowodnić, że cała
ścieżka od gestu UI do zapisu na dysku działa i jest testowalna. Docelowe „prawdziwe" narzędzia
GM-a (dziennik, NPC, zasoby itd.) nie istnieją jeszcze w kodzie.

Repozytorium zawiera też trzeci, pomocniczy projekt (`tools/MockupRenderer`) do renderowania
statycznych zrzutów `.axaml` na PNG na potrzeby projektowania UI — nie jest częścią aplikacji i nie
wchodzi w skład `DungeonApp.sln`.

---

## 2. Stos technologiczny i zależności

| Element | Wartość |
|---|---|
| Język | C#, `<LangVersion>latest</LangVersion>` (Directory.Build.props) |
| TFM wszystkich projektów | `net10.0` |
| `Nullable` | `enable` we wszystkich `.csproj` (Core, Desktop, testy, MockupRenderer) |
| `EnforceCodeStyleInBuild` | `true` (globalnie, Directory.Build.props) |
| Warnings-as-errors | **nie skonfigurowane.** Ani `Directory.Build.props`, ani żaden `.csproj` nie ustawia `TreatWarningsAsErrors`. Kod jest pisany zdyscyplinowanie, ale nic w buildzie tego nie wymusza mechanicznie — dyscyplina, nie mechanizm. |
| UI framework | Avalonia **12.0.5** (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`) |
| Compiled bindings | `AvaloniaUseCompiledBindingsByDefault = true` |
| Diagnostyka | `AvaloniaUI.DiagnosticsSupport 2.2.3`, dołączane tylko w konfiguracji `Debug` |
| Publish | `PublishReadyToRun = true` — mniej JIT-a przy pierwszym użyciu widoku w publikowanym buildzie |
| Testy | xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` — brak Moq/NSubstitute/FluentAssertions; podwójne (Fakes) pisane ręcznie |
| MockupRenderer | dodatkowo `Avalonia.Skia`, `Avalonia.Headless`, `Avalonia.Markup.Xaml.Loader` — rendering offscreen |
| Kontener DI | **brak.** Kompozycja obiektów jest ręczna, w `App.axaml.cs` (patrz sekcja 6) |
| Serializacja | wbudowany `System.Text.Json` (`System.Text.Json.Nodes`), camelCase, indentowany JSON |

Solucja `DungeonApp.sln` obejmuje 4 projekty: `DungeonApp.Desktop`, `DungeonApp.Core`,
`DungeonApp.Core.Tests`, `DungeonApp.Desktop.Tests`. `tools/MockupRenderer` żyje poza solucją
(potwierdzone komentarzem w jego `.csproj`), ale referencuje `DungeonApp.Desktop.csproj`, żeby
mockupy używały tych samych stylów/tokenów co prawdziwa aplikacja.

---

## 3. Mapa modułów

### `src/DungeonApp.Core` — domena, zero Avalonii

| Ścieżka | Odpowiedzialność |
|---|---|
| `Campaigns/Campaign.cs` | Korzeń agregatu: tożsamość, nazwa, data utworzenia, bloki danych, magistrala zdarzeń kampanii. `Create` vs `Restore` — jawne rozróżnienie „nowa" od „odtworzona z dysku". |
| `Campaigns/CampaignId.cs`, `CampaignName.cs`, `CampaignSummary.cs` | Typy wartości: trwałe ID (Guid), zwalidowana nazwa (max 100 znaków), lekki DTO do listowania (bez ładowania pełnej kampanii). |
| `Campaigns/CreateCampaign.cs` | Jedyny use case tworzenia kampanii — waliduje nazwę, tworzy agregat, zapisuje przez repozytorium. |
| `Campaigns/ICampaignRepository.cs` | Port: `SaveAsync`, `GetAsync`, `DeleteAsync`, `ListAsync`. Jedyna implementacja: `JsonCampaignRepository`. |
| `DataBlocks/CampaignDataBlocks.cs` | Jedyne miejsce trzymające i mutujące wartości bloków danych kampanii. Cała mutacja idzie przez `Apply(id, transform)` — patrz sekcja 4. |
| `DataBlocks/DataBlockRegistry.cs`, `DataBlockRegistration.cs` | Rejestr tego, co dany build „zna": id, wersja kształtu, kształt. Budowany raz, w `App.axaml.cs`. |
| `DataBlocks/DataBlockShape.cs` | Zamknięta hierarchia kształtów: `PrimitiveShape` (Integer/Fractional/Text/Boolean) i `ObjectShape` (płaski rekord pól). Kształt tylko *sprawdza* wartość, nigdy jej nie przechowuje. |
| `DataBlocks/DataBlockId.cs` | Identyfikator bloku (np. `counter`) — ograniczony znakowo, bo staje się nazwą pliku na dysku. |
| `DataBlocks/UnreadableDataBlock.cs`, `DataBlockUnreadableException.cs` | Model „ten blok istniał na dysku, ale ten build nie umie go odczytać" — nieznany rejestrowi albo nieobsługiwana wersja. |
| `DataBlocks/DataBlockShapeMismatchException.cs` | Rzucany, gdy wynik transformu nie pasuje do zarejestrowanego kształtu — poprzednia wartość zostaje nietknięta. |
| `Events/CampaignEvents.cs` | Prosta, synchroniczna magistrala zdarzeń *per kampania* (nigdy statyczna/globalna) z limitem kaskady (`MaxEventsPerCommand = 1000`). |
| `Events/ICampaignEvent.cs`, `EventCascadeException.cs` | Kontrakt zdarzenia (nazwa w czasie przeszłym) i wyjątek pętli zdarzeń. |
| `Tools/ITool.cs` | Kontrakt narzędzia: deklaruje tylko, jakich `DataBlockId` używa (`Uses`). Żadnej innej logiki w interfejsie. |
| `Content/*` | Warstwa treści: model paczki (`SystemPack`/`ContentPack`, `Template`, `Entry`), zamknięte katalogi `CardElement` (`StatblockElement`, `ProseElement`) i `FieldValue` (`TextValue`, `IntegerValue`), wczytywanie i walidacja (`ContentPackLoader`) oraz rejestr (`ContentRegistry`, `RegisteredEntry`). Paczka jest odrzucana w całości i mówi dlaczego; wpis bez szablonu zostaje w rejestrze oznaczony, wzorem `UnreadableDataBlock`. Zero wiedzy o rodzajach wpisów. |
| `Tools/Counter/CounterTool.cs` | Jedyna dziś implementacja `ITool`. Definiuje kształt bloku `counter` (pole `count`: Integer) i dwie transformacje: `Increment`/`Decrement` (z `checked` — przepełnienie rzuca `OverflowException`). |
| `Persistence/JsonCampaignRepository.cs` | Jedyna implementacja `ICampaignRepository`. Format zapisu na dysku, transakcyjność, obsługa błędów — patrz sekcja 5. |
| `Persistence/DataBlockValueSerializer.cs` | Konwersja wartość ↔ `JsonNode`, zawsze prowadzona przez `DataBlockShape` (nigdy „co się da z JSON-a wyczytać"). |
| `Persistence/CampaignStoreException.cs` | Typowany błąd repozytorium: `Unreadable`, `UnsupportedFormatVersion`, `Invalid`, `TornSave`, `UnknownModule` (ten ostatni: zdefiniowany, ale nieużywany — nie ma dziś modułów, które można by nie znać). |
| `CampaignRuleException.cs` | Wyjątek „reguła kampanii odmówiła" — komunikat czytany wprost przez GM-a (po polsku, gdy dotyczy licznika). |

**Granica:** `DungeonApp.Core.Tests/Architecture/CoreIndependenceTests.cs` odrzuca każdą referencję
`Avalonia*` w zestawie `DungeonApp.Core` — patrz sekcja 9.

### `src/DungeonApp.Desktop` — Avalonia: powłoka, biurko, panele, motyw

| Ścieżka | Odpowiedzialność |
|---|---|
| `App.axaml.cs` | **Korzeń kompozycji.** Jedyne miejsce budujące rejestr bloków danych, repozytorium, cache przygotowania kampanii, sekwencję kroków startowych i `AppShellViewModel`. Brak kontenera DI — czysty konstruktor injection. |
| `Program.cs`, `MainWindow.axaml(.cs)` | Standardowy bootstrap Avalonia; `MainWindow` to pusta powłoka bez logiki. |
| `Shell/AppShellViewModel.cs` | Właściciel „gdzie jest GM": biblioteka kampanii vs otwarte biurko, sekwencja startowa, przełączanie sekcji bocznych. |
| `Shell/CampaignSession.cs` | Jedyna droga zmiany otwartej kampanii: `ExecuteAsync(operation)` — wykonaj operację, zapisz, ogłoś `Committed`. |
| `Shell/Sidebars/*` | Globalny pasek boczny nawigacji (kolapsowalny), dziś z dwiema realnymi sekcjami („Kampanie", „Rejestr") — cokolwiek nierozpoznanego ląduje na placeholderze. |
| `Features/Registry/*` | Ekran rejestru: lista wpisów, karta wybranego wpisu składana z elementów w kolejności z szablonu, view modele elementów karty i ich widoki. Odstęp między elementami karty ustawia host, nigdy element. |
| `Shell/TopBar/*` | Pasek kontekstu: tytuł otwartej kampanii + akcja zamknięcia. |
| `Shell/StatusBar/*` | Pasek stanu na dole (komunikaty typu „Gotowe", szerokość zsynchronizowana z sidebarem). |
| `Shell/Workspace/WorkspacePlaceholderView(Model)` | Widok zastępczy dla każdej sekcji nawigacji poza „Kampanie" — dosłowny placeholder, `record WorkspacePlaceholderViewModel(string Title)`. |
| `Startup/*` | Jawna, kolejnościowa sekwencja kroków startowych (`IStartupStep`) — patrz sekcja 6 i 7a. |
| `Features/CampaignLibrary/*` | Ekran „półki" kampanii: lista, tworzenie, usuwanie, otwieranie. |
| `Features/CampaignWorkspace/*` | Biurko otwartej kampanii: `CampaignWorkspaceViewModel` (panele, ich stan, dopasowanie do rozmiaru), `CampaignWorkspacePreparation(Cache)` (odczyt danych przed zbudowaniem UI), `Layout/*` (zapis/odczyt układu biurka), `Panels/*` (katalog i implementacje paneli), `Deck/PanelDeckView` (pasek zminimalizowanych paneli). |
| `Controls/Workspace/*` | Framework „pływających okien" na płótnie: `PanelWindow` (kontrolka), `WorkspaceSurface` (`ItemsControl` hostujący panele na `Canvas`), cała arytmetyka geometrii (`PanelGeometry`, `PanelPlacement`, `PanelConstraints`, `WorkspaceMetrics(Resolver)`, `WorkspaceGridSettings`). |
| `Controls/AnchoredContentHost.cs` | Dekorator ograniczający szerokość/wysokość treści biblioteki na dużych ekranach. |
| `Controls/ResourceKeyToImageConverter.cs` | Konwerter string→zasób (ikony) dla bindingów. |
| `Themes/*.axaml` | `Tokens.axaml` (kolory/rozmiary/skale), `BuiltInControls.axaml`, `DungeonControls.axaml` (style `PanelWindow` itd.), `Icons.axaml` (zasoby ikon). |
| `ViewModels/ObservableObject.cs`, `AsyncCommand.cs` | Własna, minimalna infrastruktura MVVM — bez CommunityToolkit.Mvvm ani innej biblioteki. |

### `tests/`

| Projekt | Zakres |
|---|---|
| `DungeonApp.Core.Tests` | Domena: kampanie, bloki danych, zdarzenia, narzędzie licznika, persystencja, plus test architektoniczny. |
| `DungeonApp.Desktop.Tests` | Wąski: cache przygotowania biurka, `CounterPanelViewModel` (najlepiej pokryty plik w całym repo — 9 testów), fragment geometrii paneli, jeden test magazynu układu. |

### `tools/MockupRenderer`

CLI: `MockupRenderer <wejście.axaml> <wyjście.png> [--width=] [--height=] [--data=kontekst.json]`.
Ładuje plik `.axaml` w headless Avalonii, opcjonalnie podpina wygenerowany w locie (przez
`System.Reflection.Emit`) obiekt CLR jako `DataContext` z płaskiego JSON-a, renderuje do PNG. Służy
do produkowania zrzutów projektowych, nie jest uruchamiany jako część aplikacji ani testów.

---

## 4. Model domenowy

**Campaign** (`Campaigns/Campaign.cs`) to korzeń: `Id` (`CampaignId`, trwałe, niezależne od
nazwy), `Name` (`CampaignName`, walidowana: niepusta, ≤100 znaków), `CreatedAt` (z wstrzykniętego
`TimeProvider`, nie `DateTimeOffset.UtcNow` — testowalność czasu), `DataBlocks`
(`CampaignDataBlocks`) i `Events` (`CampaignEvents`, jedna instancja na kampanię). Dwie ścieżki
budowy: `Create` (nowa kampania, nowe ID i data) i `Restore` (odtworzenie z dysku — nie mintuje
nowej tożsamości).

**CampaignDataBlocks** to jedyne miejsce trzymające wartości bloków danych w pamięci. Kontrakt jest
celowo wąski:

```csharp
public object? Read(DataBlockId id)
public void Apply(DataBlockId id, Func<object?, object> transform)
```

`Apply` odczytuje bieżącą wartość (`null`, jeśli nic jeszcze nie zapisano), przepuszcza wynik
transformu przez `DataBlockShape.Matches`, i dopiero po pozytywnej walidacji zapisuje wartość
(„zamrożoną" — obiektowe kształty budowane jako `ImmutableDictionary`, żeby wywołujący nie mógł
zmutować tego, co trzyma repozytorium) oraz publikuje `DataBlockChanged(id)`. Błąd walidacji
(`DataBlockShapeMismatchException`) nie zmienia stanu. Odczyt/zapis nieznanego builder-owi bloku
rzuca; odczyt/zapis bloku oznaczonego jako `unreadable` (bo starsza/nieznana wersja z dysku) rzuca
`DataBlockUnreadableException` — nie ma cichego nadpisania nieodczytanej zawartości.

**Events** (`CampaignEvents`) to synchroniczna magistrala pub/sub, jedna instancja per kampania
(nigdy statyczna), dopasowanie po dokładnym typie zdarzenia, kolejność subskrypcji deterministyczna,
z twardym limitem 1000 zdarzeń na jedno polecenie (`EventCascadeException`) jako zabezpieczenie
przed pętlą, nie mechanizm produkcyjny.

**Tools** (`ITool`) to minimalny kontrakt: `IReadOnlyList<DataBlockId> Uses { get; }`. Jedyna
implementacja — `CounterTool` — deklaruje jeden blok (`counter`, kształt `{ count: Integer }`,
wersja 1) i dwie fabryki transformu (`Increment`/`Decrement`, `checked`, przepełnienie →
`OverflowException`). Narzędzie samo nie zna sesji ani UI — to warstwa Desktop (`CounterPanelViewModel`)
łączy je z konkretną kampanią.

**Cykl zmiany stanu:** gest UI → `CampaignSession.ExecuteAsync(() => campaign.DataBlocks.Apply(id, transform))`
→ w razie sukcesu `repository.SaveAsync(campaign)` → zdarzenie `Committed` na sesji → panel
odświeża się z `CampaignDataBlocks.Read`. Odmowa reguły (`CampaignRuleException`) i błąd zapisu na
dysk (`IOException`/`UnauthorizedAccessException`) są rozróżnione: pierwsza nic nie zmienia, druga
zostawia zmianę w pamięci i tylko ostrzega GM-a, że nie trafiła na dysk.

---

## 5. Persystencja — `JsonCampaignRepository`

Każda kampania to własny katalog: `<libraryPath>/<CampaignId:D>/`. W nim:

```
campaign.json                # manifest
datablocks/<blockId>.json    # jedna wartość na plik
```

**Manifest** (`CampaignManifest`, rekord wewnętrzny) niesie: `FormatVersion` (dziś zawsze `1`),
`Id`, `Name`, `CreatedAt`, zarezerwowane `Ruleset`/`ContentPacks` (zawsze `null`/`[]` — pod
przyszłość, nieużywane), `Generation` (licznik generacji zapisu) i listę `DataBlockEntry(Id,
Version, Generation)`.

**Wartość bloku** (`DataBlockDocument`) niesie `BlockId`, `Version`, `Generation` i sam `JsonNode`
zserializowany przez `DataBlockValueSerializer` **zgodnie z zarejestrowanym kształtem**, nigdy „jak
wyszło z parsera" — to jest to, co gwarantuje, że liczba całkowita nie wróci jako `double` po
przejściu przez JSON.

**Zapis (`SaveAsync`)** jest transakcyjny na poziomie plikowym: każdy plik (bloki + manifest) jest
najpierw zapisywany jako `*.writing.tmp`, dopiero po pomyślnej serializacji wszystkich plików
następuje seria atomowych `File.Move(..., overwrite: true)`, a manifest ląduje **ostatni** — to on
oznacza całą generację jako zatwierdzoną. Bloki, których build nie mógł odczytać (`unreadable`),
są przenoszone bez dotykania — ich wpis w manifeście i plik zostają dokładnie takie, jakie były.

**Odczyt (`GetAsync`)** dla każdego wpisu w manifeście: jeśli rejestr nie zna id →
`UnknownToRegistry` (nie błąd — inny build mógł mieć inny zestaw bloków); jeśli wersja się nie
zgadza → `UnsupportedVersion` (migracji dziś nie ma — świadomie odłożone, dopóki nie istnieje
pierwszy realny bump wersji); jeśli plik wartości ma inną `Generation` niż manifest →
`CampaignStoreException(TornSave)` — wykrywalny przerwany zapis. Odczytane bloki trafiają do
`Campaign.Restore` razem ze zbiorem `unreadable`.

**`ListAsync`** czyta tylko manifesty (nie wartości bloków) — jedna uszkodzona kampania nie chowa
reszty półki (łapie `CampaignStoreException` per katalog). Wynik sortowany deterministycznie
(nazwa, potem data).

**Gdzie leży biblioteka na dysku:** ustawione w `App.axaml.cs`, nie w Core —
`%USERPROFILE%\Documents\DungeonApp\Campaigns`. Świadomy wybór: kampania ma być widocznym,
przenośnym, kopiowalnym dokumentem, a nie ukrytym stanem aplikacji (`%LocalAppData%` jest użyte
tylko dla układu biurka — patrz niżej).

---

## 6. Warstwa desktopowa

**Kompozycja zależności (`App.axaml.cs`)** — brak kontenera DI, wszystko ręcznie w polach `App`:
katalog danych aplikacji (`%LocalAppData%\DungeonApp`) → `WorkspaceLayoutStore`; `CounterTool` +
`DataBlockRegistry` rejestrujący jego blok (z walidacją, że narzędzie nie używa niezarejestrowanego
bloku); katalog biblioteki kampanii (`Documents\DungeonApp\Campaigns`) →
`JsonCampaignRepository`; `CampaignWorkspacePreparationCache` (dzielony między rozgrzewkę startową
i prawdziwe otwarcie); `CampaignLibraryViewModel` — zbudowana w korzeniu kompozycji, mimo że jej
callback otwarcia kampanii domyka się nad polem `_shell`, które w tym momencie **jeszcze nie
istnieje** (rozwiązywane leniwie, dopiero przy pierwszym kliknięciu, długo po tym jak
`OnFrameworkInitializationCompleted` zdąży zbudować `_shell`). Na końcu — jawna tablica kroków
startowych, gdzie **kolejność w tablicy jest kolejnością wykonania**.

**`AppShellViewModel`** — właściciel „gdzie jest GM": `IsReady`/`IsStarting` (gate startowy),
`CurrentWorkspaceContent` (biblioteka albo `CampaignWorkspaceViewModel`, albo placeholder sekcji),
`TopBar`/`Sidebar`/`StatusBar`. `RunStartupAsync` iteruje kroki startowe sekwencyjnie, oddając
sterowanie dispatcherowi między nimi; błąd dowolnego kroku **nie blokuje wejścia** — degraduje do
zwykłego leniwego wczytywania (`CompleteStartupWithWarning`). `OpenCampaignAsync` bierze
przygotowany stan z cache'a, buduje `CampaignSession` i `CampaignWorkspaceViewModel`.
`CloseCampaignAsync` flushuje układ, dispose'uje biurko, wraca do biblioteki i odświeża
podsumowania (mogły się zmienić, gdy kampania była otwarta).

**`CampaignSession`** — jedyna droga zmiany otwartej kampanii (patrz sekcja 4). Nie jest
przechowywana w Core świadomie: „aktualnie otwarta kampania" to stan powłoki, nie domeny.

**Sekwencja startowa (`Startup/*`, `IStartupStep`)** — pięcioetapowa, cała żyje w Desktop (Core
nie wie, że start istnieje):

0. `LoadContentPacksStep` — wczytuje i waliduje paczki **przed** półką kampanii, buduje
   `ContentRegistry` i trzyma go dla powłoki. Bez własnego `try/catch`: wadliwa paczka nie jest
   wyjątkiem, tylko pozycją w `RejectedPacks`, więc „odrzucona paczka nie blokuje startu" wynika
   z kształtu loadera, a nie z łapania błędów.
1. `LoadCampaignLibraryStep` — wczytuje półkę (`ICampaignRepository.ListAsync`), trzyma wynik dla
   kolejnych kroków.
2. `WarmCampaignDataStep` — rozgrzewa dane *każdej* kampanii z półki przez
   `CampaignWorkspacePreparationCache.WarmAsync` (odczyt repozytorium + układu biurka, bez UI);
   zapamiętuje ID pierwszej kampanii z półki do rozgrzewki wizualnej.
3. `WarmCampaignWorkspaceVisualStep` — jeśli jest kampania do rozgrzania, buduje prawdziwy
   `CampaignWorkspaceViewModel` na jej danych i montuje `CampaignWorkspaceView` w niewidocznym
   hoście (`VisualWarmupHost`), żeby skompilowany XAML i pierwszy layout były gotowe **zanim** GM
   faktycznie otworzy kampanię. Bez kampanii na półce rozgrzewa sam szkielet widoku.
4. `WarmWorkspacePlaceholderStep` — rozgrzewa `WorkspacePlaceholderView` niezależnie od stanu
   półki.

Każdy krok ma `Describe()` (komunikat po polsku pokazywany przed startem), `PrepareAsync`
(praca bez UI) i `ApplyAsync` (dotknięcie żywego drzewa). Uwaga: **`WarmPanelVisualStep` istnieje
w kodzie, ale nie jest użyty w tablicy kroków w `App.axaml.cs`** — rozgrzewka per-typ-panelu jest
zaprojektowana (klasa gotowa), ale nieaktywna; komentarz w `WarmCampaignWorkspaceVisualStep`
tłumaczy dlaczego: panele na biurku pochodzą z zapisanego układu użytkownika, więc nie da się ich
wyliczyć statycznie per typ, stąd rozgrzewka bierze cały widok biurka naraz.

**`CampaignLibrary` vs `CampaignWorkspace`** — dwa odrębne ekrany podpięte pod
`AppShellViewModel.CurrentWorkspaceContent`. Biblioteka to płaski ekran „zaplecza" (lista, tworzenie,
usuwanie, otwieranie kampanii) bez żadnej logiki domenowej poza wywołaniem `CreateCampaign`/
`ICampaignRepository`. Biurko (`CampaignWorkspaceViewModel`) to właściwy „stół" z panelami.

**System paneli:**
- `WorkspaceSurface` — `ItemsControl`, którego kontener *jest* panelem (`PanelWindow`), a nie
  opakowuje go w `ContentPresenter` — świadomy wybór, żeby wiązania `Canvas.Left`/`Top` mogły być
  ustawiane bezpośrednio na tym samym obiekcie z priorytetem `LocalValue` (przeżywają gest
  przeciągnięcia; deklaratywny `ItemContainerTheme` by tego nie przeżył).
- `PanelGeometry` — cała nietrywialna arytmetyka (snapowanie do siatki, ograniczanie do
  min/max, dopasowanie do zmiany rozmiaru powierzchni, maksymalizacja) jako czyste funkcje
  statyczne, bez typów Avalonii — testowalne bez UI.
- `PanelPlacement` — prostokąt w logicznych jednostkach (DIP), niezależny od Avalonii.
- `PanelDeck` (`Features/CampaignWorkspace/Deck/PanelDeckView`) — pasek zminimalizowanych paneli.
- `PanelCatalog` — jedyne miejsce wymieniające identyfikatory paneli; buduje się per otwarta sesja
  kampanii (`PanelCatalog.For(session)`); dziś zawiera dokładnie jeden wpis: licznik.

`CampaignWorkspaceViewModel` trzyma rozdział „desired" (ostatnie jawne ustawienie użytkownika,
jedyne persystowane) i „effective" (to, co faktycznie widać po dopasowaniu do bieżącego rozmiaru
powierzchni) — to właśnie to sprawia, że zmniejszenie i ponowne powiększenie okna jest
nie-destrukcyjne.

**`WorkspaceLayoutStore`** — JSON per biurko (`%LocalAppData%\DungeonApp\layouts\<sanitized-id>.json`),
własny zapis atomowy (temp + move), wersjonowany dokument. Odczyt nigdy nie rzuca — nieznana
wersja/uszkodzony plik = `WorkspaceLayout.Empty` (użyj domyślnych). Zapisy są debounce'owane
(`WorkspaceLayoutSession`, 750 ms) i odpalane na wątku UI (uzasadnione w komentarzu: plik jest mały,
snapshot musi czytać stan ViewModelu bezpośrednio).

**Motyw (`Themes/*.axaml`)** — `Tokens.axaml` (kolory/skale/rozmiary jako zasoby), `BuiltInControls.axaml`
i `DungeonControls.axaml` (style kontrolek, w tym `PanelWindow`), `Icons.axaml`. Rozmiar plików
(163–280 linii) sugeruje realnie rozbudowany system wizualny, nie prowizorkę.

---

## 7. Kluczowe przepływy end-to-end

### (a) Start aplikacji
`Program.Main` → `AppBuilder.Configure<App>()...StartWithClassicDesktopLifetime` → `App.Initialize()`
buduje wszystkie zależności i tablicę `IStartupStep[]` (patrz sekcja 6) → `App.OnFrameworkInitializationCompleted`
tworzy `AppShellViewModel`, ustawia je jako `DataContext` `MainWindow`, podpina flush układu na
`ShutdownRequested`/`Closing`. `AppShellView.OnLoaded` (odpalane raz, po pierwszym renderze lekkiej
powłoki) woła `viewModel.RunStartupAsync(new StartupUiContext(WarmupHost))`, który iteruje cztery
kroki startowe sekwencyjnie, oddając sterowanie dispatcherowi między nimi (pasek postępu
`CompletedSteps`/`TotalSteps` się aktualizuje). Błąd dowolnego kroku → `IsReady = true` mimo
wszystko, z komunikatem ostrzegawczym w `StatusBar`.

### (b) Otwarcie kampanii
Kliknięcie wiersza w `CampaignLibraryView` → `CampaignRowViewModel.OpenCommand` →
`CampaignLibraryViewModel.OpenAsync` → callback wstrzyknięty z korzenia kompozycji →
`AppShellViewModel.OpenCampaignAsync(id)` → `CampaignWorkspacePreparationCache.TakeAsync(id)`
(zwraca to, co rozgrzewka startowa już przygotowała, albo czyta na żądanie — `Campaign` +
`WorkspaceLayout`) → `new CampaignSession(campaign, repository)` → `new CampaignWorkspaceViewModel(...)`
(restauruje panele z zapisanego układu przez `PanelCatalog`) → `AppShellViewModel.CurrentWorkspaceContent`
przełącza się na biurko, `TopBar`/`Sidebar`/`StatusBar` aktualizują tytuł/kontekst.

### (c) Inkrementacja licznika i jej zapis na dysk
Klik przycisku „+" w `CounterPanelView` → `CounterPanelViewModel.IncrementCommand` (blokowany, gdy
`!CanChange`) → `ChangeAsync(_tool.Increment(), overflowMessage)` →
`CampaignSession.ExecuteAsync(() => campaign.DataBlocks.Apply(CounterTool.DataBlockId, transform))`
→ wewnątrz `Apply`: odczyt bieżącej wartości, `checked(count + 1)` (może rzucić `OverflowException`,
wtedy nic się nie zapisuje), walidacja kształtu, zapis w pamięci, publikacja `DataBlockChanged` →
`CampaignSession` woła `repository.SaveAsync(campaign)` (`JsonCampaignRepository` — patrz sekcja 5)
→ sukces: zdarzenie `Committed`; błąd IO: `Committed` mimo to (zmiana stoi w pamięci), ale
`CounterPanelViewModel.Message` pokazuje po polsku, że zapis się nie udał. Panel jest już
zasubskrybowany na `DataBlockChanged` (`OnDataBlockChanged`), więc `Refresh()` przeczytuje nową
wartość z powrotem z `CampaignDataBlocks.Read` niezależnie od tego, kto wywołał zmianę.

### (d) Przesunięcie/zadokowanie panelu i zapamiętanie układu
Naciśnięcie paska tytułu `PanelWindow` → `OnGesturePartPointerPressed` przechwytuje wskaźnik,
zapamiętuje `_pressPlacement` i krawędź (`PanelEdge.None` dla przesunięcia) → `OnPointerMoved`
liczy deltę względem `Canvas` (nigdy względem siebie samego — pętla dodatniego sprzężenia) i woła
`PanelGeometry.ClampMove`/`ConstrainResize`, zapisując wynik bezpośrednio na właściwościach
Avalonii (`Canvas.Left/Top`, `Width`/`Height`) — to one, przez wiązania `TwoWay` utworzone w
`WorkspaceSurface.PrepareContainerForItemOverride`, spływają z powrotem do `WorkspacePanelViewModel.X/Y/Width/Height`.
Puszczenie przycisku → `EndGesture` liczy docelowe (snapped) położenie
(`PanelGeometry.SnapMove`/`SnapResize`) i odpala 120 ms animację dojazdu (`DispatcherTimer`, easing
cubic) → po jej zakończeniu `RaiseGestureCompleted()` (routed event, bąbelkuje) →
`CampaignWorkspaceView.OnGestureCompleted` woła `CampaignWorkspaceViewModel.CommitGesture(panel)` →
`panel.CommitGesture()` promuje bieżącą (efektywną) geometrię na `Desired` (jedyną persystowaną) →
`_session.MarkDirty()` restartuje debounce 750 ms (`WorkspaceLayoutSession`) → po ciszy (albo na
`FlushPendingState`/zamknięciu aplikacji/kampanii) `WorkspaceLayoutStore.Save` zapisuje cały układ
biurka atomowo do `%LocalAppData%\DungeonApp\layouts\<id>.json`.

---

## 8. Punkty rozszerzeń

### Dodanie nowego narzędzia (tool) + bloku danych

Na podstawie ścieżki `CounterTool` → `CounterPanelViewModel` → biurko:

1. **Domena (`Core`):** nowa klasa implementująca `ITool` w `Core/Tools/<NazwaNarzędzia>/`. Zdefiniuj
   statyczny `DataBlockId`, `DataBlockVersion`, `DataBlockShape` (`ObjectShape`/`PrimitiveShape`)
   i metody zwracające `Func<object?, object>` do przekazania w `CampaignDataBlocks.Apply`. Jeśli
   narzędzie potrzebuje więcej niż jednego bloku danych, `Uses` wylicza je wszystkie.
2. **Rejestracja bloku:** w `App.axaml.cs` dopisać `_dataBlocks.Register(NoweNarzędzie.DataBlockId, ...)`
   i sprawdzić `Uses` narzędzia przeciw rejestrowi (analogicznie do istniejącej pętli dla
   `CounterTool`).
3. **ViewModel panelu (`Desktop/Features/CampaignWorkspace/Panels/`):** nowa klasa implementująca
   `ObservableObject` (+ `IDisposable`, jeśli subskrybuje `CampaignEvents`), przyjmująca
   `CampaignSession`, subskrybująca `DataBlockChanged` dla swojego `DataBlockId`, wołająca
   `CampaignSession.ExecuteAsync` do każdej zmiany.
4. **Widok (`.axaml` + code-behind):** nowa `UserControl` (analogiczna do `CounterPanelView`).
5. **`WorkspacePanelDescriptor` w `PanelCatalog.For(...)`:** nowy wpis z `Id`, `Title`,
   `IconResourceKey`, `WorkspacePanelGroup`, domyślnym `PanelPlacement`, `PanelConstraints`
   (min/max rozmiar) i fabryką `CreateContent` tworzącą nowy ViewModel.
6. **Data template:** dopisać wpis w `<UserControl.DataTemplates>` w
   `src/DungeonApp.Desktop/Features/CampaignWorkspace/CampaignWorkspaceView.axaml`, gdzie dziś
   znajduje się jedyny wpis: `<DataTemplate DataType="panels:CounterPanelViewModel">` →
   `<panels:CounterPanelView />`. To tu Avalonia rozwiązuje ViewModel panelu na jego widok.
7. **Testy:** analogicznie do `CounterToolTests` (domena) i `CounterPanelViewModelTests` (ViewModel)
   — te drugie są dziś najlepiej pokrytym plikiem w projekcie i warto je traktować jako wzorzec
   kompletności (przepełnienie, błąd zapisu, nieodczytywalny blok, wyścig zapisów, dispose).

### Dodanie nowego panelu bez nowego narzędzia domenowego
Jeśli panel nie potrzebuje własnego bloku danych (np. czysto prezentacyjny), kroki 3–6 powyżej się
stosują bez kroków 1–2.

Uwaga: katalog paneli jest dziś budowany **per otwarta sesja kampanii**
(`PanelCatalog.For(session)`), więc każdy nowy panel domyślnie trafia na biurko każdej kampanii —
nie ma dziś mechanizmu włączania/wyłączania paneli per kampania czy per ruleset (pola `Ruleset`/
`ContentPacks` w manifeście kampanii są zarezerwowane, ale nieużywane).

---

## 9. Strategia testów

| Warstwa | Plik(i) | Co pokrywają |
|---|---|---|
| Domena — kampanie | `CampaignTests`, `CampaignNameTests`, `CreateCampaignTests` | Tworzenie/odtwarzanie, walidacja nazwy, use case tworzenia. |
| Domena — bloki danych | `CampaignDataBlocksTests` (24 testy — najliczniejszy plik w Core), `DataBlockIdTests`, `DataBlockRegistryTests`, `DataBlockShapeTests` (15) | Cykl `Apply`/`Read`, zamrażanie wartości, `unreadable`, walidacja kształtów, znakowe ograniczenia ID. |
| Domena — zdarzenia | `CampaignEventsTests` (12) | Kolejność subskrypcji, kaskada, limit `MaxEventsPerCommand`. |
| Domena — narzędzie | `CounterToolTests` (6) | Increment/decrement, przepełnienie. |
| Persystencja | `JsonCampaignRepositoryTests` (12), `DataBlockPersistenceTests` (12) | Zapis/odczyt na prawdziwym systemie plików (`TemporaryLibrary` — świadomie nie mockuje FS), torn save, nieznane/nieaktualne wersje bloków. |
| Warstwa treści | `ContentPackLoaderTests`, `ContentIdTests`, `FieldNameTests` | Wczytywanie i walidacja na prawdziwym systemie plików (`TemporaryPacks`), wszystkie reguły odrzucenia, wszystkie powody nierozwiązania, limity. Ręcznie napisane paczki w `tests/DungeonApp.Core.Tests/Packs/` są wczytywane jako test akceptacyjny — format jest specyfikacją, więc specyfikacja jest wykonywana. |
| Desktop — rejestr | `RegistryViewModelTests`, `LoadContentPacksStepTests` | Kolejność elementów karty zgodna z szablonem, formatowanie wartości, pominięcie pustych pól opcjonalnych, trzy powody nierozwiązania, pusty rejestr, krok startowy wobec paczki wadliwej obok poprawnej. |
| Architektura | `CoreIndependenceTests` (1 test) | Odrzuca referencję `Avalonia*` w zestawie `DungeonApp.Core` przez refleksję (`GetReferencedAssemblies`). To jedyny test wymuszający granicę Core/Desktop mechanicznie — bez niego podział na dwa projekty byłby czystą konwencją. |
| Desktop — cache przygotowania | `CampaignWorkspacePreparationCacheTests` (2) | Rozgrzewka trafia w pierwsze `Take`, kampania usunięta z półki nie blokuje startu. |
| Architektura | `CoreEntryKindIndependenceTests` (2 testy) | Bliźniak powyższego dla drugiej granicy: silnik nie zna rodzajów wpisów. Skan słownictwa po źródłach `Core`, ze słownikiem wyprowadzanym z paczek fixture'owych, więc zakaz poszerza się sam wraz z treścią. Drugi test pilnuje, żeby skan nie przeszedł przez to, że niczego nie znalazł. |
| Desktop — panel licznika | `CounterPanelViewModelTests` (9) | Najlepiej pokryty plik warstwy Desktop: stan początkowy, odświeżenie po zdarzeniu zewnętrznym, przepełnienie, błąd zapisu na dysk, nieodczytywalny blok, wyścig zapisów, dispose. |
| Desktop — geometria | `PanelGeometryTests` (2) | Tylko `FitInto` (dopasowanie do min/max) — **`ClampMove`, `SnapMove`, `SnapResize`, `ConstrainResize`, `Maximize` nie mają dedykowanych testów jednostkowych**, mimo że to najbardziej złożona czysta logika w warstwie Desktop. |
| Desktop — układ | `WorkspaceLayoutStoreTests` (1) | Tylko happy-path zapis→odczyt. Brak testów na: uszkodzony plik, nieznaną wersję, `MaxPanels`, sanityzację id. |

**Co nie jest pokryte w ogóle:** `AppShellViewModel` (sekwencja startowa, przełączanie
sekcji/kampanii — zero testów), `CampaignWorkspaceViewModel` (restauracja układu, fitowanie,
minimalizacja/maksymalizacja, z/order — zero testów), `WorkspaceLayoutSession` (debounce, flush —
zero testów), `PanelWindow` (cała logika gestów pointer — wymagałaby testów UI/headless, których nie
ma), sidebary, top bar, status bar (trywialne, ale bez testów), wszystkie cztery kroki startowe
(poza `LoadContentPacksStep`, który testy ma; pozostałe cztery kroki `Startup/*` — zero testów
jednostkowych, testowane tylko pośrednio przez uruchomienie aplikacji).

**Rola `CoreIndependenceTests`:** to nie jest test funkcjonalności, tylko test **granicy
architektonicznej**. Jego jedynym zadaniem jest nie dopuścić, żeby ktoś (człowiek albo agent AI)
dodał `using Avalonia` w projekcie Core „bo było wygodnie" — bez niego separacja domeny od UI, o
której mówi README, byłaby tylko deklaracją w dokumentacji, nie czymś wymuszonym przez CI/build.

---

## 10. Ocena stanu

### Dojrzałe
- **Domena (`Core`)** jest zaskakująco dopracowana jak na projekt w tej fazie: jasne rozróżnienie
  `Create`/`Restore`, immutability przez zamrażanie wartości, spójny model błędów
  (`CampaignRuleException` dla GM-a, `CampaignStoreException`/`DataBlockUnreadableException` dla
  reszty systemu), test architektoniczny pilnujący granicy. Kod jest gęsto skomentowany z
  uzasadnieniem decyzji (nie tylko „co", ale „dlaczego"), co jest nietypowo wysokim standardem.
- **Persystencja** (`JsonCampaignRepository`, `WorkspaceLayoutStore`) — oba magazyny mają realną
  transakcyjność przez zapis do pliku tymczasowego + atomowy `File.Move`, wykrywanie przerwanego
  zapisu przez licznik generacji, i celowo łagodną degradację (uszkodzony układ biurka → puste
  domyślne, nie crash; jedna zepsuta kampania nie chowa reszty półki).
- **Geometria paneli** (`PanelGeometry`) — czysta, bezstanowa, wolna od typów Avalonii, z
  nietrywialną, dobrze przemyślaną logiką snapowania/ograniczeń. To najbardziej „inżynierski"
  fragment warstwy Desktop.
- **`CounterPanelViewModel`** i jego testy — jedyny w pełni „domknięty" pionowy przekrój (UI → sesja
  → domena → dysk → z powrotem do UI) z kompletem testów pokrywających ścieżki błędów.

### Rusztowanie / celowo tymczasowe
- **Licznik (`CounterTool`/`CounterPanelViewModel`/`CounterPanelView`) jest jawnie testowym
  rusztowaniem, nie docelową funkcją.** Potwierdza to zarówno komentarz w `Campaign.cs`
  („CounterTool jako kompletny przykład narzędzia" w README), jak i sama natura panelu — licznik
  całkowitoliczbowy bez żadnego związku z regułami RPG. Jest to jedyne miejsce, gdzie cały
  przepływ od gestu do dysku istnieje i jest przetestowany od początku do końca — dlatego pełni rolę
  wzorca/dowodu słuszności architektury, a nie funkcji produktu. Trzeba się liczyć z tym, że
  zniknie albo zostanie przeniesiony do testów, gdy pojawi się pierwsze prawdziwe narzędzie.
- **Nawigacja boczna** (`GlobalSidebarViewModel`) ma dziś tylko jedną realną sekcję
  („Kampanie"); każda inna sekcja renderuje `WorkspacePlaceholderView` — dosłowny placeholder bez
  zawartości. Sama klasa `OnSectionSelected` w `AppShellViewModel` nosi komentarz „Temporary
  scaffolding until the real context router exists".
- **`WarmPanelVisualStep`** istnieje jako gotowa klasa, ale nie jest podpięty do sekwencji
  startowej w `App.axaml.cs` — martwy kod albo niedokończona funkcja (nie da się stwierdzić z
  samego kodu, które).
- **Pola `Ruleset`/`ContentPacks`** w manifeście kampanii są zarezerwowane, zawsze `null`/`[]`, i
  nieużywane nigdzie indziej — świadomie odłożona przyszłość, ale dziś martwe.
- **`CampaignStoreFailure.UnknownModule`** zdefiniowany, ale nic go dziś nie rzuca — nie ma
  koncepcji „modułu" w obecnym kodzie (jest tylko `ITool`/blok danych).

### Dług i luki
- **Warnings-as-errors nie jest włączone nigdzie.** `Directory.Build.props` ustawia tylko
  `EnforceCodeStyleInBuild`; nic nie wymusza `TreatWarningsAsErrors`. Wysoka jakość kodu dziś to
  efekt dyscypliny autora, nie mechanizmu — ryzyko przy wejściu kolejnej osoby/agenta.
  Wewnętrzny plik CLAUDE.md użytkownika notuje wprost: „reguła niewykonalna psuje całą listę" —
  ten punkt jest kandydatem do zamiany w wymuszalną regułę, jeśli to pożądane.
- **Pokrycie testami warstwy Desktop jest bardzo nierówne.** `CounterPanelViewModel` ma 9 testów;
  `AppShellViewModel`, `CampaignWorkspaceViewModel`, `WorkspaceLayoutSession`, cztery kroki
  startowe i cała logika gestów w `PanelWindow` — zero. To oznacza, że najbardziej złożona
  logika stanu UI (przełączanie kampanii, fitowanie paneli, debounce zapisu układu) jest dziś
  weryfikowana wyłącznie ręcznie.
- **Brak migracji wersji bloków danych i formatu manifestu.** Kod jest przygotowany strukturalnie
  (`DataBlockRegistration.Version`, `CampaignManifest.FormatVersion`), ale ścieżka migracji nie
  istnieje — każda zmiana kształtu istniejącego bloku dziś oznacza „stare dane stają się
  `unreadable`", nie „migrują się". To świadoma decyzja (komentarz: „a migration is only
  meaningful once a shape's version has actually been raised once"), ale jest to realny dług,
  który przyjdzie do spłacenia przy pierwszej zmianie kształtu.
- **Jedno narzędzie domenowe.** Cały system rozszerzalności (`PanelCatalog`, `ITool`,
  `DataBlockRegistry`) jest zaprojektowany pod wiele narzędzi, ale zweryfikowany tylko przez jedno
  (i to celowo trywialne). Nie wiadomo z kodu, jak dobrze abstrakcje wytrzymają drugie, bardziej
  złożone narzędzie (wielo-blokowe, z zależnościami między blokami, z wielo-instancyjnością —
  `AllowsMultipleInstances` istnieje we `WorkspacePanelDescriptor`, ale żaden panel go dziś nie
  używa).
- **Brak obsługi błędu przy tworzeniu kampanii z powodu IO** poza cichym połknięciem
  (`CampaignLibraryViewModel.CreateAsync` łapie `IOException` z komentarzem „Persistent feedback
  belongs to a future error state, not a temporary toast" — czyli znany, nazwany, ale nie
  zaadresowany brak UX błędów).
- **`MockupRenderer`** żyje poza `.sln` i nie ma własnych testów — to narzędzie deweloperskie,
  akceptowalne, ale warto pamiętać, że nic automatycznie nie sprawdza, czy nadal się buduje wraz z
  resztą repo.

### Zgodność z README
README opisuje intencję trafnie i bez nadmiernych obietnic — jest w tym względzie rzadko
rozjeżdżający się z kodem dokument. Jedyne miejsca do odnotowania:
- README każe czytać projekt zaczynając od `App.axaml.cs` → `AppShellViewModel` → domenę →
  `CounterTool`. Ta kolejność faktycznie odpowiada realnej strukturze zależności — potwierdzone.
- README nie wspomina, że `WarmPanelVisualStep` jest martwy/nieużywany, ani że pokrycie testami
  Desktop jest tak nierówne — to nie jest sprzeczność, tylko brak (README nie rości sobie prawa do
  bycia raportem ze stanu, i faktycznie nim nie jest).
- README poprawnie nazywa licznik „kompletnym przykładem narzędzia" — nie twierdzi, że to docelowa
  funkcja. Zgodność potwierdzona, ale warto, żeby każdy nowy współpracownik (lub agent) przeczytał
  to zdanie uważnie, bo z samego kodu (dobrze przetestowany, dopracowany UI panelu) łatwo błędnie
  wywnioskować, że licznik jest funkcją docelową.
