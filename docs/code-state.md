# DungeonApp — stan kodu

**Status: sądy o kodzie, nie jego opis.** Ten dokument mówi, co w kodzie jest dojrzałe, co jest
rusztowaniem, gdzie jest dług, czego nie pilnuje żaden test, gdzie naturalna zmiana robi co innego,
niż się wydaje, i jak rozszerza się aplikację. Przy rozbieżności z kodem prawdą jest kod.

Co ma obowiązywać — [architecture.md](architecture.md). Jak kod wygląda — sam kod: struktura
katalogów, nazwy typów i komentarze przy nich odpowiadają szybciej i nie rozjeżdżają się. Tu stoją
wyłącznie rzeczy, których z kodu nie wyczyta się w rozsądnym czasie.

> **Warunek istnienia tego dokumentu:** każda pozycja zmienia decyzję architekta albo treść briefu
> dla wykonawcy. Po etapie dogania się go, usuwając pozycje rozwiązane i dopisując nowe sądy — nie
> opisując zmian. Jeśli dogonienie po etapie wymaga więcej niż kilku zdań, dokument wrócił do
> opisywania i trzeba go przyciąć. Uzasadnienie — [collaboration.md](collaboration.md), *Jak pisać
> dokumenty tego repozytorium*, punkt o stanie kodu.

**Aktualność: 2026-09-23, po etapie 4 przebudowy.**

---

## Ocena stanu

### Dojrzałe

* **Granice ramy, bibliotek i systemu są pilnowane mechanicznie**, w obie strony i od etapu 4
  strukturalnie: rama nie referencuje żadnej biblioteki, więc nie ma jak poznać wpisu. Testy granic
  w `DungeonApp.Architecture.Tests`: rdzeń i logika biblioteki wpisów bez Avalonii; rama nie
  referencuje bibliotek; rama i biblioteki nie referencują systemów; systemy nie referencują siebie
  nawzajem; różne biblioteki nie referencują siebie nawzajem; rama i biblioteki nie nazywają
  rodzajów treści; zakładka kategorii System powstaje bez argumentu.
* **Warstwa treści** — biblioteka wpisów niesie kopertę, której nie otwiera; deserializator jest
  jedynym walidatorem wartości, więc ręcznej walidacji nie ma w ogóle.
* **Zapis** — jeden prymityw zapisu atomowego zamiast ręcznych kopii, wykrywalny rozdarty zapis
  (manifest zatwierdza generację jako ostatni), celowo łagodna degradacja. Format na dysku pilnuje
  test bajt w bajt na wzorcowej kampanii.
* **Instancje i nakładka** — łącze do wpisu z rzadką łatką, rozwiązywane od nowa przy każdym
  odczycie. Dwa miejsca, które napisane odwrotnie wyglądają identycznie, mają własne testy.
* **Geometria paneli** — czysta, bezstanowa, bez Avalonii. Najbardziej inżynierski fragment
  biblioteki biurka — i słabo pokryty (niżej).
* **Rozgrzewka przy starcie jest wyczerpująca, nie próbkowa** — ekrany ramy w obu stanach
  zwinięcia paska, zakładki i karty każdego wkompilowanego systemu, wszystko za kurtyną startową,
  która znika po ostatnim kroku.

### Rusztowanie — celowo tymczasowe

* **`AllowsMultipleInstances`** — czytane przy odtwarzaniu układu, żaden panel go nie ustawia.
* **Dwustopniowy katalog paneli** (najpierw panele biblioteki, potem narzędzia systemu) — pierwsza
  lista jest pusta.
* **Kontrolki kart żyją tylko w bibliotece wpisów** bez drugiego systemu, który by potwierdził, że
  to ich właściwy dom.
* **Zakładka „Ustawienia"** — pusta z woli autora.

### Dług

* **Zmiana nazwy okazu nie ma konsumenta** — czeka w kolejce: [tasks.md](tasks.md), *Czeka na
  miejsce na ekranie*.
* **Błąd we/wy przy operacjach na kampanii** jest połykany; nazwany komentarzem w kodzie.
* **Wymóg „widok narzędzia sprząta po sobie" nie stoi w punkcie styku** — fabryka treści panelu
  oddaje `object`; drugi system pozna wymóg dopiero przez wyciek.
