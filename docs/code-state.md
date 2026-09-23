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

* **Zakładka rejestru** — przejściowa, do zakładek treści (krok 10 w architekturze).
* **`AllowsMultipleInstances`** — czytane przy odtwarzaniu układu, żaden panel go nie ustawia.
* **Dwustopniowy katalog paneli** (najpierw panele biblioteki, potem narzędzia systemu) — pierwsza
  lista jest pusta.
* **Kontrolki kart żyją tylko w bibliotece wpisów** bez drugiego systemu, który by potwierdził, że
  to ich właściwy dom.
* **Zakładka „Ustawienia"** — pusta z woli autora.

### Dług

* **Odrzucone paczki nie docierają do Mistrza Gry** — jedyna pozycja z tabeli „Co się dzieje, gdy
  treść jest zepsuta", o której się nie dowie. Czeka na zakładki treści.
* **Zmiana nazwy okazu nie ma konsumenta** — operacja istnieje i jest przetestowana, okno „Świat
  kampanii" jej nie woła.
* **Błąd we/wy przy operacjach na kampanii** jest połykany; nazwany komentarzem w kodzie.
* **Wymóg „widok narzędzia sprząta po sobie" nie stoi w punkcie styku** — fabryka treści panelu
  oddaje `object`; drugi system pozna wymóg dopiero przez wyciek.
* **Rozgrzewka kart biegnie zaraz po paczkach**, bo rama uruchamia kroki systemu razem — jej awaria
  zabiera pozostałą rozgrzewkę, która przechodzi wtedy w leniwe wczytywanie. Przyjęte bez weta.
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
* **Testy renderujące** (bez ekranu, przez prawdziwy `App`) sprawdzają tylko to, po co powstały:
  wiersze paska bocznego, zwinięty pasek, wiersze półki, geometrię przycisku paska górnego.
* **Prawdziwe okno Windows** — ramka, skalowanie, dekoracje systemowe — jest poza zasięgiem testów
  bez ekranu. 2026-09-23 zrzut autora pokazał odstęp, którego pomiar bez ekranu nie pokazywał.

---

## Pułapki

Miejsca, w których naturalna zmiana robi co innego, niż się wydaje.

* **Każdy przycisk dostaje stałą wysokość.** Globalny styl `Button` w motywie ramy
  (`Themes/BuiltInControls.axaml`) ustawia wysokość zwykłej kontrolki. Przycisk, który ma się
  rozciągać, musi ją jawnie znieść (`Height = NaN`) — usunięcie własnej wysokości nie daje
  rozciągania, tylko odsłania globalną. Tak wyszedł za niski przycisk paska górnego (2026-09-23).
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
* **Pasek boczny budowany jest od nowa przy każdym wyborze systemu** (warunek animacji zwijania);
  stan zwinięcia trzyma rama, tylko w pamięci. Przepięcie istniejącego paska zamiast budowy nowego
  psuje animację.

---

## Punkty rozszerzeń

Wzorce do briefów: od którego miejsca zacząć i czego wykonawca nie może pominąć.

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
   wzór: zakładka rejestru z `RegistryViewModel` (`DungeonApp.Library.Entries.Desktop`) z rejestrem
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
