# Architektura

Ten dokument mówi **co wolno**: gdzie biegną szwy, co jest egzekwowane
kodem, a co wyłącznie umową. Opis stanu faktycznego — kto od kogo dziś
faktycznie zależy — żyje w `docs/code-map.md` i jest weryfikowany osobno.

Rozjazd między tymi dwoma dokumentami jest sygnałem długu, nie błędem
zapisu.

## Słownik i kierunek

Projekt mówi o **narzędziu, danych i oknie**. Narzędzie jest bezstanową
logiką w `DungeonApp.Core`; dane kampanii mieszkają we wspólnych,
wersjonowanych blokach danych; okno należy do `DungeonApp.Desktop` i
przedstawia dane. Pojęcie „modułu” nie opisuje już żadnego mechanizmu w
kodzie.

## Dwie warstwy

`DungeonApp.Core` nie wie nic o interfejsie. `DungeonApp.Desktop` zna Core
i buduje na nim widoki. Zależność jest jednokierunkowa.

**Egzekwowane:** `CoreIndependenceTests.Core_does_not_reference_Avalonia`
sprawdza przez refleksję, że zestaw `DungeonApp.Core` nie referencjonuje
żadnego assembly zaczynającego się od `Avalonia`.

**Czego test nie łapie:** samego kierunku `Desktop -> Core`. Zabezpiecza go
dziś struktura projektów, nie test — Core nie ma referencji do Desktop,
więc odwrócenie zależności wymagałoby świadomej edycji `.csproj`. To
wystarczające, dopóki projektów są dwa.

## Sekwencja startowa

Start aplikacji to jawna tablica kroków za kontraktem `IStartupStep`
(`Desktop/Startup`), wykonywanych po kolei w kolejności, w jakiej stoją w
tablicy. Kontrakt żyje **w całości w `DungeonApp.Desktop`**, nie w Core —
połowa jego pracy (`ApplyAsync`) z definicji dotyka żywego drzewa Avalonii
(podpina kontrolki pod `WarmupHost`, czeka na `Loaded`), więc Core, który nic
o UI nie wie, nie mógłby tego kontraktu wystawić bez złamania szwu z sekcji
wyżej. To, że część kroków (np. wczytanie biblioteki kampanii) nie dotyka UI
wcale, nie przenosi ich do Core: to wciąż elementy jednej, jawnie
uporządkowanej sekwencji startu interfejsu, nie osobny mechanizm domenowy.

Każdy krok rozdziela dwie fazy:

- `PrepareAsync` — praca w tle: dysk, obliczenia, zero dotykania drzewa
  wizualnego;
- `ApplyAsync(StartupUiContext, ...)` — wątek UI, nakłada efekt kroku na
  żywy interfejs.

Stan potrzebny między fazami krok trzyma we własnym prywatnym polu (np.
`WarmCampaignDataStep` pamięta identyfikator kampanii do rozgrzania wizualnej
strony); kontrakt nie ma współdzielonego miejsca na taki stan — to świadomie
zminimalizowane, każdy krok niesie tylko to, czego sam potrzebuje.
`StartupUiContext` niesie wyłącznie referencję do `WarmupHost`, żeby faza UI
nie musiała znać typu `AppShellView`.

Zgłoszenie w korzeniu kompozycji: `App.Initialize()` buduje jawną tablicę
`IStartupStep[]` i przekazuje ją do `AppShellViewModel` jako parametr
konstruktora — kolejność w tablicy jest kolejnością wykonania, więc zmiana
kolejności to widoczna w diffie edycja jednego miejsca, nie efekt uboczny
gdzieś indziej. Runner, `AppShellViewModel.RunStartupAsync(StartupUiContext)`,
woła `PrepareAsync`/`ApplyAsync` kroku po kroku i oddaje sterowanie
dispatcherowi między nimi; `AppShellView.OnLoaded` to jedno wywołanie tego
runnera, odpalane dopiero po pierwszym renderze lekkiej powłoki.

