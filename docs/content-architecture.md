# DungeonApp — architektura treści (projekt docelowy)

**Status: projekt, nie stan faktyczny.** [hld.md](hld.md) opisuje to, co jest w kodzie dzisiaj.
Ten dokument opisuje cel. Dopóki oba się rozjeżdżają, prawdą o kodzie jest `hld.md`.

Źródłem koncepcji jest [rpg_systems.md](rpg_systems.md) — dokument pisany w oderwaniu od tego
repozytorium. Ten dokument jest jego **osadzeniem w projekcie**: rozstrzyga nazwy, odrzuca to,
czego nie bierzemy, i nazywa granice wobec kodu, który już istnieje. Tam, gdzie `rpg_systems.md`
mówi coś o charakterze aplikacji sprzecznie z README lub sekcją 1 `hld.md` — wygrywa README.
Wszystkie świadome odchylenia od źródła są zebrane w sekcji 18.

---

## 1. Zakres — bez zmian

Definicja aplikacji i jej celu **nie zmienia się**. To desktopowy panel Mistrza Gry przy stole,
przy grze papierowej: otwierany wyłącznie przez DM-a, jedna maszyna, jeden użytkownik, bez sieci.
Pomaga śledzić stan świata i automatyzuje księgowość. Nie jest stołem wirtualnym, nie jest grą dla
graczy, nie jest generatorem treści.

Utrzymana zostaje zasada rozstrzygająca spory o zakres funkcji: **aplikacja prowadzi księgowość,
nie egzekwuje reguł.** Interpretacja zasad zostaje przy Mistrzu Gry. Sekcja 9 przekłada tę zasadę
na kryterium techniczne.

**Klasa systemów „trad"** — dyskretne nazwane statystyki, rozstrzyganie akcji rzutem
z modyfikatorem, jakaś forma kolejności działania — jest doprecyzowaniem tego samego zakresu,
a nie jego rozszerzeniem. Obejmuje m.in. D&D, Pathfindera, większość OSR, Call of Cthulhu,
Savage Worlds. Poza zakresem świadomie: gry bez kości, systemy oparte w rdzeniu na puli sukcesów,
gry karciane. To granica produktowa, nie techniczna.

---

## 2. Słownik

`rpg_systems.md` i dzisiejszy kod używają słowa **narzędzie** na dwie **różne** rzeczy. To nie jest
kolizja do rozstrzygnięcia usunięciem jednej z nich — obie są potrzebne i obie zostają.
Rozstrzygamy ją nazwą.

| Pojęcie | Znaczenie w tym projekcie |
|---|---|
| **wpis** | Zarejestrowana treść z paczki: żelazny miecz, goblin, zaklęcie, efekt. Esencja — czym rzecz jest. Niezmienna, tylko do odczytu, adresowana `paczka:id`. |
| **instancja** | Ten konkretny egzemplarz żyjący w kampanii: referencja do wpisu plus nakładka. |
| **nakładka** | To, czym instancja odbiega od wpisu. Rzadka — przechowuje wyłącznie odchylenia. |
| **karta** | Pełna reprezentacja wpisu lub instancji — widok szczegółowy. **Jedna z wielu reprezentacji, nie jedyna.** |
| **element karty** | Jednostka z zamkniętego katalogu silnika, z której składa się karta: statblok, lista pozycji, pasek zasobu, akcja rzutu, blok prozy, znacznik binarny. **To są „narzędzia" z `rpg_systems.md`.** |
| **kontrakt prezentacji** | Nazwany, zamknięty zestaw pól publikowany przez konsumenta, który pokazuje cudze wpisy w skrócie (wiersz ekwipunku, uczestnik kolejki tur, wkład do pola). Szablon deklaruje, czym go wypełnia. |
| **szablon** | Deklaracja z paczki systemowej: z jakich elementów składa się karta danego rodzaju wpisu, jakie ma sloty i które kontrakty wypełnia. |
| **slot** | Zadeklarowane przez szablon miejsce na dołączone wpisy (ekwipunek, zaklęcia na broni, efekty na postaci). |
| **narzędzie** (`ITool`) | Backend okna na biurku kampanii — istniejące pojęcie, **bez zmian**. Deklaruje, z jakich bloków danych korzysta. |
| **panel / okno** | Pływające okno na biurku. Kontener; hostuje narzędzie. |
| **blok danych** | Istniejący mechanizm stanu kampanii o kształcie znanym w czasie kompilacji. Substrat narzędzi biurka. |
| **paczka systemowa** | Warstwa 2: wyłącznie szablony. „D&D 5e" to dokładnie to i nic więcej. |
| **paczka treści** | Warstwa 3: wyłącznie wpisy. **Wszystko tutaj jest homebrew z definicji** — patrz sekcja 11. |
| **rejestr** | To, co silnik wie o zainstalowanych paczkach po ich wczytaniu i zwalidowaniu. Wspólny, tylko do odczytu. |

Dwa doprecyzowania nazewnicze wobec źródła:

* Element katalogu, który `rpg_systems.md` nazywa **„listą wpisów"**, nazywamy **„listą pozycji"** —
  „wpis" jest u nas zajęty przez pozycję rejestru.
* **Nie ma pojęcia „scena"** — patrz sekcja 3.

---

## 3. Kampania jest sesją, światem i zapisem gry

`rpg_systems.md` mówi o „sesji" i „scenie" i nie zna pojęcia kampanii. U nas kampania jest tym
wszystkim naraz: utworzenie kampanii **jest** utworzeniem sesji i utworzeniem nowego stanu świata.
Kampania jest save'em gry.

**Warstwa scen nie powstaje.** Nie ma bytu pośredniego między kampanią a instancjami. Kampania
zawiera swój świat wprost: instancje oraz stan narzędzi biurka. Wchodzi się do kampanii, w niej
aktualizuje się stan świata, i to jest cały model.

**Kampania jest właścicielem zbioru instancji.** Narzędzia biurka trzymają wyłącznie referencje do
nich. Cykl życia instancji — powołanie, usunięcie — jest sprawą świata, nie okna. Usunięcie
instancji zostawia w narzędziu referencję nierozwiązaną; ten sam wzorzec, co przy brakującej
paczce, więc żadnego nowego mechanizmu.

Konsekwencja dla katalogu: źródło dzieli narzędzia na „narzędzia bytu" i „narzędzia sceny",
umieszczając tracker tur w drugiej grupie. U nas ten podział znika, bo **tracker tur nie jest
elementem karty — jest narzędziem biurka** (`ITool`, własne okno, własny blok danych). Kolejka tur
dotyczy całej kampanii, a nie pojedynczego wpisu, więc nie może być sekcją czyjejś karty.

---

## 4. Trzy warstwy osadzone w tym repozytorium