* **Rozgrzewka kart biegnie zaraz po paczkach**, bo rama uruchamia kroki systemu razem — jej awaria
  zabiera pozostałą rozgrzewkę, która przechodzi wtedy w leniwe wczytywanie. Przyjęte bez weta.
* **Kolory stanów mają zapisane znaczenie, pozostałe kolory motywu — nie.** Reguła z *Niezmiennika
  interfejsu* obowiązuje wszystkie.
* **Migracji nie ma** — zbyt nowy format to odmowa odczytu, niezgodna wersja typu treści oznacza
  wpis. Strukturalnie przygotowane, ścieżki brak.

---

## Luki w testach

Czego nie pilnuje nic — to sprawdza się ręcznie albo w aplikacji:

* **Sekwencja startowa jako całość**; z kroków startowych osobny test ma wczytywanie paczek.
* **Przełączanie zakładek po kliknięciu** — testy powłoki obejmują wybór systemu i powrót do wyboru.
* **Biurko poza geometrią:** odtwarzanie układu, dopasowanie, maksymalizacja, kolejność okien,
  opóźniony zapis układu, cała logika gestów okna. Z geometrii pokryte jest tylko dopasowanie do
  min/max — snapowanie, ograniczanie ruchu i maksymalizacja nie.
* **Wyglądu nie pilnuje żaden test** poza paskiem bocznym (pierwsza klatka, zwijanie, etykiety)
  i przyciskiem paska górnego — celowo, wygląd sprawdza autor (`collaboration.md`). Testy renderujące
  nie rasteryzują: próbkowanie pikseli wymaga przełączenia `TestAppBuilder` na Skię.
* **Skan słownikowy granic ładuje zestawy systemu jawnie.** Słownik zakazanych nazw budują zestawy
  `DungeonApp.Content.*` obecne w pamięci; odrzucone `_ = typeof(...)` kompilator wycina, więc do
  2026-09-24 zawartość słownika zależała od tego, czy równoległy test zdążył załadować zestaw — test
  padał sporadycznie w pełnym przebiegu. Zestaw systemu trafia na listę przez `typeof(...).Assembly`.
  Nowy system gry dopisuje tam swój zestaw, inaczej wypada spod skanu.
* **Prawdziwe okno Windows** — ramka, skalowanie, dekoracje systemowe — jest poza zasięgiem testów
  bez ekranu. 2026-09-23 zrzut autora pokazał odstęp, którego pomiar bez ekranu nie pokazywał.

---

## Pułapki

Miejsca, w których naturalna zmiana robi co innego, niż się wydaje.

* **Każdy przycisk dostaje stałą wysokość — z motywu przycisku** (`Themes/DungeonControls.axaml`,
  od porcji 1 fundamentu). `Button` użyty jako wiersz, karta albo element ramy (dziś:
  `system-option-open`, `frame-action`) musi jawnie ustawić wysokość albo ją znieść (`Height = NaN`) —
  usunięcie własnej wysokości odsłania wysokość kontrolki, nie daje rozciągania. Wiersz listy ma
  własną kontrolkę (`ListRow`, wysokość z motywu, inną podaje lista stylem), chip z okienkiem — motyw
  `DungeonChipOpener`, kafelek — `DungeonTile` (wymiar podaje układający).
* **Błąd w motywie kontrolki kompiluje się i wywraca aplikację przy pierwszym użyciu kontrolki** —
  wpisy `DungeonControls.axaml` są budowane leniwie. Przykład: `ControlTheme` nie dopuszcza selektora
  potomka (`^ Typ`) poza szablonem — styl zawartości kontrolki idzie do `BuiltInControls.axaml`.
  Pilnuje tego test budujący każdy wpis motywów (`ControlThemesBuildTests`); motyw w innym pliku
  trzeba do niego dopisać. To samo w widoku: zasób złego typu (liczba tam, gdzie kolumna siatki chce
  `GridLength`) kompiluje się i wywraca widok przy utworzeniu — tak zakładka treści nie otwierała się
  po porcji 3a. Zakładkę treści buduje w oknie `ContentTabViewBuildTests`; widok, którego żaden test
  nie buduje, dociera do autora niesprawdzony.
* **Przygaszenie wyłączonej kontrolki stoi na `^:disabled:not(:disabled :disabled)`, nie na
  `[IsEnabled=False]`** — komenda z `CanExecute` = fałsz daje tylko pseudoklasę, a sam `:disabled`
  dziedziczy się w dół i przygasiłby treść wyłączonego pojemnika drugi raz. Nowy motyw kontrolki
  bierze ten selektor; pilnuje `DisabledOpacityTests`.