`WarmPanelVisualStep` jest ogólnym krokiem rozgrzewki jednego typu panelu;
korzeń kompozycji nie rejestruje dziś jego instancji. Wspólną mechanikę
attach → `Loaded` → drenaż dispatchera → detach dzieli z innymi krokami przez
`VisualWarmupHost`. Stół kampanii rozgrzewa `WarmCampaignWorkspaceVisualStep`,
bo jego panele wynikają z zapisanego układu użytkownika.

**Dług, świadomie zostawiony:** `CampaignLibraryViewModel` potrzebuje w
konstruktorze callbacku otwierającego kampanię, a callback ten prowadzi do
`AppShellViewModel.OpenCampaignAsync` — metody na powłoce, która w momencie
budowy biblioteki (`App.Initialize()`) jeszcze nie istnieje (powstaje dopiero
w `OnFrameworkInitializationCompleted`). Rozwiązane domknięciem nad polem
`_shell` (`id => _shell!.OpenCampaignAsync(id)`), rozstrzyganym dopiero przy
pierwszym kliknięciu, długo po tym jak powłoka na pewno już istnieje;
`OpenCampaignAsync` podniesione z `private` na `internal`, żeby korzeń
kompozycji mógł się do niej domknąć. Świadomy wybór — alternatywą był osobny
mechanizm późnego wiązania zbudowany dla jednego callbacku, uznany za
nieproporcjonalny do problemu.

## Dostęp do dysku

Granica jest o **rodzaj danych**, nie o użyte API:

- dane kampanii zapisuje wyłącznie `Core/Persistence`;
- `Core` poza `Persistence` nie dotyka dysku w ogóle;
- `Desktop` wolno zapisać własny stan lokalny — preferencje aplikacji i
  układ biurka — i nic ponad to.

