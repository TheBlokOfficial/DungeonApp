# DungeonApp — mapa kodu

**Status: opis stanu faktycznego. Nie rozstrzyga niczego.** Ten dokument mówi, **co jest w kodzie
dzisiaj** i gdzie co leży. Przy rozbieżności z kodem prawdą jest kod, a dokument jest do poprawienia.
Cel: dać człowiekowi albo agentowi możliwość zorientowania się w repozytorium bez czytania każdej
linii.

Projekt docelowy — to, do czego kod ma dojść — opisuje [architecture.md](architecture.md).
**Ten dokument i tamten rozjeżdżają się celowo i będą rozjeżdżać się coraz bardziej**, dopóki nie
zostaną wykonane kolejne kroki z sekcji *Kolejność prac* tamtego. Rozbieżność jest planem, nie
usterką; lista różnic jest tam, nie tu.

## Od czego zacząć czytanie kodu

1. `src/DungeonApp.App/Program.cs` — korzeń kompozycji i jedyne miejsce wymieniające zestawy treści
   z nazwy. `BuildAvaloniaApp` woła `AppBuilder.Configure` z fabryką (`() => new App(BuildContentSets())`),
   nie z parametrem generycznym — bo `App` potrzebuje listy zestawów treści przekazanej przez
   konstruktor, a nie odczytanej z jakiegoś statycznego miejsca.
2. `src/DungeonApp.Desktop/App.axaml.cs` — dalszy ciąg kompozycji: rejestr bloków danych, repozytoria,
   loader treści, tablica kroków startowych. Przyjmuje listę zestawów treści przez konstruktor, nigdy
   jej nie odkrywa sama.
3. `Shell/AppShellViewModel.cs` — przełącza między biblioteką kampanii, rejestrem treści i biurkiem
   otwartej kampanii.
4. `DungeonApp.Core`: `Campaign`, `CampaignDataBlocks`, `JsonCampaignRepository` — stan, jego zmiana
   i trwały zapis.
5. Licznik jako kompletny przekrój pionowy: `CounterTool` → `CounterPanelViewModel` →
   `CampaignSession` → `JsonCampaignRepository`.
6. `Core/Content/*` + `Desktop/Content/*` + `src/DungeonApp.Content.Dnd5e/*` — druga kompletna
   ścieżka pionowa, od pliku na dysku do narysowanej karty. Warto przejść ją zaraz po liczniku, bo to
   ona dziś niesie największą część architektury (patrz sekcja 4b).

**Licznik jest rusztowaniem, nie funkcją.** Powstał, żeby udowodnić, że cała ścieżka od gestu do
zapisu na dysku działa i daje się przetestować. Zniknie, gdy powstanie pierwsze prawdziwe narzędzie
biurka. Z samego kodu — dopracowanego i jednego z lepiej pokrytych testami w repozytorium — łatwo
wyciągnąć przeciwny wniosek, więc to zdanie jest tu celowo.

Testy w `tests/` są zarazem wykonywalną specyfikacją opisanych tu zachowań.

Zakres przejrzany w całości: `src/DungeonApp.Core`, `src/DungeonApp.Desktop`,
`src/DungeonApp.Content.Dnd5e`, `src/DungeonApp.App`, `tests/`, pliki `.csproj`,
`Directory.Build.props`, `DungeonApp.sln`.

> **Regeneracja 2026-09-12: co potwierdzono, co nie.** Ten dokument opisywał warstwę treści sprzed
> przejścia na skompilowane typy — szablony jako pliki danych, katalog elementów karty, kontrakty
> podsumowań. Ta regeneracja przeszła cały dokument sekcja po sekcji, otwierając dla każdego zdania
> plik źródłowy, którego dotyczyło; zdania, których nie dało się potwierdzić w kodzie, zostały
> usunięte, nie przepisane na wyczucie. Potwierdzone czytaniem: cały mechanizm treści (`Core/Content`,
> `Desktop/Content`, kontrolki karty, `DungeonApp.Content.Dnd5e`, cztery pliki testów
> architektonicznych), pięcioetapowa sekwencja startowa, brak `WarmPanelVisualStep`,
> `TreatWarningsAsErrors=true`, liczba projektów (4 produkcyjne, 4 testowe), kierunek referencji
> zestawu treści, oraz — wbrew temu, co twierdziła poprzednia wersja tej sekcji — że pole `Ruleset`
> manifestu kampanii **nie** zniknęło i że pole na paczki treści to zwykła lista identyfikatorów
> (`ContentPacks: string[]`), nigdy nie było rekordem `PackReference(Id, Major)`. Niepotwierdzone i
> dlatego pominięte: dokładny czas ostatniego rzeczywistego użycia `WorkspacePanelDescriptor.AllowsMultipleInstances`
> poza deklaracją, oraz wszystko, czego kod sam nie tłumaczy komentarzem (tam, gdzie brakowało
> uzasadnienia „dlaczego", opisano samo „co").
>
> **Aktualizacja z tego samego dnia: krok „odrzucanie per plik wpisu" został zrobiony.** Poprzednia
> wersja tej ramki zapowiadała, że gdy to się wydarzy, opis `ContentPackLoader` w sekcji 4b trzeba
> będzie napisać jeszcze raz — i tak się stało. Sekcja 4b opisuje teraz trzy zakresy odrzucenia
> (paczka / plik / wpis), a opis ekranu rejestru — wiersze plików niewczytanych. Jedyne, co z tabeli
> „Co się dzieje, gdy treść jest zepsuta" nadal nie dociera do użytkownika, to odrzucone **paczki**:
> loader je odnotowuje, ekran ich nie pokazuje, i jest to świadoma decyzja autora, nie luka
> w wykonaniu.
>
> **Aktualizacja 2026-09-13: dwa zarezerwowane pola manifestu kampanii zniknęły.** Zdanie wyżej
> o tym, że pole `Ruleset` „nie zniknęło", było prawdziwe w dniu, w którym je zapisano, i jest
> nieprawdziwe od tej zmiany — razem z nim usunięto `ContentPacks`. Powód nie jest kosmetyczny:
> `decisions.md` opisywał tę decyzję jako **już wykonaną**, podczas gdy w kodzie stały oba pola
> i test, który je zamrażał. Rozjazd wyszedł przy sprawdzaniu przesłanek kolejki i wtedy decyzję
> wykonano naprawdę. Wersja formatu została na `1`, bo odczyt manifestu jest pobłażliwy; pilnują
> tego dwa testy.

---

## 1. Czym jest aplikacja i czym nie jest

DungeonApp to desktopowa aplikacja WPF-podobna (Avalonia) dla jednej osoby — Mistrza Gry
prowadzącego sesję RPG przy stole. Jej rolą jest utrzymywać stan jednej otwartej kampanii i
zapisywać go na dysku po każdej zmianie, tak żeby nic nie przepadło między sesjami. Działa lokalnie,
na jednej maszynie, bez logowania, bez sieci, bez wielu użytkowników — nie ma pojęcia konta, serwera
ani synchronizacji.

Nie jest to: stół wirtualny (VTT) do grania online, generator treści, silnik reguł konkretnego
systemu RPG, ani aplikacja dla graczy — cały interfejs jest zaprojektowany pod jedną osobę
zarządzającą jedną kampanią naraz.

W obecnym stanie to w praktyce **szkielet aplikacji desktopowej z dwiema kompletnymi pionowymi
ścieżkami**: licznik testowy (gest → domena → dysk) i warstwa treści (plik paczki na dysku →
rejestr → narysowana karta). Docelowe „prawdziwe" narzędzia GM-a (dziennik, NPC, zasoby itd.) nie
istnieją jeszcze w kodzie — a warstwa treści dziś rysuje tylko encyklopedię do przeglądania, nie
osadza jeszcze żadnej treści w konkretnej kampanii (sekcja 5 i 8 wyjaśniają, co dokładnie na to
czeka).

Repozytorium zawiera cztery projekty produkcyjne (`DungeonApp.Core`, `DungeonApp.Desktop`,
`DungeonApp.Content.Dnd5e`, `DungeonApp.App`) i cztery testowe (`DungeonApp.Core.Tests`,
`DungeonApp.Desktop.Tests`, `DungeonApp.Architecture.Tests`, `DungeonApp.Content.Dnd5e.Tests`) —
`DungeonApp.sln` nie niesie nic poza nimi. `tools/MockupRenderer` i `design/mockups/` nie istnieją
w repozytorium. **`docs/images/` jest katalogiem roboczym autora na mockupy** — leżą tam luzem, bez
odsyłaczy z dokumentów, i tak ma zostać: to materiał do projektowania interfejsu, a nie ilustracje
do tekstu.

---