* **Nowa biblioteka musi się nazywać `DungeonApp.Library.*`.** Testy „rama nie referencuje
  biblioteki" i skan słownictwa znajdują biblioteki po tym przedrostku; projekt nazwany inaczej
  wypada spod obu po cichu.
* **System ma dwa identyfikatory o tym samym brzmieniu** — tożsamość dla ramy (manifest kampanii)
  i przestrzeń typów treści (pole `template` we wpisie). Znaczą co innego; zmiana jednego nie zmienia
  drugiego.
* **Serializacja jest ścisła w treści i pobłażliwa w manifeście — celowo.** Ujednolicenie w którąkolwiek
  stronę zabiera albo walidację treści, albo możliwość zmiany manifestu bez podbicia wersji.
* **Instancja nie ma trybu „nieodczytywalna"** — defekt w pliku modelu czyni niedostępną całą kampanię.
* **Nieznany plik modelu i katalog `datablocks/` ze starych kampanii zostają nietknięte** — nie
  sprzątać ich przy okazji; kasowanie danych, których się nie rozumie, jest gorsze niż bezwładny plik.
* **Rozgrzane kontrolki są egzemplarzami rzucanymi.** Prawdziwa zakładka buduje własny egzemplarz
  przy pierwszym pokazaniu; współdzielenie ich z rozgrzewką zmieniłoby cykl życia zakładek.
* **`MaxWidth` z domyślnym rozciąganiem centruje element** w szerszym miejscu, zamiast przykleić go
  do lewej; `HorizontalAlignment="Left"` z kolei zwęża go do treści. Ograniczenie szerokości od lewej
  kładzie się na kolumnę siatki. Tak szczegół zakładki treści stał daleko od listy (2026-09-24),
  a test mierzący same szerokości tego nie widział.
* **Element wystający ujemnym marginesem poza wiersz listy z przewijaniem jest przycinany** na krawędzi
  wiersza, niezależnie od `ClipToBounds` przodków — przyczyny nie ustalono. Dlatego kreska
  zaznaczenia wpisu stoi wewnątrz wiersza; na pasku bocznym (bez przewijania) wystaje, jak w mockupie.
* **Pasek boczny budowany jest od nowa przy każdym wyborze systemu** (warunek animacji zwijania);
  stan zwinięcia trzyma rama, tylko w pamięci. Przepięcie istniejącego paska zamiast budowy nowego
  psuje animację.
* **Lista gubi zaznaczenie, gdy model widoku podmienia jej źródło na nowe obiekty wierszy** — stary
  obiekt nie istnieje w nowej liście. Wygląda jak błąd kliknięcia (przycisk „Zapisz" w wierszu
  „odznacza" wiersz), a to przebudowa wierszy po zmianie kampanii (diagnoza z porcji 5, panel
  instancji). Lista z zaznaczeniem zachowuje obiekty wierszy albo przywraca zaznaczenie po kluczu.
* **Najechanie w motywie ramy to `:pointerover:not(.hover-suppressed)`, nie samo `:pointerover`** —
  klasę dokłada `Themes/OpenerHoverRest.cs` otwierającemu zamkniętemu kliknięciem w siebie (spoczynek do
  ponownego wjechania myszą). Nowy motyw otwierającego okienko, który jej nie uwzględni, znów miga.
  Ruch otwarcia okien wyskakujących żyje w kodzie (`Themes/PopupOpenMotion.cs`), nie w stylu — szablon
  `ComboBox` nie przechodzi przez style przy każdym otwarciu.
* **Każde nowe przejście (animacja) w motywie trzeba dopisać do `Themes/ReducedMotion.axaml`** —
  ten plik zdejmuje przejścia, gdy Windows ma wyłączone animacje (`Themes/SystemMotion.cs`, czytane
  raz przy starcie). Przejście, którego tam nie ma, zostaje animowane mimo ustawienia systemu. Test pilnuje
  dokładnie czterech reguł w tym pliku — nowy selektor dopisuje się do reguły tego samego rodzaju
  (strzałka sekcji rozwijanej stoi przy strzałkach list), nowa reguła wymaga zmiany testu w briefie.
  Ruch w kodzie (`OverlayAppearMotion`, `ProgressSweep`) sprawdza `SystemMotion.IsReduced` i tego pliku
  nie dotyczy.