Wcześniejsze brzmienie („`System.IO` nie wychodzi poza `Core/Persistence`")
było za szerokie i wzięte dosłownie kazałoby złamać szew mocniejszy: żeby
`Desktop` zapisywał układ biurka przez `Core`, rdzeń musiałby poznać
pojęcia interfejsu — szerokość panelu, pozycję na biurku, profil UI. Rdzeń
nie wie o warstwie UI i to jest ważniejsze.

**Egzekwowane:** nic. To konwencja — żaden test nie zatrzyma odczytu pliku
dopisanego w narzędziu ani w ViewModelu panelu.

**Stan faktyczny:** zgodny. W `Core` rzeczywisty odczyt i zapis plików
wykonuje wyłącznie `JsonCampaignRepository`. W `Desktop` pliki czytają i
zapisują tylko `Settings/AppSettingsStore` oraz
`Features/CampaignWorkspace/Layout/WorkspaceLayoutStore`, oba dla lokalnego
stanu interfejsu. `App` jedynie buduje ścieżki, a `CampaignSession` rozpoznaje
wyjątki dyskowe bez własnego dostępu do plików.

## Narzędzia i rejestr bloków danych

`ITool` wymaga tylko `IReadOnlyList<DataBlockId> Uses`. Narzędzie nie ma
cyklu życia, własnego stanu, katalogu ani zależności od innego narzędzia.
Może deklarować, których bloków używa; nie deklaruje kolejności aktywacji,
bo nic nie jest aktywowane. Nie deklaruje też, których definicji katalogu
paczek używa — katalog jest tylko do odczytu i globalny, nie coś, co
narzędzie posiada; `Uses` dotyczy wyłącznie bloków danych, bo tylko bloki
się zapisuje (patrz „Katalog treści systemowej" niżej).

Każdy blok danych jest opisany przez `DataBlockId`, wersję i `DataBlockShape`.
`DataBlockRegistry` odrzuca duplikat i pozwala odczytać opis tylko znanego
bloku. Rejestracje wbudowanych bloków i narzędzi są wpisane jawnie w
`App.Initialize()`; korzeń kompozycji sprawdza też, czy każdy blok z `Uses`
jest zarejestrowany. Nie ma skanowania dysku ani assembly i nie ma jeszcze
katalogu narzędzi — pojawi się dopiero, gdy drugie narzędzie go uzasadni.

**Egzekwowane:** kompilator wymaga `Uses` od implementacji `ITool`, a
`DataBlockRegistry` sprawdza duplikaty w runtime. Jawność rejestracji,
sprawdzenie wszystkich narzędzi w korzeniu kompozycji oraz brak katalogu są
konwencją.

## Katalog treści systemowej

Rdzeń rozróżnia **dwa różne mechanizmy kształtu, świadomie**. Blok danych —
kształt w `DataBlockRegistry`, sprawdzany w czasie działania przy zapisie
(patrz „Dane, zapis i nieczytelne bloki" niżej). Wpis katalogu paczek —
kształt wzięty wprost z typów C#, sprawdzany przez kompilator, nigdy przez
rejestr. Powód rozdziału: rdzeń zapisuje bloki danych, a katalogu nie
zapisuje nigdy — nie potrzebuje więc jego kształtu do niczego poza
odczytem, dla którego kompilator wystarcza.

Z tego rozdziału wynika granica **języka kształtu bloków**: rośnie on o
trzy rzeczy i nie więcej — zagnieżdżony rekord, listę, referencję. Warianty
oznaczone typem, mapy i niejednorodne zestawy pól nie wchodzą do tego
języka wcale. Cała ta złożoność (warianty klasy pancerza potwora, obrażenia
„albo kość, albo wybór", mapa poziomu czaru na formułę) leży po stronie
katalogu, a katalog kształtu z rejestru nie używa. To jest zapora przed
językiem kształtu pełzającym w stronę interpretera, przed czym ostrzega
`docs/vision.md`; wcześniejsze brzmienie tamtego dokumentu dopuszczało
warianty jako kolejny krok tego samego języka co zagnieżdżanie i
referencje — to było niedopowiedzenie, poprawione przy tej okazji.

Katalog to pliki JSON na dysku, bez bazy danych. Aplikacja nigdy nie
zapisuje do katalogu paczek; odczyt wykonuje jeden jawny czytnik w
`Core/Persistence`, zgodnie z istniejącym szwem opisanym w „Dostęp do
dysku" wyżej. Baza danych odpada świadomie: jeden użytkownik, jeden proces,
a kopia zapasowa ma pozostać skopiowanym katalogiem — ten sam model, co dla
kampanii (`docs/vision.md`), nie eksport z silnika bazy.

Wpis katalogu ma zamkniętą, krótką listę pól, które kod czyta i liczy —
dziś waga i cena; dla statbloków dojdą zasoby. Reszta wpisu to statystyki
(pary etykieta-wartość) oraz sekcje z nagłówkiem i tekstem, których kod nie
interpretuje, tylko pokazuje. Konsekwencja praktyczna: dodanie zaklęcia,
potwora czy akcji legendarnej to zero linijek kodu; zmiana tego, co jest
liczone, to zmiana kodu.

Nowa reguła odnawiania zasobu spoza zamkniętej listy (pełne, połowa, stała
wartość, z opcjonalnym minimum) jest zgłoszeniem, nie nowym polem w pliku
paczki. Nazwy rodzajów odpoczynku nie są w kodzie — deklaruje je paczka
systemowa.

## Dane, zapis i nieczytelne bloki

`CampaignDataBlocks` przechowuje wartości kampanii. Żyjąca kampania zmienia
blok wyłącznie przez `Apply(DataBlockId, Func<object?, object>)`: otrzymuje
ona wartość bieżącą (albo `null` dla bloku nigdy niezapisanego), sprawdza
wynik według zarejestrowanego kształtu, zamraża go i ogłasza
`DataBlockChanged`. `Hydrate` jest osobną fabryką odtworzenia kampanii z
dysku; nie publikuje zdarzeń i nie jest drogą zmiany już żyjącej kampanii.

Narzędzie zwraca przekształcenie, a nie gotową treść. `CounterTool` jest
pierwszym przykładem: deklaruje blok `counter`, zwraca przekształcenia
zwiększenia i zmniejszenia, a przepełnienie wykrywa `checked`. Rdzeń zna
kształt danych, lecz nie ich znaczenie; serializację JSON zna wyłącznie
warstwa `Core/Persistence`.

`JsonCampaignRepository` zapisuje manifest i wartości bloków jako jedną
generację. Nieczytelny blok — nieznany rejestrowi albo zapisany w
nieobsługiwanej wersji — jest oznaczany przy odczycie, pozostaje poza
wartościami roboczymi i nie może zostać zapisany przez `Apply`. Przy kolejnym
zapisie repozytorium zachowuje jego plik i wpis manifestu. Ścieżek migracji
jeszcze nie ma.

**Egzekwowane:** API `CampaignDataBlocks` nie wystawia settera ani indeksatora,
a testy pokrywają zgodność kształtu, publikację zdarzenia, normalizację i
ochronę bloku nieczytelnego. Granica JSON w `Core/Persistence`, jedyność
wejścia do zapisu oraz brak alternatywnej drogi z warstwy Desktop wynikają z
kształtu API i konwencji, nie z osobnego testu architektonicznego.

## Komunikacja i sesja kampanii

Narzędzia komunikują się pośrednio: wspólne bloki odpowiadają na pytanie
„jak jest”, a `CampaignEvents` na pytanie „co się stało”. Publikujący nie zna
odbiorców; zdarzenie niesie fakt, nie wartość bloku. `DataBlockChanged`
niesie tylko identyfikator, więc okno po jego otrzymaniu odczytuje dane
ponownie.

`CampaignEvents` działa synchronicznie dla jednej kampanii, dopasowuje
dokładny typ zdarzenia, zachowuje kolejność subskrypcji i ogranicza kaskadę.
`Subscribe` zwraca idempotentne `IDisposable`; odpięcie dotyczy dokładnie
tej referencyjnej rejestracji, którą zwróciło wywołanie. Publikacja iteruje
po migawce aktualnych subskrypcji, więc zmiany subskrypcji podczas obsługi
wpływają dopiero na następne zdarzenie.

`CampaignSession` w Desktop koordynuje operację na otwartej kampanii i jej
zapis do `ICampaignRepository`. Panel przekazuje mu operację zawierającą
`Apply`; po błędzie dysku zmiana pozostaje w pamięci, a sesja zwraca
komunikat. Brak bezpośredniego wywołania narzędzie → narzędzie oraz używanie
sesji do operacji paneli są konwencją; magistrala i bloki nie mogą jej same
wymusić.

## Bilans egzekwowania

| Szew | Stan |
|---|---|
| Granica Core / Desktop | test częściowy (brak referencji do Avalonii) |
| Dostęp do dysku | konwencja |
| Kontrakt narzędzia i rejestr bloków | kompilator dla `Uses`, runtime dla duplikatu bloku, reszta konwencja |
| Kształt wpisu katalogu treści systemowej | kompilator (typy C#), nigdy `DataBlockRegistry` |
| Katalog tylko do odczytu, `Uses` ograniczone do bloków danych | konwencja — jeden czytnik w `Core/Persistence`, zero pisarzy do katalogu |
| Komunikacja pośrednia | testy magistrali i bloków danych, brak krawędzi narzędzie → narzędzie to konwencja |
| Stan bloków i zapis | testy zachowania `Apply`, persystencji i bloków nieczytelnych; granice warstw to konwencja |

Większość szwów nadal wymaga przeglądu kodu: testy sprawdzają zachowanie, ale
nie zastępują jawnej kompozycji ani zależności pośrednich. `docs/code-map.md`
opisuje stan faktyczny, dlatego rozjazd z tym dokumentem pozostaje sygnałem
długu.