## 2. Stos technologiczny i zależności

| Element | Wartość |
|---|---|
| Język | C#, `<LangVersion>latest</LangVersion>` (Directory.Build.props) |
| TFM wszystkich projektów | `net10.0` |
| `Nullable` | `enable` we wszystkich ośmiu `.csproj` |
| `EnforceCodeStyleInBuild` | `true` (globalnie, Directory.Build.props) |
| `TreatWarningsAsErrors` | **`true`** (globalnie, Directory.Build.props) — ostrzeżenie kompilatora zatrzymuje build; to nie jest już dług, tylko mechanizm. |
| UI framework | Avalonia **12.0.5** (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`) |
| Compiled bindings | `AvaloniaUseCompiledBindingsByDefault = true` |
| Diagnostyka | `AvaloniaUI.DiagnosticsSupport 2.2.3`, dołączane tylko w konfiguracji `Debug` |
| Publish | `PublishReadyToRun = true` (w `DungeonApp.App`) — mniej JIT-a przy pierwszym użyciu widoku w publikowanym buildzie |
| Testy | xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` — brak Moq/NSubstitute/FluentAssertions; podwójne (fakes) pisane ręcznie |
| Kontener DI | **brak.** Kompozycja obiektów jest ręczna, w `DungeonApp.App/Program.cs` i `DungeonApp.Desktop/App.axaml.cs` (patrz sekcja 6) |
| Serializacja | wbudowany `System.Text.Json`, camelCase, indentowany JSON; strict (`UnmappedMemberHandling.Disallow`) w warstwie treści, wyrozumiały w manifeście kampanii |

Solucja `DungeonApp.sln` obejmuje osiem projektów: cztery produkcyjne
(`DungeonApp.Core`, `DungeonApp.Desktop`, `DungeonApp.Content.Dnd5e`, `DungeonApp.App`) i cztery
testowe (`DungeonApp.Core.Tests`, `DungeonApp.Desktop.Tests`, `DungeonApp.Architecture.Tests`,
`DungeonApp.Content.Dnd5e.Tests`).

**Kierunek referencji projektów** (§13 architecture.md, „Warstwy i granice"):

```
DungeonApp.Core            — nie referencje niczego z repozytorium
DungeonApp.Desktop         — referencje Core
DungeonApp.Content.Dnd5e   — referencje Core i Desktop
DungeonApp.App             — referencje Desktop i Content.Dnd5e
```

`DungeonApp.Desktop` nie jest dziś plikiem wykonywalnym — jego `.csproj` nie deklaruje
`OutputType`, więc buduje się jako biblioteka. Korzeniem kompozycji i jedynym `WinExe` jest
`DungeonApp.App`. Ta odwrotność — zestaw treści zależny od powłoki, a nie odwrotnie — jest tym, co
pozwala powłoce nigdy nie widzieć żadnego zestawu po nazwie: gdyby to `Desktop` referencjonował
`Content.Dnd5e`, granica „powłoka nie zna zestawów" byłaby tylko konwencją. `DungeonApp.Content.Dnd5e.Tests`
referencjonuje `Core` i `Content.Dnd5e` wprost (nie `Desktop` — dostaje go tranzytywnie, bo
`Content.Dnd5e` sam go referencjonuje).

---

## 3. Mapa modułów

### `src/DungeonApp.Core` — domena, zero Avalonii

| Ścieżka | Odpowiedzialność |
|---|---|
| `Campaigns/Campaign.cs` | Korzeń agregatu: tożsamość, nazwa, data utworzenia, bloki danych, magistrala zdarzeń kampanii. `Create` vs `Restore` — jawne rozróżnienie „nowa" od „odtworzona z dysku". |
| `Campaigns/CampaignId.cs`, `CampaignName.cs`, `CampaignSummary.cs` | Typy wartości: trwałe ID (Guid), zwalidowana nazwa (max 100 znaków), lekki DTO do listowania (bez ładowania pełnej kampanii). |
| `Campaigns/CreateCampaign.cs` | Jedyny use case tworzenia kampanii — waliduje nazwę, tworzy agregat, zapisuje przez repozytorium. |
| `Campaigns/ICampaignRepository.cs` | Port: `SaveAsync`, `GetAsync`, `DeleteAsync`, `ListAsync`. Jedyna implementacja: `JsonCampaignRepository`. |
| `DataBlocks/CampaignDataBlocks.cs` | Jedyne miejsce trzymające i mutujące wartości bloków danych kampanii. Cała mutacja idzie przez `Apply(id, transform)` — patrz sekcja 4a. |
| `DataBlocks/DataBlockRegistry.cs`, `DataBlockRegistration.cs` | Rejestr tego, co dany build „zna": id, wersja kształtu, kształt. Budowany raz, w `App.axaml.cs`. |
| `DataBlocks/DataBlockShape.cs` | Zamknięta hierarchia kształtów: `PrimitiveShape` (Integer/Fractional/Text/Boolean) i `ObjectShape` (płaski rekord pól). Kształt tylko *sprawdza* wartość, nigdy jej nie przechowuje. |
| `DataBlocks/DataBlockId.cs` | Identyfikator bloku (np. `counter`) — ograniczony znakowo, bo staje się nazwą pliku na dysku. |
| `DataBlocks/DataBlockChanged.cs` | Zdarzenie „wartość bloku właśnie się zmieniła" — niesie tylko id, nigdy wartość (przed ani po). |
| `DataBlocks/UnreadableDataBlock.cs`, `DataBlockUnreadableException.cs` | Model „ten blok istniał na dysku, ale ten build nie umie go odczytać" — nieznany rejestrowi albo nieobsługiwana wersja. |
| `DataBlocks/DataBlockShapeMismatchException.cs` | Rzucany, gdy wynik transformu nie pasuje do zarejestrowanego kształtu — poprzednia wartość zostaje nietknięta. |
| `Events/CampaignEvents.cs` | Prosta, synchroniczna magistrala zdarzeń *per kampania* (nigdy statyczna/globalna) z limitem kaskady (`MaxEventsPerCommand = 1000`). |
| `Events/ICampaignEvent.cs`, `EventCascadeException.cs` | Kontrakt zdarzenia (nazwa w czasie przeszłym) i wyjątek pętli zdarzeń. |
| `Tools/ITool.cs` | Kontrakt narzędzia: deklaruje tylko, jakich `DataBlockId` używa (`Uses`). Żadnej innej logiki w interfejsie. |
| `Tools/Counter/CounterTool.cs` | Jedyna dziś implementacja `ITool`. Definiuje kształt bloku `counter` (pole `count`: Integer) i dwie transformacje: `Increment`/`Decrement` (z `checked` — przepełnienie rzuca `OverflowException`). |
| `Content/*` | Silnik treści: `Pack` (jeden typ, tylko lista wpisów — następca dawnego podziału paczka-systemowa/paczka-treściowa), `Entry`, `ContentValues` (opieczętowana koperta nad surowym JSON-em wpisu), `ContentId`, `ContentTypeReference` (`"zestaw:typ"`), `ContentTypeDescriptor`, `IContentTypeCatalog` (jedyne okno silnika na typy treści), `ContentRegistry`, `RegisteredEntry`, `EntryAddress` (`"paczka:wpis"`), `EntryUnresolvedReason`, `RejectedPack`, `PackVersion`, `ContentPackLoader`. Patrz sekcja 4b — to dziś najbogatsza część `Core`. |
| `Persistence/JsonCampaignRepository.cs` | Jedyna implementacja `ICampaignRepository`. Format zapisu na dysku, transakcyjność, obsługa błędów — patrz sekcja 5. |
| `Persistence/AtomicWrite.cs` | Zapis „obok, potem podmiana" jako jeden prymityw: `Stage`/`StageAsync` serializują do `*.writing.tmp`, `Commit` przenosi wszystko atomowo **w kolejności dodania**, `Dispose` sprząta to, czego nie zatwierdzono. Jedyne miejsce, w którym ten wzorzec jest napisany — używają go `JsonCampaignRepository` i `WorkspaceLayoutStore`. |
| `Persistence/DataBlockValueSerializer.cs` | Konwersja wartość ↔ `JsonNode`, zawsze prowadzona przez `DataBlockShape` (nigdy „co się da z JSON-a wyczytać"). |
| `Persistence/CampaignStoreException.cs` | Typowany błąd repozytorium: `Unreadable`, `UnsupportedFormatVersion`, `Invalid`, `TornSave`. |
| `CampaignRuleException.cs` | Wyjątek „reguła kampanii odmówiła" — komunikat czytany wprost przez GM-a (po polsku, gdy dotyczy licznika). |

**Granica:** cztery pliki w `DungeonApp.Architecture.Tests/Architecture/` pilnują tego wszystkiego
mechanicznie — patrz sekcja 9.

### `src/DungeonApp.Desktop` — Avalonia: powłoka, biurko, panele, motyw, prezentacja treści

| Ścieżka | Odpowiedzialność |
|---|---|
| `App.axaml.cs` | Dalszy ciąg kompozycji po `DungeonApp.App/Program.cs`: buduje rejestr bloków danych, repozytoria, `ContentPackLoader`, agregaty treści, cache przygotowania kampanii, sekwencję kroków startowych i `AppShellViewModel`. Przyjmuje listę zestawów treści przez konstruktor — nigdy jej nie odkrywa. Brak kontenera DI — czysty constructor injection. |
| `Program.cs`, `MainWindow.axaml(.cs)` | `Program.cs` tu jest tylko projektantowym/hot-reloadowym zaczepem (patrz niżej); `MainWindow` to pusta powłoka bez logiki. |
| `Content/IContentSet.cs`, `IContentPresentation.cs` | Kontrakt zestawu treści widziany od strony powłoki — patrz sekcja 4b. |
| `Content/ContentTypeCatalogAggregate.cs`, `ContentPresentationAggregate.cs` | Agregują listę zestawów treści do pojedynczego `IContentTypeCatalog`/`IContentPresentation`, jakiego oczekują `ContentPackLoader` i `RegistryViewModel` — bez tego jedna paczka treści musiałaby znać wszystkie zestawy z osobna. |
| `Controls/Content/TraitListView`, `ProseBlockView`, `TraitRow` | Współdzielone kontrolki karty — jedyne miejsce, z którego zestaw treści komponuje wygląd wpisu. Patrz sekcja 4b. |
| `Features/Registry/*` | Ekran rejestru: `RegistryViewModel` (wpisy posortowane po nazwie i adresie, za nimi pliki niewczytane; karta wybranego wpisu jako gotowy `Control` z `IContentPresentation.CreateCard`), `RegistryEntryRowViewModel`, `RegistryView`. Patrz sekcja 4b. |
| `Shell/AppShellViewModel.cs` | Właściciel „gdzie jest GM": biblioteka kampanii vs rejestr treści vs otwarte biurko, sekwencja startowa, przełączanie sekcji bocznych. |
| `Shell/CampaignSession.cs` | Jedyna droga zmiany otwartej kampanii: `ExecuteAsync(operation)` — wykonaj operację, zapisz, ogłoś `Committed`. |
| `Shell/Sidebars/*` | Globalny pasek boczny nawigacji (kolapsowalny), dziś z dwiema realnymi sekcjami („Kampanie", „Rejestr") — cokolwiek nierozpoznanego ląduje na placeholderze. |
| `Shell/TopBar/*` | Pasek kontekstu: tytuł otwartej kampanii/sekcji + akcja zamknięcia. |
| `Shell/StatusBar/*` | Pasek stanu na dole (komunikaty typu „Gotowe", szerokość zsynchronizowana z sidebarem). |
| `Shell/Workspace/WorkspacePlaceholderView(Model)` | Widok zastępczy dla każdej sekcji nawigacji poza „Kampanie" i „Rejestr" — dosłowny placeholder, `record WorkspacePlaceholderViewModel(string Title)`. |
| `Startup/*` | Jawna, kolejnościowa sekwencja pięciu kroków startowych (`IStartupStep`) — patrz sekcja 6 i 7a. |
| `Features/CampaignLibrary/*` | Ekran „półki" kampanii: lista, tworzenie, usuwanie, otwieranie. |
| `Features/CampaignWorkspace/*` | Biurko otwartej kampanii: `CampaignWorkspaceViewModel` (panele, ich stan, dopasowanie do rozmiaru), `CampaignWorkspacePreparation(Cache)` (odczyt danych przed zbudowaniem UI), `Layout/*` (zapis/odczyt układu biurka), `Panels/*` (katalog i implementacje paneli), `Deck/PanelDeckView` (pasek zminimalizowanych paneli). |
| `Controls/Workspace/*` | Framework „pływających okien" na płótnie: `PanelWindow` (kontrolka), `WorkspaceSurface` (`ItemsControl` hostujący panele na `Canvas`), cała arytmetyka geometrii (`PanelGeometry`, `PanelPlacement`, `PanelConstraints`, `WorkspaceMetrics(Resolver)`, `WorkspaceGridSettings`). |
| `Controls/AnchoredContentHost.cs` | Dekorator ograniczający szerokość/wysokość treści biblioteki na dużych ekranach. |
| `Controls/ResourceKeyToImageConverter.cs` | Konwerter string→zasób (ikony) dla bindingów. |
| `Themes/*.axaml` | `Tokens.axaml` (183 linie: kolory/rozmiary/skale), `BuiltInControls.axaml` (163), `DungeonControls.axaml` (280: style `PanelWindow` itd.), `Icons.axaml` (110: zasoby ikon). |
| `ViewModels/ObservableObject.cs`, `AsyncCommand.cs` | Własna, minimalna infrastruktura MVVM — bez CommunityToolkit.Mvvm ani innej biblioteki. |

### `src/DungeonApp.App` — korzeń kompozycji

| Ścieżka | Odpowiedzialność |
|---|---|
| `Program.cs` | `Main` + `BuildAvaloniaApp`. Jedyne miejsce w aplikacji, które wymienia zestaw treści z nazwy (`new Dnd5eContentSet()`) — hard-wired przez referencję projektu, nigdy nie odkrywane w czasie działania (komentarz w kodzie tłumaczy, czemu wtyczki z katalogu nic tu nie dają). |

### `src/DungeonApp.Content.Dnd5e` — zestaw treści D&D 5e

| Ścieżka | Odpowiedzialność |
|---|---|
| `Dnd5eContentSet.cs` | Jedyne miejsce w aplikacji, któremu wolno wiedzieć, czym jest potwór albo przedmiot. Implementuje `IContentSet` (czyli `IContentTypeCatalog` + `IContentPresentation`); deklaruje dwa typy treści (`monster`/„Potwór", `gear`/„Przedmiot", oba w wersji 1) i rysuje ich karty. |
| `Monster.cs`, `Gear.cs` | Zaprojektowane rekordy z nazwanymi (nie pozycyjnymi) właściwościami i `required` na polach obowiązkowych — deserializator `System.Text.Json` jest jedynym walidatorem, zero ręcznie pisanej walidacji w `TryValidate`. |
| `MonsterCardView(.axaml)`, `GearCardView(.axaml)` | Karty złożone z `TraitListView`/`ProseBlockView` z `Desktop/Controls/Content` — patrz sekcja 4b. |

### `tests/`

| Projekt | Zakres |
|---|---|
| `DungeonApp.Core.Tests` | Domena: kampanie, bloki danych, zdarzenia, narzędzie licznika, persystencja, silnik treści (`ContentPackLoader`, `ContentId`). |
| `DungeonApp.Desktop.Tests` | Cache przygotowania biurka, `CounterPanelViewModel`, `RegistryViewModel`, `LoadContentPacksStep`, fragment geometrii paneli, magazyn układu, guard `App.Initialize` na pusty katalog treści. |
| `DungeonApp.Architecture.Tests` | Cztery pliki testujące granice między `Core`, `Desktop` i zestawami treści jednocześnie — patrz sekcja 9. |
| `DungeonApp.Content.Dnd5e.Tests` | `Dnd5eContentSet` na prawdziwych plikach paczki: deserializacja poprawnych wartości i odrzucenie złych, wyłącznie przez `System.Text.Json`. |

---

## 4a. Model domenowy — kampania, bloki danych, zdarzenia, narzędzia

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
(„zamrożoną" — obiektowe kształty budowane jako `ImmutableDictionary`, liczby normalizowane do
`long`/`double`, żeby dwa zapisy tego samego pola nigdy nie zostawiły różnych typów CLR) oraz
publikuje `DataBlockChanged(id)`. Błąd walidacji (`DataBlockShapeMismatchException`) nie zmienia
stanu. Odczyt/zapis nieznanego builder-owi bloku rzuca; odczyt/zapis bloku oznaczonego jako
`unreadable` (bo starsza/nieznana wersja z dysku) rzuca `DataBlockUnreadableException` — nie ma
cichego nadpisania nieodczytanej zawartości. Oddzielna ścieżka `Hydrate` (nie `Apply`) odtwarza stan
z dysku — nie publikuje żadnego zdarzenia, bo wczytanie zapisu nie jest zmianą, którą ktokolwiek
wykonał w tej sesji.

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

## 4b. Model domenowy — silnik treści

To dziś najbogatsza i najświeższa część `Core`, więc opisana osobno i szczegółowo. Rzecz, którą
warto zrozumieć zanim cokolwiek inne: **silnik nie wie, czym jest potwór.** Wpis niesie
nieprzejrzaną kopertę wartości (`ContentValues`); jedynym kodem, który tę kopertę kiedykolwiek
otwiera, jest zestaw treści nazwany w referencji typu wpisu. Ta zasada biegnie przez trzy warstwy:

```
Core/Content            — nosi kopertę, nigdy jej nie otwiera
Desktop/Content         — agreguje wiele zestawów w jeden katalog/prezentację, nadal nie otwiera
DungeonApp.Content.Dnd5e — jedyny kod, któremu wolno wywołać ContentValues.Read<Monster>()
```

**Paczka na dysku** (`ContentPackLoader`, katalog per paczka pod `Documents\DungeonApp\Packs`):

```
pack.json            # { formatVersion, id, name, version: { major, minor } }
entries/*.json        # { id, name, template: "zestaw:typ", templateVersion, values: {...} }
```

Katalog `templates/`, jeśli ktoś go zostawi, jest po cichu ignorowany — typy treści nie pochodzą
już z paczek, tylko z kodu, więc taki katalog jest martwy, nie błędny. Pole `values` jest surowym
JSON-em, zawijanym w `ContentValues` bez interpretacji.

**Wczytywanie jest dziś jednoprzebiegowe** (nie dwuprzebiegowe, jak w poprzedniej wersji tego
dokumentu) — bo nie ma już kolizji między szablonem a wpisem do wykrywania: paczka niesie tylko
wpisy. `ContentPackLoader.LoadAsync` skanuje katalogi alfabetycznie, dla każdego wczytuje i
waliduje `pack.json`, potem każdy plik w `entries/` osobno.

**Trzy zakresy odrzucenia — i to jest dziś najważniejsza rzecz w tej klasie.** Odpowiadają
dokładnie tabeli z `architecture.md`, sekcja „Co się dzieje, gdy treść jest zepsuta":

| Zakres | Co go wywołuje | Gdzie ląduje |
|---|---|---|
| **cała paczka** | `pack.json`: brak pliku, niepoprawny JSON, nieznany `formatVersion` lub klucz, zły id/nazwa/wersja, przekroczony rozmiar. Poza manifestem: katalog `entries/` niedający się wylistować, więcej niż `MaxItemsPerPack` plików, kolizja id z inną zainstalowaną paczką | `RejectedPacks` |
| **jeden plik** | niepoprawny JSON, nieznany klucz, brak lub zły `id`, brak nazwy, złe odwołanie do typu, brak `templateVersion`, przekroczony rozmiar, plik nie do odczytu | `RejectedEntries` |
| **jeden wpis** | plik sam w sobie poprawny, ale nie daje się związać z typem treści | `Entries`, jako `RegisteredEntry` nierozwiązany |

Pierwszy zakres pilnuje własności paczki **jako całości** — bez tożsamości z manifestu nie ma czym
adresować zawartości, a limit liczby plików nie jest defektem żadnego z nich z osobna. Drugi to
`RejectedEntry(Pack, Location, Reason)`: to samo pojęcie co `RejectedPack`, piętro niżej, z podziałem
ról między polami — `Location` mówi **który** plik, `Reason` mówi **co** jest nie tak i nazwy pliku
już nie powtarza (wyjątek: gdy defektem jest kolizja id, `Reason` wymienia pliki kolidujące, ale nie
ten własny). Komunikat odrzucenia **paczki** nadal nazywa plik sam, bo `RejectedPack.Location` niesie
katalog, a nie plik. Trzeci to cztery powody z `EntryUnresolvedReason`
(`MissingSet/MissingType/TypeVersionMismatch/ValuesRejected`), z opcjonalnym `UnresolvedDetail`
niosącym wyjaśnienie zestawu treści dla ostatniego przypadku.

**Zduplikowany id wpisu w obrębie paczki odrzuca wszystkie kolidujące pliki**, nie tylko drugi
z kolei, i żaden z nich nie zostaje zarejestrowany. Powód nie jest kosmetyczny: `RegisteredEntry`
nie niesie nazwy pliku, więc przepuszczenie któregokolwiek zabrałoby jedyną informację, po której
da się to naprawić — a przepuszczenie obu wstawiłoby do rejestru dwa wpisy pod jednym
`EntryAddress`, czyli adres przestałby adresować. Kolizja jest widoczna dopiero po sparsowaniu
wszystkich plików, więc wykrywa ją drugi przebieg po wynikach — dokładnie tak, jak kolizję id
**między** paczkami wykrywa przebieg po wszystkich katalogach.

Jeden przypadek graniczny jest domknięty jawnie: paczka odrzucona dopiero przez kolizję id między
paczkami została wcześniej wczytana w całości, więc jej `RejectedEntry` **też** nie trafiają do
rejestru. Cały katalog jest poza grą, razem z zawartością.

Dopiero gdy plik wpisu sam w sobie jest poprawny, `ResolveEntries` sprawdza go wobec katalogu typów
w jednym przebiegu czterech kroków — `IContentTypeCatalog.HasSet` → `TryGet` → zgodność wersji →
`TryValidate`. Żaden z czterech powodów nie odrzuca paczki, w której wpis się znalazł — to jest
odwrócenie „odrzucania całej paczki za jeden wadliwy wpis" zapisane w `docs/decisions.md` (poz. 30).

**`IContentTypeCatalog`** to jedyne okno silnika na typy treści: `HasSet` (czy jakikolwiek
zainstalowany zestaw odpowiada na ten identyfikator), `TryGet` (metadane typu — referencja, nazwa,
wersja, nigdy kształt), `TryValidate` (poproś właściwy zestaw o werdykt, bez oddawania
zmaterializowanego rekordu silnikowi, bo silnik nie ma typu, żeby go przyjąć). `ContentValues.Read<T>`
deserializuje dwa razy w skrajnym przypadku (raz do walidacji, raz do rysowania karty) — zaakceptowany
koszt przy skali setek wpisów.

**Rejestr** (`ContentRegistry`) niesie cztery listy: `Packs` (te, które przeszły), `Entries`
(wszystkie z każdej paczki, rozwiązane i nierozwiązane razem), `RejectedEntries` (pliki, które nie
stały się wpisem) i `RejectedPacks`. Podział między drugą a trzecią jest tą samą granicą co wyżej:
w `Entries` leży plik, który sparsował się poprawnie, ale nie dał się związać z typem treści;
w `RejectedEntries` — plik, którego w ogóle nie dało się przeczytać jako wpis. Budowany raz na
start, bez sposobu dopisania paczki do istniejącego rejestru — nowy skan oznacza nowy rejestr, tak
samo jak `DataBlockRegistry`.

**Po stronie Desktop:** `IContentSet : IContentTypeCatalog, IContentPresentation` to widok
kompozycji na jeden zainstalowany zestaw — zna siebie (`Id`) i potrafi zarówno odpowiadać na pytania
o typy, jak i narysować kartę (`CreateCard(Entry) : Control`). `ContentTypeCatalogAggregate` i
`ContentPresentationAggregate` zamieniają listę zestawów w pojedynczy katalog/prezentację,
znajdując właściciela po `ContentTypeReference.Set` — to one wypełniają jednoinstancyjne sloty,
których oczekują `ContentPackLoader` i `RegistryViewModel`, nigdy nie ujawniając korzeniowi
kompozycji, że zestawów mogło być więcej niż jeden.

**Karta jest gotowym `Control`-em, nie listą elementów do zinterpretowania.** `IContentPresentation.CreateCard`
zwraca skończony widok — powłoka (`RegistryViewModel`) nie ma niczego do przełączania na podstawie
typu wpisu, bo fizycznie nie ma czego introspekcjonować: wynik to już `Avalonia.Controls.Control`.
To strukturalna, nie tylko konwencjonalna, strona zakazu „narzędzie nie introspekcjonuje typu treści".
Zestaw treści komponuje kartę we własnym `.axaml` z dwóch współdzielonych kontrolek w
`Desktop/Controls/Content`:

- **`TraitListView`** — opcjonalny tytuł nad listą par etykieta/wartość (`TraitRow`, z opcjonalną
  drugą wartością w nawiasie, np. `KP 15 (zbroja skórzana, tarcza)`), w układzie jeden-na-wiersz albo
  kompaktowej siatce trzykolumnowej (`Compact`). `Compact` ustawia projektant karty w XAML-u — nigdy
  paczka; to jawne odwrócenie dawnej flagi szablonu, o którym mówi komentarz w kodzie i
  `docs/architecture.md`, „Kontrolki, nie katalog".
- **`ProseBlockView`** — opcjonalny tytuł nad blokiem zwykłego tekstu.

Obie kontrolki są same swoim `DataContext` (nigdy nie dziedziczą go po rodzicu) i nigdy nie noszą
własnego marginesu — odstęp między nimi ustawia zawsze host (karta), tak samo jak w rejestrze i na
biurku. `MonsterCardView` i `GearCardView` w `DungeonApp.Content.Dnd5e` to jedyny dziś kod,
który je komponuje: siedem bloków dla potwora (rozmiar/typ/charakter bez tytułu, obrona i ruch,
cechy w siatce kompaktowej, biegłości i zmysły, trzy bloki prozy), dwa dla przedmiotu.

**Ekran rejestru** (`RegistryViewModel`) buduje **jedną** listę wierszy z dwóch źródeł: najpierw
`ContentRegistry.Entries`, posortowane deterministycznie (nazwa, potem pełny adres) niezależnie od
kolejności skanu loadera, potem `RejectedEntries`, posortowane po id paczki i lokalizacji pliku.
`RegistryEntryRowViewModel` niesie więc dwa rodzaje wiersza i pilnuje tego dwiema fabrykami nad
prywatnym konstruktorem — dokładnie tą samą dyscypliną, którą `RegisteredEntry` wymusza „albo typ,
albo powód, nigdy oboje".

Wiersz pliku niewczytanego pokazuje w miejscu nazwy lokalizację pliku, a w miejscu adresu **samo id
paczki** — nie ma id wpisu, z którego dałoby się zbudować `EntryAddress`, więc to pole odpowiada na
to samo pytanie („gdzie to leży") jedyną częścią, która istnieje. Adresopodobny ciąg sklejony ze
ścieżki byłby rzeczą, która nie jest adresem, wstawioną w pole znaczące adres.

Grupę otwiera nagłówek `NIE WCZYTANE`, niesiony przez **pierwszy** wiersz niewczytany
(`ShowsNotLoadedHeader`) — lista zostaje jedną listą z jednym `DataTemplate`, bez grupowania
i bez drugiego `ListBox`-a, kosztem tego, że ten jeden wiersz jest wyższy od pozostałych.

Dla wybranego wiersza ekran albo woła `IContentPresentation.CreateCard` (wpis rozwiązany), albo
składa komunikat po polsku: osobny dla każdego z czterech powodów nierozwiązania
(`DescribeUnresolved`), jeden wspólny dla pliku niewczytanego (`DescribeNotLoaded`), oprawiający
diagnostykę loadera zamiast ją tłumaczyć. Oba idą tym samym kanałem (`UnresolvedMessage`).

`ShowSelectionPrompt` gasi zaproszenie „wybierz coś z listy" na pustym rejestrze, żeby nie stać
obok zdania tłumaczącego, czemu lista jest pusta. Rejestr złożony wyłącznie z zepsutych plików
**nie** jest pusty — lista ma co pokazać, więc komunikat „nie ma jeszcze żadnej paczki" się nie
pojawia.

**`RejectedPacks` nie są dziś wyświetlane nigdzie.** To świadoma decyzja autora przy tym kroku,
nie przeoczenie — ale zostaje jako jedyna pozycja z tabeli „Co się dzieje, gdy treść jest zepsuta",
o której Mistrz Gry nie dowiaduje się z aplikacji.

---

## 5. Persystencja

### Kampanie — `JsonCampaignRepository`

Każda kampania to własny katalog: `<libraryPath>/<CampaignId:D>/`. W nim:

```
campaign.json                # manifest
datablocks/<blockId>.json    # jedna wartość na plik
```

**Manifest** (`CampaignManifest`, rekord wewnętrzny) niesie: `FormatVersion` (dziś zawsze `1`),
`Id`, `Name`, `CreatedAt`, `Generation` (licznik generacji zapisu) i listę
`DataBlockEntry(Id, Version, Generation)`. **Nic ponadto** — manifest niesie dokładnie to, co ten
build zapisuje, i ani jednego pola trzymanego otworem dla przyszłego.

Dwa pola zarezerwowane, `Ruleset` i `ContentPacks`, zostały usunięte 2026-09-13; opis tego, po co
istniały i w jakim kształcie ma wrócić deklaracja paczek kampanii, jest w `decisions.md`, pozycja
o zarezerwowanych polach manifestu. Odczyt manifestu jest wyrozumiały (nie ustawia
`JsonUnmappedMemberHandling.Disallow`, w przeciwieństwie do `ContentPackLoader`), więc starszy plik
niosący tamte klucze wczytuje się dalej — pilnują tego dwa testy
w `JsonCampaignRepositoryTests`: jeden sprawdza, że nowy zapis tych kluczy nie niesie, drugi że
plik w starym kształcie nadal się otwiera. Wersja formatu została przy `1`, bo usunięcie pola
z pobłażliwego odczytu nie jest zmianą łamiącą.

**Wartość bloku** (`DataBlockDocument`) niesie `BlockId`, `Version`, `Generation` i sam `JsonNode`
zserializowany przez `DataBlockValueSerializer` **zgodnie z zarejestrowanym kształtem**, nigdy „jak
wyszło z parsera" — to jest to, co gwarantuje, że liczba całkowita nie wróci jako `double` po
przejściu przez JSON.

**Zapis (`SaveAsync`)** jest transakcyjny na poziomie plikowym i idzie w całości przez
`AtomicWrite`: każdy plik (bloki + manifest) jest najpierw zapisywany jako `*.writing.tmp`, dopiero
po pomyślnej serializacji wszystkich plików następuje seria atomowych `File.Move(..., overwrite: true)`.
Manifest jest dodawany **ostatni** i dzięki kolejności zatwierdzania ląduje ostatni — to on oznacza
całą generację jako zatwierdzoną. (Ta kolejność jest własnością `AtomicWrite.Commit` i jest tam
zamrożona testem — gdyby przestała obowiązywać, przerwany zapis przestałby być wykrywalny.) Bloki, których build nie mógł odczytać (`unreadable`),
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
przenośnym, kopiowalnym dokumentem, a nie ukrytym stanem aplikacji.

### Paczki treści — `ContentPackLoader`

Paczki leżą obok kampanii, nie pod nimi: `%USERPROFILE%\Documents\DungeonApp\Packs`, ustawione w
`App.axaml.cs` tak samo jak katalog kampanii, bo paczka jest dokumentem użytkownika na tych samych
prawach. Ich format opisuje sekcja 4b. Wczytywanie nie jest transakcyjne w sensie plikowym (to
odczyt, nie zapis) — jego bezpieczeństwem jest walidacja przy starcie, limity rozmiaru pliku
(1 MB) i liczby wpisów w paczce (10 000), oraz zasada „cała paczka albo nic".

### Układ biurka — `WorkspaceLayoutStore`

JSON per biurko (`%LocalAppData%\DungeonApp\layouts\<sanitized-id>.json`), zapis atomowy przez
wspólny `AtomicWrite` (nie własna kopia wzorca), wersjonowany dokument. Obie ścieżki odczytu
(`Load` i `LoadAsync`) różnią się wyłącznie sposobem sięgnięcia po plik — mapowanie dokumentu na
układ jest jedno, wspólne. Odczyt nigdy nie rzuca —
nieznana wersja/uszkodzony plik = `WorkspaceLayout.Empty` (użyj domyślnych). Zapisy są debounce'owane (`WorkspaceLayoutSession`,
750 ms) i odpalane na wątku UI (uzasadnione w komentarzu: plik jest mały, snapshot musi czytać stan
ViewModelu bezpośrednio).

---

## 6. Warstwa desktopowa

**Kompozycja zależności** dziś biegnie przez dwa pliki, nie jeden. `DungeonApp.App/Program.cs` woła
`AppBuilder.Configure(() => new DungeonApp.Desktop.App(BuildContentSets()))` — przeciążenie z
fabryką, nie parametr generyczny — właśnie po to, żeby dało się przekazać skompilowaną listę
zestawów treści (`[new Dnd5eContentSet()]`) do konstruktora `App`, zamiast trzymać ją w jakimś
mutowalnym miejscu odkrywanym później. `App` ma też bezparametrowy konstruktor `App() : this([])`
istniejący wyłącznie na potrzeby projektanta XAML/hot reloadu Avalonii; poza trybem projektowania
`Initialize()` rzuca głośno (`InvalidOperationException` po polsku, wskazujący `DungeonApp.App/Program.cs`
jako miejsce naprawy), jeśli lista zestawów jest pusta — sprawdzone testem
(`AppTests.Initialize_throws_and_names_the_composition_root_when_built_without_any_content_set`).

Dalszy ciąg kompozycji w `App.axaml.cs`, wciąż bez kontenera DI, wszystko ręcznie w polach `App`:
katalog danych aplikacji (`%LocalAppData%\DungeonApp`) → `WorkspaceLayoutStore`; `CounterTool` +
`DataBlockRegistry` rejestrujący jego blok (z walidacją, że narzędzie nie używa niezarejestrowanego
bloku); katalog biblioteki kampanii (`Documents\DungeonApp\Campaigns`) → `JsonCampaignRepository`;
katalog paczek treści (`Documents\DungeonApp\Packs`) + `ContentTypeCatalogAggregate` nad listą
zestawów → `ContentPackLoader`, opakowany w `LoadContentPacksStep`; `ContentPresentationAggregate`
nad tą samą listą zestawów, trzymany dla ekranu rejestru; `CampaignWorkspacePreparationCache`
(dzielony między rozgrzewkę startową i prawdziwe otwarcie); `CampaignLibraryViewModel` — zbudowana w
korzeniu kompozycji, mimo że jej callback otwarcia kampanii domyka się nad polem `_shell`, które w
tym momencie **jeszcze nie istnieje** (rozwiązywane leniwie, dopiero przy pierwszym kliknięciu,
długo po tym jak `OnFrameworkInitializationCompleted` zdąży zbudować `_shell`). Na końcu — jawna
tablica pięciu kroków startowych, gdzie **kolejność w tablicy jest kolejnością wykonania**.

**`AppShellViewModel`** — właściciel „gdzie jest GM": `IsReady`/`IsStarting` (gate startowy),
`CurrentWorkspaceContent` (biblioteka, rejestr treści, `CampaignWorkspaceViewModel`, albo placeholder
sekcji), `TopBar`/`Sidebar`/`StatusBar`. `RunStartupAsync` iteruje kroki startowe sekwencyjnie,
oddając sterowanie dispatcherowi między nimi; błąd dowolnego kroku **nie blokuje wejścia** —
degraduje do zwykłego leniwego wczytywania (`CompleteStartupWithWarning`). Ekran rejestru
(`RegistryViewModel`) jest budowany leniwie, dopiero przy pierwszym wejściu w sekcję „Rejestr" (nie
w konstruktorze powłoki) — bo w momencie budowy powłoki start jeszcze nie wczytał paczek — i trzymany
potem przez cały czas życia powłoki, tak samo jak biurko kampanii. `OpenCampaignAsync` bierze
przygotowany stan z cache'a, buduje `CampaignSession` i `CampaignWorkspaceViewModel`.
`CloseCampaignAsync` flushuje układ, dispose'uje biurko, wraca do biblioteki i odświeża
podsumowania (mogły się zmienić, gdy kampania była otwarta).

**`CampaignSession`** — jedyna droga zmiany otwartej kampanii (patrz sekcja 4a). Nie jest
przechowywana w Core świadomie: „aktualnie otwarta kampania" to stan powłoki, nie domeny.

**Sekwencja startowa (`Startup/*`, `IStartupStep`)** — pięć kroków, cała żyje w Desktop (Core
nie wie, że start istnieje), potwierdzone bezpośrednio w tablicy `_startupSteps` w `App.axaml.cs`:

0. `LoadContentPacksStep` — wczytuje i waliduje paczki **przed** półką kampanii, buduje
   `ContentRegistry` i trzyma go dla powłoki (`Registry`). Bez własnego `try/catch`: wadliwa paczka
   nie jest wyjątkiem, tylko pozycją w `RejectedPacks`, więc „odrzucona paczka nie blokuje startu"
   wynika z kształtu loadera, a nie z łapania błędów tutaj.
1. `LoadCampaignLibraryStep` — wczytuje półkę (`ICampaignRepository.ListAsync` przez
   `CampaignLibraryViewModel.LoadAsync`), trzyma wynik dla kolejnych kroków.
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
(praca bez UI) i `ApplyAsync` (dotknięcie żywego drzewa). **W kodzie nie ma klasy
`WarmPanelVisualStep`** — nie istnieje pod tą ani żadną inną nazwą w `Startup/`; komentarz w
`WarmCampaignWorkspaceVisualStep` tłumaczy, czemu rozgrzewka per-typ-panelu nie miałaby sensu: panele
na biurku pochodzą z zapisanego układu użytkownika, więc nie da się ich wyliczyć statycznie per typ,
stąd rozgrzewka bierze cały widok biurka naraz.

**`CampaignLibrary` vs `CampaignWorkspace` vs `Registry`** — trzy odrębne ekrany podpięte pod
`AppShellViewModel.CurrentWorkspaceContent`. Biblioteka to płaski ekran „zaplecza" (lista, tworzenie,
usuwanie, otwieranie kampanii) bez żadnej logiki domenowej poza wywołaniem `CreateCampaign`/
`ICampaignRepository`. Rejestr to encyklopedia treści (sekcja 4b). Biurko
(`CampaignWorkspaceViewModel`) to właściwy „stół" z panelami.

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

**Motyw (`Themes/*.axaml`)** — `Tokens.axaml` (kolory/skale/rozmiary jako zasoby), `BuiltInControls.axaml`
i `DungeonControls.axaml` (style kontrolek, w tym `PanelWindow`), `Icons.axaml`. Rozmiar plików
(110–280 linii) sugeruje realnie rozbudowany system wizualny, nie prowizorkę.

---

## 7. Kluczowe przepływy end-to-end

### (a) Start aplikacji
`DungeonApp.App/Program.Main` → `AppBuilder.Configure(() => new App(BuildContentSets()))...StartWithClassicDesktopLifetime`
→ `App.Initialize()` (sprawdza niepustość listy zestawów, buduje wszystkie zależności i tablicę
pięciu `IStartupStep`) → `App.OnFrameworkInitializationCompleted` tworzy `AppShellViewModel`, ustawia
je jako `DataContext` `MainWindow`, podpina flush układu na `ShutdownRequested`/`Closing`.
`AppShellView.OnLoaded` (odpalane raz, po pierwszym renderze lekkiej powłoki) woła
`viewModel.RunStartupAsync(new StartupUiContext(WarmupHost))`, który iteruje pięć kroków startowych
sekwencyjnie, oddając sterowanie dispatcherowi między nimi (pasek postępu
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

### (d) Wczytanie paczki i wybór wpisu w rejestrze
Start aplikacji, krok 0 (`LoadContentPacksStep`) → `ContentPackLoader.LoadAsync` skanuje
`Documents\DungeonApp\Packs`, dla każdej paczki wczytuje `pack.json` i `entries/*.json`, rozwiązuje
każdy wpis wobec `ContentTypeCatalogAggregate` → `ContentRegistry` trzymany przez krok. Wejście GM-a
w sekcję „Rejestr" → `AppShellViewModel.OnSectionSelected` buduje leniwie
`new RegistryViewModel(registry, presentation)` → lista wierszy: wpisy posortowane po nazwie
i adresie, za nimi pliki niewczytane pod nagłówkiem `NIE WCZYTANE`.
Kliknięcie wiersza → `SelectedEntry` → `RecomputeCard`: wpis rozwiązany woła
`IContentPresentation.CreateCard(entry)` (agregat znajduje `Dnd5eContentSet`, ten deserializuje
`ContentValues` do `Monster`/`Gear` i buduje `MonsterCardView`/`GearCardView` z kontrolek
`TraitListView`/`ProseBlockView`); wpis nierozwiązany pokazuje jeden z czterech komunikatów po
polsku, a plik niewczytany — komunikat oprawiający diagnostykę loadera. W obu przypadkach bez
żadnej karty.

### (e) Przesunięcie/zadokowanie panelu i zapamiętanie układu
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

### Dodanie nowego typu treści (bez zmiany silnika)

Na podstawie `Dnd5eContentSet`/`Monster`/`MonsterCardView`:

1. **Rekord wartości** w projekcie zestawu treści (nowym albo istniejącym `DungeonApp.Content.*`):
   właściwości nazwane, nie pozycyjne, `required` na polach obowiązkowych — deserializator jest
   walidatorem.
2. **Deklaracja typu** w `IContentSet.TryGet`/`TryValidate`/`CreateCard`: nowy `ContentTypeDescriptor`
   z własnym `ContentId`, nazwą wyświetlaną i wersją; dopisanie gałęzi w trzech metodach zestawu
   (jedyne legalne miejsce branżowania po identyfikatorze typu treści w całej aplikacji).
3. **Widok karty** (`.axaml` + code-behind z `SetXxx(model)`): kompozycja z `TraitListView`/`ProseBlockView`
   z `Desktop/Controls/Content` — spacing między nimi ustawia host, żadna z kontrolek nie niesie
   marginesu.
4. **Testy**: analogicznie do `Dnd5eContentSetTests` (deserializacja poprawnych wartości, odrzucenie
   nieznanego klucza, odrzucenie brakującego pola wymaganego) — wszystkie trzy bez linijki ręcznej
   walidacji w kodzie produkcyjnym.
5. **Granica słownictwa**: `CoreEntryKindIndependenceTests` czerpie zakazane słowa z publicznych
   nazw typów każdego załadowanego zestawu `DungeonApp.Content.*` — nowy typ automatycznie poszerza
   zakaz o swoją nazwę, żadnej ręcznej rejestracji nie trzeba robić.

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
   — te drugie są dziś jednym z najlepiej pokrytych plików w projekcie i warto je traktować jako
   wzorzec kompletności (przepełnienie, błąd zapisu, nieodczytywalny blok, wyścig zapisów, dispose).

### Dodanie nowego panelu bez nowego narzędzia domenowego
Jeśli panel nie potrzebuje własnego bloku danych (np. czysto prezentacyjny), kroki 3–6 powyżej się
stosują bez kroków 1–2.

Uwaga: katalog paneli jest dziś budowany **per otwarta sesja kampanii**
(`PanelCatalog.For(session)`), więc każdy nowy panel domyślnie trafia na biurko każdej kampanii —
nie ma dziś mechanizmu włączania/wyłączania paneli per kampania czy per zestaw paczek. Manifest
kampanii nie ma dziś żadnego pola, które by go obsługiwało — dwa zarezerwowane pod to pola zostały
usunięte, bo przez cztery sesje nie doczekały konsumenta. Filtrowanie narzędzi per kampania jest
zresztą świadomie odłożone do drugiego zestawu treści (`architecture.md`, „Narzędzia biurka
i system okien").

---

## 9. Strategia testów

| Warstwa | Plik(i) | Co pokrywają |
|---|---|---|
| Domena — kampanie | `CampaignTests`, `CampaignNameTests`, `CreateCampaignTests` | Tworzenie/odtwarzanie, walidacja nazwy, use case tworzenia. |
| Domena — bloki danych | `CampaignDataBlocksTests` (32: 22 `[Fact]` + 10 przypadków `[Theory]`), `DataBlockIdTests`, `DataBlockRegistryTests`, `DataBlockShapeTests` (24: 12 `[Fact]` + 12 przypadków `[Theory]`) | Cykl `Apply`/`Read`/`Hydrate`, zamrażanie i normalizacja wartości, `unreadable`, walidacja kształtów, znakowe ograniczenia ID. |
| Domena — zdarzenia | `CampaignEventsTests` (12) | Kolejność subskrypcji, kaskada, limit `MaxEventsPerCommand`. |
| Domena — narzędzie | `CounterToolTests` (7: 5 `[Fact]` + 2 przypadki `[Theory]`) | Increment/decrement, przepełnienie. |
| Domena — treść | `ContentPackLoaderTests` (26: 23 `[Fact]` + 3 przypadki `[Theory]`), `ContentIdTests` (16: 3 `[Fact]` + 13 przypadków `[Theory]`) | Wczytywanie i walidacja na prawdziwym systemie plików (`Fakes/TemporaryPacks`), manifest paczki, wszystkie cztery powody nierozwiązania wpisu, kolizja id paczki, limity rozmiaru pliku i liczby wpisów, akceptacja paczek-fixture'ów `tests/DungeonApp.Core.Tests/Packs/{dnd5e,goblinoids}` jako test wykonywalnej specyfikacji formatu. |
| Persystencja | `JsonCampaignRepositoryTests` (12), `DataBlockPersistenceTests` (12), `AtomicWriteTests` (8) | Zapis/odczyt na prawdziwym systemie plików (`Fakes/TemporaryLibrary` — świadomie nie mockuje FS), torn save, nieznane/nieaktualne wersje bloków, zarezerwowane pola `ruleset`/`contentPacks` w zapisanym JSON-ie. |
| Architektura | `CoreIndependenceTests` (1), `ContentAssemblyReferenceTests` (2), `ContentAssemblyIsolationTests` (1), `CoreEntryKindIndependenceTests` (2), `VocabularyWordBoundaryTests` (7 przypadków `[Theory]`) — razem 13 | Cztery granice na raz: `Core` bez Avalonii; `Core`/`Desktop` bez referencji do żadnego zestawu treści; zestawy treści nigdy nie referencjonują się nawzajem; `Core`/`Desktop` (`.cs` i `.axaml`) nie nazywają żadnego rodzaju wpisu — słownik zakazanych słów budowany częściowo z refleksji po publicznych typach zainstalowanych zestawów, więc poszerza się sam wraz z przybywającą treścią. Piąty plik (`VocabularyWordBoundary`) to sama logika granicy CamelCase, nie test. Projekt istnieje osobno od `Core.Tests`/`Desktop.Tests` z jednego powodu wypisanego w jego `.csproj`: test widzący wszystkie warstwy naraz nie może mieszkać w warstwie, którą częściowo ogranicza. |
| Desktop — rejestr | `RegistryViewModelTests` (13), `LoadContentPacksStepTests` (5) | Sortowanie wierszy, nazwy pakietu/typu na wierszu, karta jako nieprzejrzysty `Control` budowany przez fałszywy `IContentPresentation` (`FakeContentSet`), cztery osobne komunikaty nierozwiązania, pusty rejestr, zaproszenie do wyboru gaszone na pustej liście, pliki niewczytane na końcu listy w deterministycznej kolejności, nagłówek na dokładnie jednym wierszu, rejestr złożony z samych zepsutych plików jako niepusty, krok startowy wobec paczki wadliwej obok poprawnej. |
| Desktop — panel licznika | `CounterPanelViewModelTests` (9) | Stan początkowy, odświeżenie po zdarzeniu zewnętrznym, przepełnienie, błąd zapisu na dysk, nieodczytywalny blok, wyścig zapisów, dispose. |
| Desktop — geometria | `PanelGeometryTests` (2) | Tylko `FitInto` (dopasowanie do min/max) — **`ClampMove`, `SnapMove`, `SnapResize`, `ConstrainResize`, `Maximize` nie mają dedykowanych testów jednostkowych**, mimo że to najbardziej złożona czysta logika w warstwie Desktop. |
| Desktop — układ | `WorkspaceLayoutStoreTests` (22) | Zapis→odczyt, a dalej cała łagodna degradacja: brak pliku, uszkodzony JSON, nieznana wersja dokumentu, `MaxPanels` przy odczycie i przy zapisie, panel bez `descriptorId`, pusty `instanceKey`, stan panelu spoza enuma, sanityzacja id. Każdy przypadek po obu ścieżkach odczytu (`Load` i `LoadAsync`) — to te testy pozwoliły scalić mapowanie, które było w magazynie napisane dwa razy. |
| Desktop — cache przygotowania | `CampaignWorkspacePreparationCacheTests` (2) | Rozgrzewka trafia w pierwsze `Take`, kampania usunięta z półki nie blokuje startu. |
| Desktop — korzeń kompozycji | `AppTests` (1) | `App.Initialize()` rzuca głośno i nazywa `DungeonApp.App/Program.cs`, gdy zestaw treści jest pusty poza trybem projektowania. |
| Content.Dnd5e | `Dnd5eContentSetTests` (4) | Deserializacja realnych wpisów fixture'owych do `Monster`/`Gear` z poprawnymi wartościami; odrzucenie nieznanego klucza i brakującej wartości wymaganej — obie ścieżki wyłącznie przez `System.Text.Json`, zero ręcznej walidacji w `Dnd5eContentSet`. |

**Razem: 217 testów, wszystkie zielone** (Core 173, Desktop 27, Architecture 13, Content.Dnd5e 4).

**Co nie jest pokryte w ogóle:** `AppShellViewModel` (sekwencja startowa, przełączanie
sekcji/kampanii/rejestru — zero testów), `CampaignWorkspaceViewModel` (restauracja układu, fitowanie,
minimalizacja/maksymalizacja, z/order — zero testów), `WorkspaceLayoutSession` (debounce, flush —
zero testów), `PanelWindow` (cała logika gestów pointer — wymagałaby testów UI/headless, których nie
ma), sidebary, top bar, status bar (trywialne, ale bez testów), cztery z pięciu kroków startowych
(poza `LoadContentPacksStep`, który testy ma — pozostałe cztery: zero testów jednostkowych,
testowane tylko pośrednio przez uruchomienie aplikacji), `ContentTypeCatalogAggregate` i
`ContentPresentationAggregate` (żaden dedykowany plik testowy — pokryte tylko pośrednio przez
`RegistryViewModelTests`/`LoadContentPacksStepTests`, które używają pojedynczego fałszywego zestawu,
nigdy więcej niż jednego naraz).

**Rola testów architektonicznych:** to nie są testy funkcjonalności, tylko testy **granic**. Ich
jedynym zadaniem jest nie dopuścić, żeby ktoś (człowiek albo agent AI) dodał `using Avalonia` w
`Core`, referencję do zestawu treści w `Core`/`Desktop`, referencję między dwoma zestawami treści,
albo nazwę „Monster"/„Spell"/podobną w kodzie silnika lub powłoki — bez nich każda z tych czterech
granic byłaby tylko deklaracją w dokumentacji, nie czymś wymuszonym przez build.

---

## 10. Ocena stanu

### Dojrzałe
- **Domena (`Core`)** jest zaskakująco dopracowana jak na projekt w tej fazie: jasne rozróżnienie
  `Create`/`Restore`/`Hydrate`, immutability przez zamrażanie i normalizację wartości, spójny model
  błędów (`CampaignRuleException` dla GM-a, `CampaignStoreException`/`DataBlockUnreadableException`
  dla reszty systemu), cztery testy architektoniczne pilnujące granic mechanicznie. Kod jest gęsto
  skomentowany z uzasadnieniem decyzji (nie tylko „co", ale „dlaczego"), co jest nietypowo wysokim
  standardem i utrzymuje się także w nowszym kodzie (silnik treści, `DungeonApp.Content.Dnd5e`).
- **Silnik treści** (`Core/Content`, `Desktop/Content`, `DungeonApp.Content.Dnd5e`) jest dojrzalszy,
  niż sugerowałby jego wiek: granica „silnik nie wie, czym jest potwór" jest dziś strukturalna
  (koperta `ContentValues`, `CreateCard` zwraca gotowy `Control`), nie tylko deklarowana, i pilnowana
  mechanicznie przez skan słownictwa, który poszerza się sam wraz z przybywającymi typami treści.
  `System.Text.Json` jako jedyny walidator (`required` + strict unmapped-member handling) usunął całą
  klasę ręcznie pisanej walidacji, którą trzeba by inaczej utrzymywać przy każdym nowym typie treści.
- **Persystencja** (`JsonCampaignRepository`, `WorkspaceLayoutStore`) — oba magazyny mają realną
  transakcyjność przez jeden wspólny `AtomicWrite` (zapis do pliku tymczasowego + atomowy
  `File.Move`, w jednym miejscu zamiast trzech), wykrywanie przerwanego
  zapisu przez licznik generacji, i celowo łagodną degradację (uszkodzony układ biurka → puste
  domyślne, nie crash; jedna zepsuta kampania nie chowa reszty półki).
- **Geometria paneli** (`PanelGeometry`) — czysta, bezstanowa, wolna od typów Avalonii, z
  nietrywialną, dobrze przemyślaną logiką snapowania/ograniczeń. To najbardziej „inżynierski"
  fragment warstwy Desktop.
- **`CounterPanelViewModel`** i jego testy — jedyny w pełni „domknięty" pionowy przekrój (UI → sesja
  → domena → dysk → z powrotem do UI) z kompletem testów pokrywających ścieżki błędów.

### Rusztowanie / celowo tymczasowe
- **Licznik (`CounterTool`/`CounterPanelViewModel`/`CounterPanelView`) jest jawnie testowym
  rusztowaniem, nie docelową funkcją.** Jest to jedyne miejsce, gdzie cały przepływ od gestu do
  dysku istnieje i jest przetestowany od początku do końca — dlatego pełni rolę wzorca/dowodu
  słuszności architektury dla nowych narzędzi (sekcja 8), a nie funkcji produktu. Trzeba się liczyć
  z tym, że zniknie albo zostanie przeniesiony do testów, gdy pojawi się pierwsze prawdziwe
  narzędzie — `docs/tasks.md` wymienia to wprost jako kolejny krok po domknięciu przejścia treści.
- **Nawigacja boczna** (`GlobalSidebarViewModel`) ma dziś dwie realne sekcje („Kampanie", „Rejestr");
  każda inna sekcja renderuje `WorkspacePlaceholderView` — dosłowny placeholder bez zawartości. Sama
  metoda `OnSectionSelected` w `AppShellViewModel` nadal nosi komentarz „Temporary scaffolding until
  the real context router exists".
- **Rejestr rysuje encyklopedię, nie kampanię.** Ekran „Rejestr" pokazuje wszystko, co wczytały
  wszystkie zainstalowane paczki, niezależnie od otwartej kampanii — bo nic dziś nie osadza wpisu w
  konkretnej kampanii. To zgodne z tym, co silnik dziś umie, nie błąd — a pole, które kiedyś na to
  czekało, zostało usunięte, żeby wróciło razem ze swoim konsumentem.
- **`WorkspacePanelDescriptor.AllowsMultipleInstances`** istnieje i jest reprezentowalne w zapisanym
  układzie (przez klucz instancji), ale żaden dzisiejszy panel go nie ustawia — zaprojektowane pod
  przyszłość, nieużyte.

### Dług i luki
- **Odrzucone paczki nie docierają do użytkownika.** Loader je odnotowuje (`RejectedPacks`), ekran
  rejestru ich nie pokazuje — jedyna pozycja z tabeli „Co się dzieje, gdy treść jest zepsuta",
  o której Mistrz Gry nie dowie się z aplikacji. Świadoma decyzja autora przy kroku „odrzucanie per
  plik wpisu", nie przeoczenie, ale dług zostaje długiem: paczka odrzucona za literówkę w manifeście
  znika dziś po cichu, a to jest dokładnie to, czego tamta sekcja zakazuje.
- **Pokrycie testami warstwy Desktop jest bardzo nierówne.** `CounterPanelViewModel` ma 9 testów;
  `AppShellViewModel`, `CampaignWorkspaceViewModel`, `WorkspaceLayoutSession`, cztery z pięciu kroków
  startowych i cała logika gestów w `PanelWindow` — zero. To oznacza, że najbardziej złożona
  logika stanu UI (przełączanie kampanii/sekcji, fitowanie paneli, debounce zapisu układu) jest dziś
  weryfikowana wyłącznie ręcznie.
- **Brak migracji wersji bloków danych i formatu manifestu.** Kod jest przygotowany strukturalnie
  (`DataBlockRegistration.Version`, `CampaignManifest.FormatVersion`), ale ścieżka migracji nie
  istnieje — każda zmiana kształtu istniejącego bloku dziś oznacza „stare dane stają się
  `unreadable`", nie „migrują się". Ten sam brak dotyczy typów treści: `TypeVersionMismatch`
  oznacza dziś „wpis trzeba zapisać ponownie", nie ma ścieżki automatycznej migracji wartości.
- **Jedno narzędzie domenowe.** Cały system rozszerzalności (`PanelCatalog`, `ITool`,
  `DataBlockRegistry`) jest zaprojektowany pod wiele narzędzi, ale zweryfikowany tylko przez jedno
  (i to celowo trywialne). Nie wiadomo z kodu, jak dobrze abstrakcje wytrzymają drugie, bardziej
  złożone narzędzie (wielo-blokowe, z zależnościami między blokami, z wielo-instancyjnością).
- **Brak obsługi błędu przy tworzeniu/otwieraniu/usuwaniu kampanii z powodu IO** poza cichym
  połknięciem (`CampaignLibraryViewModel` łapie `IOException`/`CampaignStoreException` w kilku
  miejscach z komentarzem „Persistent feedback belongs to a future error state, not a temporary
  toast" — czyli znany, nazwany, ale nie zaadresowany brak UX błędów).

### Zgodność z README
README opisuje intencję trafnie i bez nadmiernych obietnic. Jedyne miejsca do odnotowania:
- README poprawnie nazywa `DungeonApp.App` jedynym miejscem wymieniającym zestaw treści z nazwy i
  poprawnie opisuje `DungeonApp.Content.Dnd5e` jako istniejący zestaw treści — zgodność potwierdzona
  czytaniem `Program.cs` i `Dnd5eContentSet.cs`.
- README nie wspomina, że pokrycie testami Desktop jest tak nierówne, ani że rejestr dziś nie zależy
  od otwartej kampanii — to nie jest sprzeczność, tylko brak (README nie rości sobie prawa do bycia
  raportem ze stanu, i faktycznie nim nie jest).
- README poprawnie nazywa licznik przykładem narzędzia, nie twierdzi, że to docelowa funkcja.
  Zgodność potwierdzona, ale warto, żeby każdy nowy współpracownik (lub agent) przeczytał to zdanie
  uważnie, bo z samego kodu (dobrze przetestowany, dopracowany UI panelu) łatwo błędnie wywnioskować,
  że licznik jest funkcją docelową.