* **Okno potwierdzenia i powiadomienie szukają warstwy `WindowOverlay` przez okno elementu wywołującego**
  (`MainWindow.axaml`) — element w okienku wysuwanym (menu, lista rozwijana) ma własny korzeń, warstwy
  nie znajdzie i wywołanie rzuca wyjątkiem.
* **Rama nie może nazywać rodzajów wpisu — także w danych przykładowych galerii.** Test architektury
  (`CoreEntryKindIndependenceTests`) odrzuca w `DungeonApp.Desktop` nazwy typu „monster”; przykład
  w galerii nazywa się neutralnie (`SampleRow`).
* **Pasek przewijania leży nad treścią** (motyw `ScrollViewer`) — treść sama musi skończyć się przed
  nim. Strefa paska (16) liczy się **od krawędzi obszaru przewijania**, wcięcie kontenera leży w niej,
  nie obok: kontener z wcięciem nadpisuje `DungeonScrollBarZone` na 16 minus wcięcie (okienka —
  `DungeonScrollBarZoneInFlyout` = 12; galeria `ListsSection` — 10; pasek boczny — 0). Wiersz listy
  (`ListBoxItem`, więc też `ComboBoxItem` i `DropDownPicker`) i `MenuItem` robią to w motywie; nowy
  przewijany kontener spoza nich — jawnie. W okienkach strefa jest tylko przy przepełnieniu: `Themes/PopupOpenLayout.cs`
  przy otwarciu daje klasę `.no-bar-zone`, style w `BuiltInControls.axaml` zerują przy niej strefę. Ten
  sam plik ustala szerokość okienka listy z napisów pozycji — `ComboBox` z `ItemTemplate` nie jest
  mierzony (dziś nikt go nie używa); pierwszy taki — do obsłużenia w tym samym miejscu. Okienko `.list-host` nie ma wcięcia — niesie je lista
  w środku, żeby pasek stał 7 od krawędzi jak w każdym okienku. Wielowierszowy `TextBox` ignoruje `Padding`: wcięcie tekstu
  niesie `DungeonMultilineInputContentPadding`.
* **Motywu domyślnego biblioteki nie ma** (od porcji 9a fundamentu). Wbudowana kontrolka użyta po raz
  pierwszy bez motywu ramy nie ma szablonu — jest niewidoczna, nic nie zgłasza błędu. Motyw dostaje
  w `Themes/DungeonControls.axaml`, a typ — do jawnej listy w `BuiltInControlThemesTests`.
* **Rozmiaru i położenia okienek wysuwanych nie sprawdzi test bez okna.** W testach okienko rysuje się
  w warstwie okna (korzeń szerokości okna), w aplikacji na Windows jest osobnym oknem dopasowanym do
  zawartości — dwa błędy (menu wyśrodkowane, szerokość listy przy przewijaniu) przeszły pomiar bez okna.
  Test okienka musi też sam zarejestrować `PopupOpenLayout` i `PopupOpenMotion` — aplikacja włącza je
  tylko przy desktopowym cyklu życia.
* **Przewija jedna warstwa.** Lista w owijającym `ScrollViewer` albo w okienku wysuwanym, które samo
  przewija, daje dwa paski; w okienku z samą listą — `FlyoutPresenterClasses="list-host"`. Galeria,
  „Długa lista” w `DropDownsSection`, wciąż tak stoi.
* **`ScrollViewer.IsScrollChainingEnabled` trzeba ustawić także w motywach, które przekazują ją
  do własnego przewijania** (`ListBox`, `TextBox`) — ich szablon podaje własną wartość i zasłania
  ustawienie z motywu przewijania.

---

## Punkty rozszerzeń

Wzorce do briefów: od którego miejsca zacząć i czego wykonawca nie może pominąć.

### Paczka dostarczana z systemem