`rpg_systems.md` opisuje warstwę 1 jako „kompilowaną (C# / Avalonia)". W tym projekcie ta warstwa
jest **rozdzielona, a granica wymuszona mechanicznie** przez `CoreIndependenceTests`. Nie
spłaszczamy jej.

| Warstwa źródła | Gdzie żyje u nas | Odpowiedzialność |
|---|---|---|
| **1. Silnik** | `DungeonApp.Core` | Katalog elementów karty, katalog kontraktów prezentacji, silnik formuł, wczytywanie i walidacja paczek, rejestr, magazyn instancji, istniejące narzędzia i bloki danych. Zero Avalonii. |
| | `DungeonApp.Desktop` | Widoki elementów karty, składanie karty z układu szablonu, widoki skrótowych reprezentacji, ekran rejestru, okna biurka. |
| **2. Szablony** | paczki systemowe | Kształt: z czego składa się karta, jakie są sloty, które kontrakty wpis wypełnia. |
| **3. Wpisy** | paczki treści + instancje w kampanii | Wartości i odchylenia — patrz sekcja 8. |

**Oba katalogi mieszkają w `Core`**, mimo że mają widoki. Katalog jest zobowiązaniem zakresowym
i podlega walidacji przy wczytaniu paczki, więc musi być znany warstwie, która paczki waliduje.
Widoki żyją osobno, w `Desktop` — to one są podmienialne, nie kontrakty.

**Silnik nie zna rodzajów wpisów.** Nie ma klasy `Monster`, `Spell`, ani enuma rodzaju wpisu, ani
dispatchu po rodzaju. Ścieżka renderowania karty jest identyczna dla każdego wpisu: wpis wskazuje
szablon, szablon wskazuje elementy. **Każde `switch` po rodzaju wpisu w `Core` jest naruszeniem
architektury** — kryterium tak samo weryfikowalne jak dzisiejszy zakaz `using Avalonia` w `Core`.

Zakaz dotyczy **rodzajów wpisów**, nie narzędzi biurka. Narzędzie jest kompilowaną funkcją
aplikacji i wolno mu być konkretnym; wpis nie.

**Jeden poziom szablonowania, nie dwa.** „Karta postaci" nie jest pojęciem silnika ani
szablonowanym kontenerem — jest szablonem o nazwie `postać gracza`, mieszkającym w paczce
systemowej i składanym z tego samego zamkniętego katalogu elementów, co karta goblina. Sześć cech
z D&D to statblok sparametryzowany sześcioma nazwami; silnik nie wie, że to cechy, ani że jest ich
sześć. Wrażenie, że taka karta „ma gotowe API do ekwipunku", opisuje dwie osobne rzeczy — katalog
elementów i kontrakt prezentacji, obie w sekcji 6 — a żadna z nich nie jest szablonem kontenera.

---

## 5. Decyzja centralna: zamknięte katalogi

Silnik **nie** udostępnia danym generycznych prymitywów UI (przycisk, pasek, etykieta). Udostępnia
skończone katalogi gotowych kształtów. Dane wybierają i wypełniają; nigdy nie decydują o kształcie
interfejsu. Udostępnianie klocków — choćby najprostszych — przenosi projektowanie interfejsu do
warstwy danych i w praktyce oznacza napisanie silnika UI cudzymi rękami.

Konsekwencje przyjęte świadomie:

* **Różnice między systemami wyrażamy parametrem istniejącego kształtu, nie nowym kształtem.**
* **Nowy rodzaj elementu lub kontraktu = nowe wydanie aplikacji.** Katalogi nie są rozszerzalne
  przez dane.
* **Reguła jednorodności.** Zmienna liczba **jednorodnych** elementów jest dozwolona (dowolnie
  długi statblok); zmienny skład **różnych typów** obok siebie — nie. To jedyne kryterium
  odróżniające konfigurowalny element od przebranego silnika UI.

„Zamknięty" znaczy **zamknięty wobec danych**, nie „kompletny na zawsze". Kolejne wydania
poszerzają katalogi; każdy nowy kształt przechodzi test z sekcji 17.

---

## 6. Reprezentacje wpisu i kontrakty prezentacji

**Wpis nie ma jednej reprezentacji.** Ten sam żelazny miecz pojawia się co najmniej w trzech:

* jako **karta** — pełny widok szczegółowy; to on jest widoczny w rejestrze po kliknięciu pozycji,
* jako **wiersz w ekwipunku bohatera** — nazwa, waga, skrót obrażeń i nic więcej, bo miejsca jest
  na jedną linię,
* dla potwora: jako **uczestnik kolejki tur** — nazwa i klucz sortowania.

**Kształt skrótowej reprezentacji definiuje konsument, nie szablon wpisu.** To zasada nośna tej
sekcji i bezpośrednie przedłużenie decyzji z sekcji 5: gdyby szablon mógł zaprojektować, jak
wygląda wiersz ekwipunku, projektowanie interfejsu wracałoby do warstwy danych tylnymi drzwiami.

Mechanizm: konsument — element karty albo narzędzie biurka — publikuje **kontrakt prezentacji**,
czyli nazwany, zamknięty zestaw pól, które umie pokazać. Sekcja ekwipunku wie, że wiersz ma nazwę,
wagę i skrót; tracker tur wie, że uczestnik ma nazwę i klucz sortowania. **Szablon deklaruje
wyłącznie, czym te pola wypełnia** — wartością, ścieżką w danych albo formułą. Nic poza tym.

Praktyczna konsekwencja, warta nazwania wprost: **projektowanie nowego narzędzia biurka nie wymaga
projektowania danych.** Narzędzie przychodzi z gotowym wyglądem i publikuje kontrakt; treść tylko
go wypełnia.

**Kwalifikacja jest własnością szablonu, wyliczaną przy wczytaniu paczek.** Konsument deklaruje,
które pola kontraktu są wymagane. Zbiór kandydatów trackera tur to zbiór szablonów wypełniających
jego wymagany kontrakt — policzony raz, na starcie. Przedmiotu nie da się zarejestrować w kolejce
tur nie dlatego, że narzędzie odrzuca go w momencie próby, tylko dlatego, że **nigdy nie pojawia
się na liście do wyboru**. Odrzucenie jest fizyczne, nie proceduralne.

Katalog kontraktów jest zamknięty wobec danych i wersjonowany razem z katalogiem elementów — to
jedna oś wersjonowania, nie dwie.

To jest uogólnienie **„zdolności"** ze źródła. Źródło definiuje je wąsko, jako „udział
w narzędziach sceny"; u nas scen nie ma, a mechanizm okazuje się potrzebny wszędzie tam, gdzie
jeden wpis pokazuje inny — również wewnątrz karty, w liście pozycji.

---

## 7. Katalog elementów karty — wersja 1

| Element | Odpowiedzialność |
|---|---|
| statblok | Lista cech; każda z opcjonalną wartością pochodną z formuły. |
| lista pozycji | Jednorodna lista; opcjonalna ilość na pozycję. Pozycje są **albo** dołączonymi wpisami (renderowanymi przez kontrakt prezentacji), **albo** czystym tekstem — lista deklaruje, które, i nie miesza. Pokrywa ekwipunek, tagi, cechy, amunicję, efekty, zaklęcia na broni. |
| pasek zasobu | Wartość bieżąca i maksymalna; maksimum stałe **albo** liczone formułą — nigdy oba naraz. |
| akcja rzutu | Etykieta plus dowolnie długa lista nazwanych formuł. Pokrywa pojedynczy rzut rozstrzygający i akcję wieloetapową (trafienie + obrażenia). Grupa („Akcje", „Legendarne", „Reakcje") jest **parametrem**, nie osobnym elementem-nagłówkiem. |
| blok prozy | Statyczny tekst z wpisu: opis, taktyka, treść zaklęcia. |
| znacznik binarny | Jednorodny zbiór przełączników o stałej liczbie: bennies, przygotowane zaklęcie, rzuty na śmierć. |

**Blok prozy i znacznik binarny są w v1 świadomie**, wbrew źródłu, które odkłada je poza pierwsze
wydanie — jednocześnie przyznając, że oba przechodzą jego własny test bez dyskusji. Bez nich
katalog nie renderuje statbloku żadnego systemu klasy „trad".

**Akcja skryptowa nie wchodzi** — wypada razem z poziomem 3, patrz sekcja 10.
**Tracker tur nie jest elementem karty** — jest narzędziem biurka, patrz sekcja 3.

---

## 8. Wpis, instancja, nakładka

Rozróżnienie jest tym samym, co **item** i **item stack**: wpis jest esencją, instancja jest
egzemplarzem istniejącym w świecie. **Dotyczy każdego wpisu warstwy trzeciej, nie tylko
przedmiotów** — potwór jest instancjonowany z dokładnie tego samego powodu, dla którego stos jest
instancją przedmiotu: jego obrażenia są własnością egzemplarza, nie gatunku. Trzy gobliny to trzy
instancje jednego wpisu.

> **Szablon deklaruje kształt. Wpis deklaruje wartości. Instancja deklaruje odchylenia.**

| Pytanie | Odpowiada | Przykład |
|---|---|---|
| Jaki *kształt* może mieć rzecz i jej nakładka? | szablon (warstwa 2) | „broń ma wagę, ilość i slot `zaklęcia`" |
| Jakie *wartości* ma konkretna rzecz? | wpis (warstwa 3) | „diamentowy miecz waży 3, stos maks. 1"; „goblin nosi tasak" |
| Co *odbiega* w tym egzemplarzu? | instancja (kampania) | „ten ma Ostrość V i nazywa się Zgubą" |

Waga i ilość rozchodzą się dokładnie po tej linii: **waga jest we wpisie** — każdy diamentowy
miecz waży tyle samo, to esencja; **ilość jest w nakładce** — to własność stosu, nie przedmiotu.
Oba są *zadeklarowane* przez szablon, ale mieszkają gdzie indziej.

**Rozstrzyganie wartości pola** ma trzy kroki, w tej kolejności: nakładka instancji → wartość
z wpisu → wartość pochodna z formuły szablonu. Instancja jest **rzadka**: przechowuje wyłącznie
odchylenia. Spełnia to zasadę, że kampania odnosi się do treści przez referencję i nigdy jej nie
duplikuje.

**Co wolno nakładce:** stan zmienny (ilość, zużycie, bieżąca wartość zasobu, przełączniki),
dołączone wpisy w zadeklarowanych slotach, tożsamość egzemplarza („Goblin 2"), ręczna korekta
pola.

**Czego nakładce nie wolno — reguła projektowa, nie blokada techniczna:** modelować **wariantu
rzeczy**. Miecz +1 to osobny wpis, nie miecz z nadpisanym polem. Różnica jest ostra i sprawdzalna:
korekta jest lokalna, nie da się jej użyć ponownie i nie ma jej w rejestrze; wariant jest treścią
i należy do paczki. Bez tego zakazu kampania po cichu staje się miejscem autorstwa treści, co łamie
zasadę, że kampania treści nie zawiera.

**Kryterium rozstrzygające brzmi więc: czy chcesz tego użyć ponownie.** Goblin łucznik, który ma
być pod ręką w każdej przyszłej potyczce i wyszukiwalny w rejestrze, jest wpisem. Ten jeden goblin,
któremu DM dał dziś procę, jest instancją. Kryterium jest operacyjne, a nie estetyczne — nie
wymaga rozstrzygania sporu o to, co „naprawdę" należy do esencji. Obie ścieżki istnieją równolegle
i nie wykluczają się: kilka wariantów w paczce daje szybki punkt startu, a ubieranie instancji
zostaje dostępne zawsze.

### Sloty i zagnieżdżanie

**Slot jest polem, nie pojemnikiem instancji.** Szablon deklaruje, że pole istnieje i co przyjmuje;
dalej obowiązuje dokładnie to samo rozstrzyganie, co dla każdego innego pola — nakładka → wpis →
formuła. Slot wyróżnia się wyłącznie tym, że jego wartością jest lista referencji, a nie liczba.
Nakładka nie jest miejscem, w którym sloty mieszkają: nakładka to zbiór odchyleń i może zawierać
odchylenie dowolnego pola, w tym slotu.

Zaklinanie rozkłada się przez to na trzy warstwy bez żadnego nowego mechanizmu: **warstwa 1**
publikuje kontrakt wkładu, **warstwa 2** deklaruje slot — czyli decyduje, że miecz da się zaklinać —
a **warstwa 3** dostarcza Ostrość V jako zwykły wpis z własną kartą. Slot to ta sama „lista
pozycji", co ekwipunek bohatera, o poziom niżej: miecz jest pojemnikiem na zaklęcia tak samo, jak
plecak jest pojemnikiem na miecze.

To, co wygląda na dwa różne zachowania, jest jednym: wpis „diamentowy miecz" zostawia slot zaklęć
**pusty**, więc Ostrość V pojawia się dopiero w instancji; wpis „goblin" ma slot ekwipunku
**wypełniony**, więc tasak jest już w deklaracji. **Różnica nie leży w mechanizmie, tylko w tym,
czy wpis wypełnił pole.** Wpis „Płonący Miecz" z zaklęciem ognia w slocie jest równie legalny;
parametru „slot tylko dla instancji" nie wprowadzamy, bo byłby wyjątkiem.

Zasady:

* **Silnik nie ma zdania o tym, co wpis powinien mieć w slocie, i mieć go nie może.** Wymuszanie
  „wpisy muszą być gołe" wymagałoby wiedzy, że istnieje stworzenie i istnieje wyposażenie, a jedno
  nie należy do drugiego — czyli wiedzy domenowej w warstwie pierwszej, zakazanej w sekcji 4. Co
  należy do esencji, decyduje autor paczki.
* **Zawartość początkowa slotu może wskazywać wyłącznie wpisy z tej samej paczki.** Inaczej wpis
  odwoływałby się do wpisu z innej paczki — krawędź 3 → 3, której zakazuje sekcja 11. Ograniczenie
  dotyczy tylko wartości domyślnych: instancja rozwiązuje referencje wobec wszystkich paczek
  kampanii, więc DM może ubrać goblina w cokolwiek. Paczka z potworami po prostu wozi ich
  uzbrojenie razem z nimi.
* **Gdy instancja zmieni zawartość slotu, przejmuje całą listę**, nie różnicę. Odczyt i zapis
  zostają trywialne, kosztem tego, że rozbrojony goblin nie zobaczy późniejszej aktualizacji
  paczki. To ta sama zasada, co przy każdym innym nadpisanym polu.
* **Pozycje slotu same są instancjami** — stąd zagnieżdżenie: kampania → bohater → stos → zaklęcie.
* **Trwałość idzie za własnością.** Instancja należąca do kampanii dostaje własny plik; instancja
  zagnieżdżona jest zapisywana wewnątrz właściciela. Nie ma osobnego rejestru stosów.
* **Cykl w grafie szablonów jest dozwolony** (torba w torbie), bo bez niego znika normalny
  przypadek użycia. Chroni nas twardy **limit głębokości zagnieżdżenia w danych** — ten sam rodzaj
  limitu, co głębokość wyrażenia i liczba kości.

**Referencja nierozwiązana.** Paczki może zabraknąć, może zniknąć w niej wpis, może zmienić się
niekompatybilnie. Wtedy referencja jest **nierozwiązana, a nie martwa**. Powielamy wzorzec, który
projekt już ma i ma przetestowany — `UnreadableDataBlock`: instancja zostaje, jest jawnie
oznaczona, jej nakładka **nigdy nie jest po cichu nadpisana ani skasowana**, a kampania otwiera się
dalej w trybie ograniczonym. Zainstalowanie brakującej paczki przywraca wszystko bez utraty stanu.

---

## 9. Granica automatyzacji

To najcieńsza linia w całym projekcie: między asystentem DM-a przy grze papierowej a silnikiem
cRPG. Pytanie „co jest policzalne, a co rozstrzyga DM" nie wystarcza, bo kruszy się przy pierwszym
efekcie modyfikującym klasę pancerza. Wystarcza dopiero pytanie **kto podjął decyzję**:

> **Aplikacja sumuje. DM decyduje o składnikach.**
> Aplikacja nigdy nie podejmuje decyzji, której DM nie podjął.

KP z pancerza: DM założył ten pancerz, pancerz sam o sobie mówi, ile daje, aplikacja dodała liczby.
Efekt „+2 do KP": DM dopisał go do listy efektów, aplikacja go zsumowała. Oba są księgowością —
**pod warunkiem**, że aplikacja nie wie, czym jest Tarcza Wiary, kiedy się zaczyna, kiedy wygasa,
czy się kumuluje i na kogo działa.

### Pięć zakazów

Wszystkie są sprawdzalne w przeglądzie kodu, bo są własnościami katalogu, nie intencji:

1. **Brak warunków** — formuła nie ma gałęzi. Efekt nie ma „jeśli", nie ma „gdy aktywny".
2. **Brak czasu** — aplikacja nie zna rund, czasu trwania ani wygasania. Efekt leży na liście,
   dopóki DM go nie zdejmie. „3 rundy" to tekst na karcie, nie mechanizm.
3. **Brak reguł kumulacji** — wszystko na liście się sumuje. Jeśli w systemie coś się nie kumuluje,
   DM tego nie dodaje.
4. **Brak celowania** — efekt jest tam, gdzie DM go położył. Aplikacja nigdy nie rozsyła efektów.
5. **Brak wyzwalaczy** — nic nie dzieje się samo w odpowiedzi na zmianę stanu.

Zabierz którykolwiek i zaczynasz pisać cRPG. Zostaw wszystkie pięć i nie da się go napisać, nawet
chcąc.

Te pięć pozycji to nie przypadkowa lista — **to kanoniczny zestaw funkcji silnika cRPG,
zanegowany.** Baldur's Gate czy Pathfinder adaptują ten sam system co my i muszą mieć warunki, czas
trwania, reguły kumulacji, celowanie i wyzwalacze, bo grają bez DM-a. Ten sam podręcznik, inny
wykonawca, inna architektura. Daje to praktyczny test: **jeśli zapragniemy którejkolwiek z tych
pięciu rzeczy, nie zbliżamy się do granicy — przekraczamy ją.**

Warto przy tym trzymać ostro, czego te zakazy *nie* mówią. Logika w aplikacji jest: formuły,
sumowanie wkładów, wartości pochodne. Niepotrzebne jest **wykonywanie reguł**. Księgowość też jest
logiką — tylko taką, która nie podejmuje decyzji.

### Efekt jest wkładem, nie mechanizmem

Efekt to zwykły wpis: ma szablon, kartę, może mieć prozę. Leży na instancji jako pozycja slotu —
dokładnie tak samo jak przedmiot w plecaku. Jego szablon wypełnia **kontrakt wkładu**: nazwa pola
plus wartość.

Stąd wniosek, który usuwa całą klasę projektowania: **pancerz i efekt są dla aplikacji tym
samym.** Oba to pozycje deklarujące wkład do pola `kp`. Nie ma „systemu efektów" — są wkłady.
Formuła KP brzmi „baza plus suma wkładów do `kp`" i nie wie, skąd te wkłady przyszły ani czym są.

### Test asystenta

Do zastosowania przy każdej przyszłej funkcji, obok testu na nowy kształt z sekcji 17:

1. Czy DM jawnie umieścił **każdy** składnik tego wyniku?
2. Czy wynik pozostanie niezmieniony, dopóki DM czegoś nie ruszy?
3. Czy aplikacja liczy, **nie wiedząc, czym** są składniki?

Trzy razy „tak" → księgowość, wolno automatyzować. Choć raz „nie" → aplikacja co najwyżej
proponuje, a nanosi DM.

### Wynik jest propozycją, nie zapisem

Dane kampanii są formularzem: DM dodaje, usuwa i poprawia ręcznie, a każde pole instancji jest
edytowalne, łącznie z wartością pochodną. Narzędzie liczy i **pokazuje** wynik; to DM decyduje, czy
i gdzie go nanieść. Bezpośredni zapis do pola w następstwie wykonanej operacji jest rzadkim
wyjątkiem, zadeklarowanym jawnie w kontrakcie akcji — i tam, gdzie występuje, **nie pyta
o potwierdzenie**: wywołanie akcji przez DM-a samo w sobie jest intencją.

Ta zasada usuwa z projektu kaskady automatycznych zmian stanu, dialogi potwierdzeń, cofanie zmian
wywołanych regułą i efekty obejmujące wiele instancji naraz jako wymaganie silnika.

---

## 10. Poziomy logiki i zasięg formuły

Źródło przewiduje trzy poziomy. Bierzemy dwa.

1. **Czyste dane** — pole wskazuje ścieżkę w danych. Zero logiki.
2. **Deklaratywna formuła** — jeden, współdzielony silnik stojący za każdym polem obliczanym
   w obu katalogach. Bez pętli, bez gałęzi, bez efektów ubocznych: bezpieczna z definicji, wymaga
   tylko walidacji przy wczytaniu paczki, nie piaskownicy. **Domyślna i jedyna ścieżka.**
3. ~~Skrypt (Lua)~~ — **wycofane.**

**Co usuwa wycofanie poziomu 3.** Nie tylko interpreter: znika piaskownica, biała lista API hosta,
limity czasu i liczby instrukcji, izolacja per paczka, przepływ jawnej zgody na instalację,
kontener błędów skryptu oraz element „akcja skryptowa". To jednocześnie **cała sekcja 6 dokumentu
źródłowego** — po tej decyzji model bezpieczeństwa redukuje się do sekcji 13.

**Zakaz gałęzi pełni drugą, ważniejszą rolę niż bezpieczeństwo: uniemożliwia warstwie drugiej
napisanie silnika reguł.** Autor paczki systemowej nie może zapisać „jeśli ciężki pancerz, to zeruj
zręczność", bo nie ma czym. Może napisać `min(zręczność, pancerz.limit)` — ale to arytmetyka nad
tym, co DM sam założył, a limit deklaruje ten konkretny pancerz o sobie samym. Aplikacja nadal nie
wie, co to „ciężki pancerz". `min` i `max` przenoszą trochę reguł, ale przenoszą je **do danych
konkretnego przedmiotu**, a nie do wiedzy aplikacji o kategoriach — i to jest różnica między tabelą
a silnikiem.

**Dwa konteksty ewaluacji** — rozróżnienie, którego źródło nie robi, a które jest konieczne:

* **Wartość pochodna** — deterministyczna funkcja danych, przeliczana przy każdym odczycie
  (modyfikator cechy, maksimum zasobu, KP, klucz sortowania w kolejce tur). **Nie może zawierać
  kości.**
* **Rzut** — wykonywany na żądanie; wynik jest zdarzeniem pokazywanym DM-owi, nie wartością
  odczytywalną z danych. Kości dozwolone.

Bez tego rozdziału wartość pochodna zmieniałaby się przy każdym renderze, a kolejka tur
przetasowywałaby się sama.

**Zasięg: jeden skok, nieprzechodni.** Formuła czyta własną instancję oraz pola tego, do czego ta
instancja odwołuje się bezpośrednio — pozycje slotów — i nie idzie dalej. Odczytane przez skok pole
nie może samo być wynikiem skoku. Pokrywa to KP z pancerza i efektów oraz obrażenia z broni, a
cykle stają się niemożliwe strukturalnie: bez wykrywania cykli, bez kolejności przeliczania, bez
inwalidacji zależności.

**Eksplodujące kości bez skryptu.** Były w źródle kanonicznym uzasadnieniem dla Lui. Traktujemy
eksplozję jako **własność notacji kości z twardym limitem eskalacji**, a nie jako pętlę pisaną
przez autora paczki. To usuwa ostatni realny argument za poziomem 3.

**Kryterium powrotu do dyskusji.** Jeśli okaże się, że poziom 2 nie pokrywa większości mechanik
klasy „trad", błędne jest założenie o klasie z sekcji 1 — wtedy wracamy do zakresu produktu,
a nie dopisujemy skryptów.

---

## 11. Paczki i zależności

**Paczka systemowa (warstwa 2)** wnosi wyłącznie szablony. „D&D 5e" to dokładnie to i nic więcej:
kształt karty postaci, kształt statbloku potwora, jakie są sloty, co się sumuje do czego.

**Paczka treści (warstwa 3)** wnosi wyłącznie wpisy — potwory, przedmioty, zaklęcia, efekty.
**Wszystko tutaj jest homebrew z definicji**, niezależnie od tego, czy stoi w oficjalnym
podręczniku. Aplikacja nie zna pojęcia autorytetu i nie musi go znać.

**Jedyną dozwoloną krawędzią zależności jest 3 → 2.** Paczka treści może zależeć od dowolnie wielu
paczek systemowych; paczka systemowa nie zależy od niczego, a treść nie zależy od treści. Dzięki
temu zależności są dwudzielne i nie tworzą grafu: nie ma problemu diamentu, zakresów wersji ani
kolejności wczytywania. „Wiele" przestaje być ryzykiem, bo głębokość jest zawsze jeden.

Każdy wpis wskazuje dokładnie jeden szablon. Paczka treści może zawierać wpisy dla różnych
systemów; te, których szablonu brakuje, po prostu się nie rozwiązują.

Ta sama zasada obowiązuje **wewnątrz** warstwy trzeciej: wpis może wskazywać inny wpis w zawartości
początkowej slotu (goblin ze swoim tasakiem), ale **wyłącznie z własnej paczki**. Gdyby mógł
sięgnąć do cudzej, powstałaby krawędź 3 → 3 i cała gwarancja płaskich zależności by upadła.
Instancji to nie dotyczy — ona rozwiązuje referencje wobec wszystkich paczek kampanii, bo jest
stanem świata, nie treścią.

**Kampania deklaruje jedną paczkę systemową i dowolnie wiele paczek treści.** Jedna kampania to
jedna gra.

### Szablony są płaskie — bez dziedziczenia

Szablon nie dziedziczy z innego szablonu. Nie ma hierarchii `diamentowy miecz → broń → narzędzie →
przedmiot`. Decyzja jest świadoma i opiera się na dwóch argumentach:

* **Skala jest inna niż tam, skąd ten wzorzec pochodzi.** Systemy, które dziedziczą deklaracje,
  robią to, bo mają tysiące definicji przedmiotów. U nas tysiące rzeczy to **wpisy**, a one już
  dzielą szablon — pięćset broni wskazuje jeden szablon `broń` i nic się nie powtarza. Dziedziczenie
  deduplikowałoby wyłącznie warstwę szablonów, których w paczce systemowej jest kilka.
* **Szablon nie ma zachowania, więc nie ma czego dziedziczyć.** Zachowanie mieszka w zamkniętym
  katalogu warstwy pierwszej. Szablon to lista pól, slotów i wypełnień kontraktów — dziedziczenie
  samych deklaracji, bez zachowania, jest skracaniem tekstu, a do tego nie potrzeba hierarchii
  typów.

Źródłem obu jest jedna różnica wobec systemów, z których ten wzorzec pochodzi: **tam zachowanie
wykonuje silnik, a przy stole wykonuje człowiek.** Program musi rozstrzygnąć każdy przypadek, więc
potrzebuje polimorfizmu; DM czytający podręcznik nie potrzebuje. Stąd bierze się reszta: w grze
komputerowej wiadro i miecz różnią się powierzchnią interakcji, a w tabeli broni miecz i topór
różnią się wyłącznie wartościami. **Tam są tysiące kształtów, tu jest jeden kształt i tysiąc
wierszy.** Systemy stołowe wyrażają zresztą różnice tagami, nie podtypami — bo tagi się składają,
a podtypy nie — i robią to od dekad, świadomie. Dodając dziedziczenie, modelowalibyśmy strukturę,
której w dziedzinie nie ma.

Koszt, którego w ten sposób unikamy, to nie parser, tylko reguły: kolejność rozstrzygania,
semantyka nadpisania kontra rozszerzenia, pytanie czy potomek może **usunąć** element rodzica
(jeśli tak, przestaje być prawdą, że układ karty da się przeczytać z jednego miejsca), problem
diamentu, oraz raportowanie błędów walidacji rozwiniętego szablonu w pliku źródłowym. Najdotkliwsze
jest wersjonowanie: dziś wersja szablonu wiąże wpis i tą ścieżką migruje, a przy dziedziczeniu
podbicie szablonu bazowego po cichu zmieniałoby efektywny kształt każdego potomka. Dziedziczenie
i tak byłoby ograniczone do jednej paczki, bo krawędź 2 → 2 jest zakazana.

**Gdyby powtórzenia stały się realne, sięgamy po kompozycję, nie dziedziczenie:** fragment jako
nazwany kawałek deklaracji, szablon jako lista fragmentów plus własne pola. Wyłącznie addytywnie,
wyłącznie w obrębie paczki, bez nadpisywania — zadeklarowanie tego samego pola dwa razy jest błędem
wczytania, nie cichym scaleniem. Zachowuje to jedną regułę rozstrzygania, brak diamentu, brak
odejmowania i lokalne wersjonowanie.

**Wyzwalacz do powrotu:** pierwsza paczka systemowa przekraczająca kilkanaście szablonów albo taka,
w której to samo wypełnienie kontraktu powtarza się w większości z nich. Wcześniej to spekulacja.

---

## 12. Rejestr i kampania — granica własności

Podział przebiega wzdłuż jednej linii: **co jest statyczne i wspólne, a co zmienne i własne.**
Pokrywa się to z podziałem, który projekt już stosuje — dokument użytkownika kontra stan aplikacji.

| Co | Gdzie | Charakter |
|---|---|---|
| Paczki systemowe i treści | `Documents\DungeonApp\Packs\<paczka>\` | Instalowane, tylko do odczytu, wspólne. Dokument użytkownika — ma być widoczny i kopiowalny. |
| Kampanie (instancje, stan narzędzi, manifest) | `Documents\DungeonApp\Campaigns\<id>\` | Istniejąca lokalizacja, bez zmian. |
| Układy biurka | `%LocalAppData%\DungeonApp\layouts\` | Istniejąca lokalizacja, bez zmian. Stan aplikacji, nie dokument. |

Manifest kampanii ma już zarezerwowane, dziś martwe pole `ContentPacks` — ożywiamy je jako listę
`{ id, major }`. Daje to istotną własność bezpieczeństwa: **zainstalowanie nowej paczki nie może po
cichu zmienić otwartej kampanii.** Kampania rozwiązuje referencje wyłącznie wobec paczek, które
sama deklaruje.

Drugiego zarezerwowanego pola, `Ruleset`, **nie ożywiamy** — `ContentPacks` wraz z deklaracją
paczki systemowej wystarcza. Nie wymyślamy dla niego znaczenia.

---

## 13. Model bezpieczeństwa

Bez poziomu 3 paczka to **czyste dane**. Nie ma czego uruchamiać, więc nie ma czego izolować. Cały
model bezpieczeństwa to **walidacja przy wczytaniu plus limity rozmiarowe**:

* Paczka z nieznanym elementem, nieznanym kontraktem, nieznanym parametrem albo błędną składniowo
  formułą jest **odrzucana w całości przy starcie** — nigdy nie ujawnia się przy pierwszym
  kliknięciu w środku sesji. To jedyny punkt z sekcji 7 źródła, który wymagał decyzji, i decyzja
  jest: odrzucamy natychmiast.
* Formuła jest walidowana nie tylko składniowo, ale i **wobec ścieżek deklarowanych przez
  szablon** — odwołanie do nieistniejącego pola jest błędem wczytania, nie błędem w trakcie gry.
* Niewypełnione wymagane pole kontraktu jest wykrywane przy wczytaniu, nie przy użyciu.
* Identyfikatory paczek i wpisów podlegają **ograniczeniom znakowym**, tak samo jak dzisiejszy
  `DataBlockId`, ograniczony właśnie dlatego, że staje się nazwą pliku. To zamyka przechodzenie po
  ścieżkach.
* **Limity:** rozmiar paczki, głębokość zagnieżdżenia wyrażenia, głębokość zagnieżdżenia instancji,
  liczba i rozmiar kości w jednym rzucie, limit eskalacji eksplozji. Bez pętli w języku to
  wystarcza, żeby czas ewaluacji był ograniczony z góry.
* Odrzucona paczka nie przewraca aplikacji — aplikacja startuje bez niej i mówi, której zabrakło
  i dlaczego.

---

## 14. Powłoka, nawigacja i okna

**System okien zostaje bez zmian wizualnych i bez zmian w geometrii** — `PanelGeometry`,
`WorkspaceSurface`, `WorkspaceLayoutStore` zostają w całości. Okno hostuje **narzędzie biurka**,
nie kartę.

**Karta nie jest oknem.** Jest reprezentacją: widokiem szczegółowym, który pojawia się w rejestrze
po kliknięciu pozycji i który narzędzie biurka może pokazać, gdy DM chce zobaczyć szczegóły
konkretnej instancji. Okno na biurku to narzędzie — na przykład lista kart bohaterów albo tracker
tur — a karta jest tym, co takie narzędzie wyświetla.

**Nawigacja ma dwa niezależne poziomy.** Globalna szyna boczna mówi, w której części aplikacji
jesteś. Przełącznik powierzchni mówi, na której powierzchni otwartej kampanii. Rozdzielenie jest
rozstrzygnięciem, a nie szczegółem układu — z niego wynika reszta tej sekcji.

**Globalna szyna ma trzy pozycje i to jest liczba docelowa**, nie stan przejściowy do zapełnienia.
Dziś realna jest jedna („Kampanie"), reszta renderuje placeholder, a `AppShellViewModel` nosi
komentarz o tymczasowym rusztowaniu.

* **Kampanie** — półka i wejście w kampanię. Bez zmian koncepcyjnych.
* **Rejestr** — przeglądanie zainstalowanej treści **poza kampanią**: lista wpisów z filtrem po
  paczce i szablonie, karta wybranego wpisu, lista zainstalowanych paczek. Tylko do odczytu.
* **Ustawienia.**

Sufit to pięć. Dojść mogą jeszcze **zarządzanie paczkami**, gdy zasłuży (patrz niżej), oraz
**autorstwo treści**, jeśli pytanie 3 z sekcji 19 rozstrzygnie się na korzyść pisania paczek
w aplikacji. Nic poza tym nie przechodzi kryterium: wszystko inne jest albo powierzchnią kampanii,
albo oknem biurka, albo rodzajem wpisu.

**Karta renderuje się tą samą ścieżką w rejestrze i w kampanii.** W rejestrze — z samego wpisu, bez
nakładki, bez edycji. W kampanii — ten sam skład elementów, zasilony instancją i edytowalny. To nie
jest oszczędność kodu, tylko własność: **jeśli szablon renderuje się w rejestrze, wyrenderuje się
przy stole.**

Zarządzanie paczkami (instalacja, usunięcie) jest widokiem podrzędnym rejestru. Osobną sekcję
najwyższego poziomu dostanie dopiero, gdy na nią zasłuży.

### Kontekstowy sidebar — odrzucony

Szyna, której **zawartość** zmienia się po wejściu w kampanię, jest odrzucona. Rozstrzygnięcie jest
starsze od tego dokumentu i zapisujemy je tutaj, bo jego brak spowodował, że wróciło w propozycji
raz jeszcze.

Trzy powody, każdy wystarczający osobno:

* **Pozycje pochodziłyby z danych.** Wczesny szkic wypełniał szynę nazwami w rodzaju „Bestiariusz",
  „Przedmioty", „Lokacje" — czyli rodzajami wpisów, których silnik z założenia nie zna (sekcja 4).
  Praktycznie: literówka w nazwie pola odrzuca paczkę, a wraz z nią **znikają pozycje nawigacji**.
  Nawigacja przestaje działać, bo autor pomylił klucz w pliku tekstowym. Do tego kampania deklaruje
  jedną paczkę systemową, więc zbiór szablonów różni się między kampaniami — globalna szyna
  o zawartości zależnej od tego, co masz otwarte, nie jest nawigacją, tylko widokiem, który ją udaje.
* **Szablon nie jest kategorią.** Szablon deklaruje kształt, nie rodzaj rzeczy. Jeden szablon
  `statblok` może obsługiwać potwory, NPC-e i zwierzęta naraz, a trzy szablony mogą opisywać to, co
  DM uważa za jedno. Grupowanie po szablonie jest taksonomią silnika, nie człowieka.
* **Nie miałaby własnej treści.** Wszystko, co należy do jednej kampanii, jest z definicji oknem
  biurka, bo biurko jest warsztatem kampanii, a okna są jego jednostkami. Szyna kampanii mogłaby
  więc być wyłącznie drugim sposobem otwierania okien.

Osobno: **rejestr nie jest miejscem wewnątrz kampanii.** Wpisy to referencje, kampania zawiera
instancje. Wybór wpisu z rejestru jest w kampanii potrzebny — sekcja 15(d) na nim stoi — ale jest
**momentem, nie miejscem**: przywoływanym z narzędzia, przelotnym, znikającym po wyborze. Błąd
wczesnego szkicu polegał na zamienieniu czynności chwilowej w stałe miejsce docelowe.

### Kampania jest zbiorem powierzchni

**Kampania nie jest tożsama z biurkiem.** Biurko jest jedną z jej powierzchni, nie jedyną.
Powierzchnie są **kompilowane i policzalne w czasie budowania** — nigdy wyprowadzane z paczek — i
jest ich kilka, nie kilkanaście. Przełącznik między nimi należy do kampanii, nie do globalnej szyny;
dzięki temu szyna pozostaje w pełni globalna, a poprzedni podrozdział nie zostaje unieważniony.

Powierzchnie, które przechodzą kryterium z następnego podrozdziału:

* **Biurko** — okna narzędzi. Stan świata oglądany kątem oka, w wielu rzeczach naraz.
* **Świat** — przegląd instancji tej kampanii, lista plus karta. Ten sam kształt co rejestr,
  pogrupowany wewnątrz danymi — czyli grupowanie z danych tam, gdzie jest legalne.
* **Fabuła** — dokumenty scenariusza, patrz niżej.
* **Kronika** — pełna historia zmian. Jednocześnie okno (ogon ostatnich zdarzeń) i powierzchnia
  (całość). To nie jest niespójność, tylko dwie długości tego samego.

### Okno czy powierzchnia — kryterium

Zasięg danych nie wystarcza do rozstrzygnięcia, bo biurko i powierzchnie kampanii mają ten sam
zasięg. Rozstrzyga **tryb obcowania**:

> Czy patrzy się na to kątem oka obok innych rzeczy, czy się w tym przebywa?

**Peryferyjne i równoczesne → okno biurka.** Kolejka tur, drużyna, zegar świata, kostki, notatka
sesyjna, ekwipunek. Patrzysz na nie *w trakcie* robienia czegoś innego.

**Centralne i wyłączne → powierzchnia.** Długi tekst, w którym się czyta i scrolluje. Maksymalizacja
okna daje rozmiar, ale nie daje wyłączności — nadal jest ramką z paskiem tytułu i resztą biurka pod
spodem.

Okna są legalne mimo swojej konkretności, bo stoi za nimi mechanizm z sekcji 6: narzędzie publikuje
**kontrakt prezentacji**, a szablon deklaruje, czym go wypełnia. Dzięki temu okno jest konkretne
i zaprojektowane, nie wiedząc, co pokazuje.

### Dokument przygody

Scenariusz — długi tekst z odhaczanymi krokami, wybranymi wariantami i wpisywanymi kwotami — jest
**wpisem, którego karta jest edytowalna nad instancją**. Nie wymaga ani jednego nowego mechanizmu:

* tekst scenariusza jest **treścią** (przygoda przyjeżdża jako paczka);
* odhaczenia i wpisane wartości są **nakładką instancji** w kampanii;
* aktualizacja przygody w obrębie tej samej wersji major nie kasuje odhaczeń — ta sama gwarancja, co
  przy każdym innym polu (sekcja 17).

Nowego elementu katalogu też nie wymaga: sekcja 9 mówi wprost, że **dane kampanii są formularzem**
i że każde pole instancji jest edytowalne. Karta w rejestrze jest tylko do odczytu, ta sama karta nad
instancją jest formularzem. Zmienna sekwencja różnych elementów nie łamie reguły jednorodności
z sekcji 5 — ta reguła dotyczy wnętrza **jednego** elementu, a karta jest sekwencją elementów
z założenia.

Autor pisze taki dokument w markdownie, w zwykłym edytorze, wplatając w tekst **markery** wybierające
pola z zamkniętego katalogu. Składnia wyboru nie ma znaczenia architektonicznego — markdown
z markerami to inna serializacja tego samego, co tablica w JSON-ie. Wiążą natomiast cztery warunki:

1. **Podzbiór markdowna jest zamknięty i nie zawiera układu.** Markdown opisuje strukturę
   *dokumentu*, nigdy strukturę *aplikacji*: nagłówki, akapity, listy, cytaty, wyróżnienia, tabele —
   tak; surowy HTML, szerokości, kolumny, kolory, wymiarowanie obrazków — nie. Passthrough HTML jest
   wyłączony, bo to jedna furtka, przez którą wchodzi wszystko naraz.
2. **Marker jest walidowalny przy wczytaniu.** Musi być jednoznacznie ogranicznikowany i nieść własne
   id. Dokument z markerem zniekształconym, z id powtórzonym albo z id, które nie jest poprawną nazwą
   pola, jest odrzucany jak każda inna wadliwa paczka (sekcja 13). Bez tego literówka w markerze nie
   jest błędem, tylko zwykłym tekstem — a to jest dokładnie ta klasa cichej awarii, której sekcja 13
   zakazuje.
3. **Markery znajduje się w drzewie, nie regeksem po pliku.** Wyrażenie regularne po surowym tekście
   trafi marker w bloku kodu, w linku i w komórce tabeli.
4. **Pola zadeklarowane przez dokument są prywatne dla dokumentu.** Dokument sam ogłasza swoje pola
   interaktywne, czyli warstwa 3 deklaruje kształt — wyłom w sekcji 8, dopuszczalny wyłącznie dlatego,
   że te pola nie mają konsumenta. Reguła warstw istnieje po to, żeby konsument mógł polegać na
   zadeklarowanym kształcie. W chwili, gdy formuła albo inne okno ma przeczytać „czy brama została
   otwarta", to pole musi być zadeklarowane w szablonie jak każde inne.

**Nierozstrzygnięte:** katalog pól formularza, kształt markera, dokładny podzbiór markdowna, oraz to,
czy dokument dostaje własny szablon, czy jest zwykłą kartą o długiej liście elementów. Zbiega się to
z pytaniem 3 z sekcji 19 w jedno pytanie — **jak człowiek pisze treść do tej aplikacji** — i przygoda
jest dla niego mocniejszym motywatorem niż ubrany goblin, bo jest treścią, którą **trzeba** napisać
długim tekstem.

---

## 15. Przepływy

**(a) Start aplikacji.** Nowy krok startowy wczytuje i waliduje paczki **przed** wczytaniem półki
kampanii, buduje rejestr i wylicza zbiory kandydatów dla kontraktów. Paczka odrzucona nie blokuje
startu.

**(b) Przeglądanie rejestru.** Sekcja `Rejestr` → lista wpisów → karta złożona z elementów według
układu szablonu, bez nakładki. Nie wymaga otwartej kampanii.

**(c) Otwarcie kampanii.** Manifest → paczka systemowa i paczki treści → rozwiązanie referencji
wobec rejestru (nierozwiązane oznaczone) → wczytanie instancji i stanu narzędzi → biurko
z zapisanego układu.

**(d) Wprowadzenie wpisu do świata.** Wybór wpisu z rejestru → kampania tworzy **instancję** (nowe
id, referencja, pusta nakładka, zawartość slotów z wpisu). Trzy gobliny to trzy wywołania tej
ścieżki.

**(e) Wpis widziany przez cudzego konsumenta.** Sekcja ekwipunku odczytuje **kontrakt prezentacji**
każdej pozycji i wypełnia nim wiersz o kształcie, który sama zdefiniowała. Tracker tur robi to samo
z kluczem sortowania. Element listy pozycji sumujący wkłady robi to samo z polem i wartością. Jeden
mechanizm, trzy zastosowania.

**(f) Zmiana stanu.** Edycja pola na karcie instancji albo naniesienie wyniku obliczonego przez
narzędzie → `CampaignSession.ExecuteAsync` → zapis nakładki → zdarzenie na `CampaignEvents` →
odświeżenie widoku. **To dokładnie ta sama ścieżka, którą dziś przechodzi licznik** — i po to
licznik istniał. Zmienia się magazyn docelowy, nie kształt przepływu.

---

## 16. Delta wobec dzisiejszego kodu

| Element | Co się z nim dzieje |
|---|---|
| `Core/Tools/ITool.cs` | **Zostaje.** Backend okien biurka; nie ma nic wspólnego z elementami karty. |
| `DataBlockRegistry`, `CampaignDataBlocks` | **Zostają** jako substrat narzędzi biurka. Nadal kompilacyjne. |
| `Core/Tools/Counter/`, `Desktop/.../Panels/CounterPanel*` | **Usunięte**, gdy powstanie pierwsze prawdziwe narzędzie biurka. `CounterPanelViewModelTests` zostają jako wzorzec kompletności. |
| `PanelCatalog` | Rośnie o prawdziwe narzędzia biurka w miejsce licznika. |
| `CampaignSession` | Bez zmian. Nadal jedyna droga zmiany otwartej kampanii. |
| `CampaignEvents` | Bez zmian — i rozstrzyga pytanie otwarte źródła, patrz niżej. |
| `JsonCampaignRepository` | Rozszerzone o magazyn instancji; te same prymitywy zapisu. |
| `CampaignManifest.ContentPacks` | **Ożywione.** `Ruleset` — nie. |
| `WorkspaceLayoutStore`, `PanelGeometry`, `WorkspaceSurface` | Bez zmian. |
| `CoreIndependenceTests` | Bez zmian; kandydat na bliźniaczy test zakazujący dispatchu po rodzaju wpisu w `Core`. |
| Sidebar | Sekcja `Rejestr` przestaje być placeholderem. |
| Nowe w `Core` | Wczytywanie i walidacja paczek, rejestr, katalog elementów karty, katalog kontraktów, silnik formuł, magazyn instancji. |

**Dwa magazyny stanu, każdy z własnym źródłem kształtu:**

| Magazyn | Co trzyma | Kto waliduje kształt |
|---|---|---|
| bloki danych (istniejący) | stan narzędzi biurka | `DataBlockShape`, znany w czasie kompilacji |
| magazyn instancji (nowy) | nakładki instancji, wraz z zagnieżdżonymi | szablon z paczki systemowej |

Magazyn instancji stoi na tych samych prymitywach zapisu co bloki danych — plik na instancję
kampanii, zapis przez plik tymczasowy plus atomowe przeniesienie, licznik generacji, wykrywanie
przerwanego zapisu. Dzięki rozdzieleniu walidacji `DataBlockRegistry` zostaje szczery: **„nieznany
blok" nadal znaczy „inny build aplikacji", a nie „brak zainstalowanej paczki".**

**Pytanie otwarte źródła nr 1 (kronika zdarzeń) jest już rozstrzygnięte przez kod.**
`CampaignEvents` istnieje: synchroniczna magistrala per kampania, z twardym limitem kaskady.
Odpowiedź: elementy karty nie wiedzą o dzienniku. Zmiana stanu emituje zdarzenie, a dziennik — gdy
powstanie — jest jego subskrybentem.

**Pytanie otwarte źródła nr 2 (kształt sceny) znika razem ze scenami** — patrz sekcja 3.

---

## 17. Wersjonowanie

Trzy niezależne osie, od najwolniej zmiennej:

1. **Wersja katalogu** — obejmuje elementy karty i kontrakty prezentacji łącznie; deklaruje ją
   szablon, a wpis dziedziczy ją przez szablon i nigdy nie deklaruje samodzielnie. W obrębie tej
   samej wersji major katalog rośnie **wyłącznie addytywnie** (nowe opcjonalne parametry i nowe
   opcjonalne pola kontraktów). Zmiana łamiąca wymaga bumpa, wpisu w changelogu i jawnej ścieżki
   migracji.
2. **Wersja szablonu** — wiąże się z nią wpis i tą ścieżką migruje.
3. **Wersja paczki** — kampania deklaruje `{ id, major }`. Zmiana minor rozwiązuje się normalnie;
   zmiana major zostawia referencję nierozwiązaną, dopóki kampania świadomie nie zaktualizuje
   deklaracji. **Nigdy cicha podmiana treści pod otwartą kampanią.**

Aktualizacja paczki w obrębie tej samej wersji major może zmienić wartości we wpisie. To jest
akceptowane: nakładka należy do kampanii i przeżywa, a Mistrz Gry widzi zmianę.

**Migracji nie budujemy teraz** — spójnie z decyzją, którą projekt już podjął dla bloków danych:
migracja ma sens dopiero, gdy jakaś wersja faktycznie została podniesiona.

**Test przed dodaniem nowego rodzaju elementu karty lub kontraktu** (nie parametru):

1. Czy da się to wyrazić parametryzacją kształtu, który już istnieje?
2. Czy zachowana jest reguła jednorodności z sekcji 5?
3. Czy **wbudowane zachowanie** — nie same parametry — miałoby sens bez modyfikacji w co najmniej
   2–3 różnych systemach klasy „trad"?

Odpowiedź „nie" na którekolwiek pytanie oznacza, że kształt jest przebraną funkcją jednego systemu.

---

## 18. Świadome odchylenia od `rpg_systems.md`

| Źródło mówi | My robimy | Dlaczego |
|---|---|---|
| „Narzędzia" na elementy składające widok bytu | „Elementy karty"; „narzędzie" zostaje przy istniejącym `ITool` | Dwa różne pojęcia, oba potrzebne. Nazwa rozstrzyga, nie usunięcie. |
| Warstwa 1 jako jedna, „C# / Avalonia" | Rozdzielona `Core` / `Desktop`, granica wymuszona testem | Granica już istnieje i jest egzekwowana mechanicznie. |
| Sesja i scena jako byty | Ani jedno, ani drugie — jest kampania | Kampania jest sesją, światem i zapisem gry. |
| Tracker tur jako „narzędzie sceny" | Narzędzie biurka z własnym oknem | Bez scen nie ma gdzie go umieścić, a kolejka tur dotyczy całej kampanii. |
| „Zdolności" wyłącznie jako interfejs bytu wobec sceny | Kontrakty prezentacji — mechanizm ogólny | Ten sam problem występuje wewnątrz karty (ekwipunek, wkłady), nie tylko na zewnątrz. |
| Byt ma jedną ścieżkę renderowania | Wpis ma wiele reprezentacji; skrótowe definiuje konsument | Wiersz ekwipunku nie mieści karty i nie może jej mieścić. |
| Poziom 3 (Lua) w wersji 1 | Wycofany | Jedyny wektor ryzyka w całym projekcie; poziom 2 pokrywa klasę „trad". |
| Blok prozy i znacznik binarny poza v1 | W v1 | Bez nich katalog nie renderuje statbloku żadnego systemu klasy „trad". |
| „Lista wpisów" jako nazwa elementu | „Lista pozycji" | „Wpis" jest zajęty przez pozycję rejestru. |
| Byt referencjonowany przez ID i nigdy nie duplikowany | Wpis + instancja z rzadką nakładką | Trzy gobliny mają trzy różne stany, a referencja i tak nie jest duplikowana. |
| Warstwy 2 i 3 nierozdzielone jako jednostki dystrybucji | Paczka systemowa i paczka treści, krawędź wyłącznie 3 → 2 | Homebrew musi być możliwe bez powielania szablonu; graf zależności — nie. |
| Efekty międzybytowe „świadomie odłożone" | Nie odłożone — wykluczone przez pięć zakazów z sekcji 9 | Nie jest to brak funkcji, tylko granica produktu. |
| Pytanie otwarte: kronika zdarzeń | Rozstrzygnięte przez istniejący `CampaignEvents` | Mechanizm już jest. |
| Pytanie otwarte: kształt sceny | Nieaktualne | Scen nie ma. |

---

## 19. Pytania otwarte

1. **Czy silnik formuł potrzebuje tablicy przeglądowej?** Modyfikator cechy to czysta arytmetyka,
   ale premia z biegłości i stopnie kości w Savage Worlds są tabelami. Tablica stała nie łamie
   zakazu gałęzi, ale poszerza język. Do rozstrzygnięcia przy pierwszym realnym szablonie.
2. **Jakie jeszcze pola trafiają do nakładki poza stanem i slotami.** Waga siedzi we wpisie, ilość
   w nakładce, dołączone wpisy w slotach — reszta wymaga konkretnych przykładów. Reguła
   rozstrzygająca jest już zapisana (szablon deklaruje kształt, wpis wartości, instancja
   odchylenia); brakuje przejścia przez realny system.
3. **Autorstwo treści w aplikacji.** Rejestr jest w v1 tylko do odczytu. Czy własną paczkę treści
   pisze się w edytorze tekstu, czy w aplikacji — nierozstrzygnięte, a ma duży wpływ na to, jak
   surowa może być walidacja przy wczytaniu. **Najmocniejszy motywator, jaki dotąd padł:** DM ubiera
   goblina i nie ma jak zapisać tego jako czegoś wielokrotnego użytku. Kuszące obejście — wpisy
   lokalne dla kampanii — jest **odrzucone**: brzmi tanio, a sprawia, że kampania zaczyna zawierać
   treść, czyli powoduje dokładnie tę erozję granicy, której pilnuje sekcja 8. Ta potrzeba ma być
   policzona jako argument za porządnym rozwiązaniem autorstwa, a nie przemycić je bocznymi
   drzwiami.
4. **Liczba przełączników w znaczniku binarnym** jest w v1 stała. Liczba przygotowanych zaklęć
   zależy od poziomu, więc prędzej czy później zechce być formułą. Odłożone.

**Świadomie nierozstrzygnięte na tym etapie:** konkretna implementacja poszczególnych narzędzi
biurka — w tym to, skąd tracker tur bierze inicjatywę i jak wygląda jego okno. Mechanizm jest
opisany w sekcji 6 (narzędzie publikuje kontrakt, szablon go wypełnia); wybór pól i zachowania
należy do projektu tego narzędzia, nie do architektury systemu treści.