Katalog w `src/<projekt systemu>/Packs/<id paczki>/`; `.csproj` systemu kopiuje go do
`<wyjście>\<system>\packs\` (wzór: `DungeonApp.Content.Dnd5e.csproj` — nazwa katalogu systemu wpisana
drugi raz, zgodność z `IdValue` pilnuje `BundledPacksTests`). Test formatu `Loads_the_fixture_packs_cleanly`
czyta ją razem z paczkami wzorcowymi i wymienia id — nowa paczka dopisuje tam swoje. Powtórzone id paczki,
także między źródłami, odrzuca obie.

### Nowy typ treści

Wzorzec: `Dnd5eSystem` / `Monster` / `MonsterCardView`.

1. **Rekord wartości** w projekcie systemu; `required` na polach obowiązkowych — deserializator jest
   walidatorem.
2. **Deklaracja typu** w katalogu typów treści systemu (`IContentTypeCatalog`, w `Dnd5eSystem`) —
   jedyne legalne miejsce rozgałęziania po identyfikatorze typu w aplikacji.
3. **Karta** z kontrolek karty biblioteki wpisów (`DungeonApp.Library.Entries.Desktop`, `Controls/`);
   odstęp ustawia host.
4. **Testy** wzorem `Dnd5eSystemTests`. Słownik zakazanych słów poszerza się sam o nazwę nowego typu.

### Nowa zakładka systemu

Wzorzec: `SystemTabs` / `CampaignTabs` w `Dnd5eSystem`.

1. **Deklaracja** — `SystemTabDeclaration` (kategoria System, fabryka **bez argumentu**, więc
   zakładka nie ma skąd dostać kampanii) albo `CampaignTabDeclaration` (kategoria Kampania, fabryka
   asynchroniczna dostająca `CampaignTabContext`: id kampanii, migawka tylko do odczytu, jedyne
   wejście zmiany, powiadomienia). Id, tytuł, klucz ikony **z istniejącego motywu**.
2. **Fabryka oddaje `ITabContent`** — gotowy `Control` plus `IDisposable`; bez własnego sprzątania
   przez `DelegateTabContent`.
3. **Zakładka ze szkieletu biblioteki** — system składa ją sam z widoku biblioteki i własnych danych;
   wzór: zakładka rejestru z `ContentTabViewModel` (`DungeonApp.Library.Entries.Desktop`, `Features/ContentTab/`) z rejestrem
   i prezentacją kart systemu.
4. **Zwalnianie** zakładek jest sprawą ramy (`ActiveSystemSession`), nie systemu.

### Narzędzie biurka wnoszone przez system

Wzorzec: `Dnd5eSystem.CreateDeskTabAsync` → `BuildTools` → `CampaignDesk.CreateAsync` →
`CampaignInstancesToolView(Model)`. Ścieżka dla narzędzia czytającego pola własnej treści po nazwie —
dlatego mieszka w systemie, nie w bibliotece.

1. **Biurko wystawia systemowi jedno publiczne wejście** — `CampaignDesk.CreateAsync(kontekst,
   magazyn układów, narzędzia)`, oddające gotową zakładkę z wczytanym układem.
2. **Kontekst narzędzia** — `CampaignEntriesContext` (`DungeonApp.Library.Entries.Desktop`), składany
   przez system z `CampaignTabContext` i własnego rejestru: instancje, rejestr, rozwiązywanie,
   powiadomienia i jedne drzwi zapisu. Każdy zapis przez niego, nigdy przez repozytorium.
3. **Narzędzie oddaje gotową kontrolkę, nie ViewModel** — biblioteka nie potrzebuje wtedy szablonu
   znającego typ z systemu.
4. **Widok implementuje `IDisposable`** i zwalnia swój ViewModel z subskrypcjami — biurko sprząta
   tylko to, co jest `IDisposable`.
5. **Deskryptor** z id prefiksowanym nazwą systemu i ikoną z istniejącego motywu.
6. **Stan niezwiązany z wpisem** — własny model stanu w `IGameSystem.StateModels`.

Każde narzędzie systemu trafia na biurko każdej kampanii; włączania per kampania nie ma.

### Krok startowy systemu

Wzorzec: `LoadContentPacksStep`, `WarmContentCardsStep` (`DungeonApp.Library.Entries.Desktop`,
`Startup/`), zgłaszane przez `IGameSystem.StartupSteps`. Kroki systemów biegną przed krokami ramy,
w kolejności zgłoszenia. Krok podaje własny tekst ostrzeżenia (`FailureWarning`); awaria nigdy nie
zatrzymuje startu.

### Przycisk akcji ramy w pasku górnym

Wzorzec: „Zmień system" w `TopBarView`, klasa `frame-action` — kwadrat na wysokość paska, jeden stan:
najechanie, podpowiedź z nazwą akcji. Pułapka ze stałą wysokością przycisków dotyczy go wprost;
geometrię pilnuje `TopBarRenderingTests`.
