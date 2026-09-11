# DungeonApp — architektura

**Status: projekt docelowy.** Ten dokument rozstrzyga, **jak ma być**. Przy rozbieżności z kodem
prawdą o zamiarze jest ten dokument, a prawdą o kodzie jest [hld.md](hld.md). Nad jednym i drugim
stoi [CLAUDE.md](CLAUDE.md).

Kierunki, które rozważono i odrzucono, wraz z uzasadnieniami, są w [decisions.md](decisions.md).
**Czyta się je przed zaproponowaniem zmiany, nie po.**

Część projektu jest zbudowana — paczki i ich walidacja, rejestr, pierwszy przekrój pionowy warstwy
treści. Nie zmienia to statusu dokumentu.

---

## 1. Zakres

Desktopowy panel Mistrza Gry przy stole, przy grze papierowej: otwierany wyłącznie przez MG, jedna
maszyna, jeden użytkownik, bez sieci. Nie jest stołem wirtualnym, grą dla graczy ani generatorem
treści.

Klasa systemów **„trad"** — dyskretne nazwane statystyki, rozstrzyganie akcji rzutem z modyfikatorem,
jakaś forma kolejności działania — jest doprecyzowaniem zakresu, nie jego rozszerzeniem. Poza
zakresem świadomie: gry bez kości, systemy oparte w rdzeniu na puli sukcesów, gry karciane.
To granica produktowa, nie techniczna.

**Kampania jest sesją, światem i zapisem gry naraz.** Utworzenie kampanii jest utworzeniem stanu
świata. Nie ma bytu pośredniego między kampanią a instancjami — warstwa scen nie powstaje. Kampania
jest właścicielem zbioru instancji; narzędzia biurka trzymają wyłącznie referencje do nich. Cykl
życia instancji jest sprawą świata, nie okna: usunięcie instancji zostawia w narzędziu referencję
nierozwiązaną, tym samym wzorcem co przy brakującej paczce.

---

## 2. Słownik

| Pojęcie | Znaczenie |
|---|---|
| **zestaw** | Skompilowany projekt niosący typy treści, ich widoki i narzędzia biurka. Jedyne miejsce w aplikacji, w którym wolno wiedzieć, czym jest potwór. |
| **typ treści** | Para: rekord opisujący wartości + zaprojektowany widok karty. Adresowany `zestaw:id`. |
| **wpis** | Zarejestrowana treść z paczki: żelazny miecz, goblin, zaklęcie, efekt. Esencja — czym rzecz jest. Niezmienna, tylko do odczytu, adresowana `paczka:id`. |
| **dokument** | Drugi kształt treści: długi tekst z markerami deklarującymi pola interaktywne. Przygoda, scenariusz. Ma własny renderer, nie ma karty. |
| **instancja** | Egzemplarz żyjący w kampanii: referencja do wpisu lub dokumentu plus nakładka. |
| **nakładka** | Rzadka łatka nad wartościami wpisu — wyłącznie odchylenia. |
| **karta** | Zaprojektowany widok jednego typu treści. Nie jest składana z danych. |
| **kontrakt** | Interfejs publikowany przez konsumenta. Typ treści go implementuje albo nie. |
| **slot** | Pole, którego wartością jest lista referencji (we wpisie) lub lista instancji (w kampanii). |
| **narzędzie** (`ITool`) | Backend okna na biurku. Deklaruje, z jakich bloków danych korzysta. |
| **panel / okno** | Pływające okno na biurku. Kontener; hostuje narzędzie. |
| **blok danych** | Stan narzędzia biurka o kształcie znanym w czasie kompilacji. |
| **paczka** | Katalog z manifestem, niosący wpisy i dokumenty. Nie niesie typów treści. |
| **rejestr** | Co silnik wie o zainstalowanych paczkach po wczytaniu i zwalidowaniu. Wspólny, tylko do odczytu. |
| **powierzchnia** | Jeden ze skompilowanych widoków otwartej kampanii. |

---

## 3. Warstwy i granice

| Warstwa | Projekt | Charakter | Co wolno wiedzieć |
|---|---|---|---|
| **Silnik** | `DungeonApp.Core` | kompilowana | kampania, bloki danych, zdarzenia, zapis, wczytywanie paczek, rejestr, magazyn instancji, silnik formuł. **Zna `Entry`. Nie zna `Monster`.** |
| **Powłoka** | `DungeonApp.Desktop` | kompilowana | szyna, powierzchnie, framework okien, kontrolki wielokrotnego użytku. **Też nie zna `Monster`.** |
| **Zestaw** | `DungeonApp.Content.<x>` | kompilowana | typy treści, widoki kart, narzędzia biurka. **Jedyne miejsce, gdzie wolno być konkretnym.** |
| **Paczka** | `Dokumenty\DungeonApp\Packs\` | dane | wpisy i dokumenty. Zmienne, dodawane w trakcie sesji. |
| **Kampania** | `Dokumenty\DungeonApp\Campaigns\` | stan | instancje, nakładki, stan narzędzi. |

```
Content.<x>  ──►  Desktop  ──►  Core
paczka       ──►  Content.<x>          (wpis wskazuje typ treści)
kampania     ──►  paczki, które sama deklaruje
```

**Zestawy nigdy nie referencują się nawzajem.** Byłaby to krawędź wewnątrz jednej warstwy, z całym
bagażem, którego unikamy gdzie indziej: problem diamentu, kolejność wczytywania, wersjonowanie
kaskadowe.

**Dziś zestaw jest jeden.** Liczba mnoga jest zdolnością, nie planem. Drugi powstaje w dniu, w którym
naprawdę zmienia się system, i jest wtedy równoległy, nie zależny.

**Ładowanie zestawów jest statyczne — referencją projektu, nigdy `Assembly.LoadFrom`.** Ładowanie
wtyczek w czasie wykonania przywróciłoby cały model piaskownicy, który wycofanie skryptów usunęło
(sekcja 11), i kupiłoby zero: instalującym jest autor repozytorium.

---

## 4. Co wie silnik, a czego nie

**Cel:** aplikacja nie ma się stać aplikacją jednego systemu RPG. Wymiana systemu ma być napisaniem
czegoś obok, nigdy przepisaniem rdzenia.

**Mechanizm:** wiedza o tym, czym jest potwór, nie jest *zakazana* — jest *umiejscowiona*. Mieszka
w zestawie i nigdzie indziej.

Wcześniejsza wersja projektu zakazywała tej wiedzy wszędzie i egzekwowała to tym, że karta była
składana z danych. Cel był słuszny; mechanizm był jedyną znaną drogą do niego i przez to był z nim
mylony. Rozdzielenie jednego od drugiego jest najważniejszą zmianą w tym dokumencie i to ono
pozwala kartom być zaprojektowanymi.

Granica jest sprawdzalna mechanicznie, tak samo jak dzisiejszy zakaz Avalonii w `Core`:

* `Core` i `Desktop` nie referencują żadnego zestawu.
* Źródła `Core` i `Desktop` nie zawierają słownictwa treści — skan po słowniku wyprowadzonym
  z zestawu (id typów, nazwy właściwości, etykiety), więc zakaz poszerza się sam wraz z treścią.

Drugi test obejmuje **także `Desktop`**, co jest szersze niż dotychczas. Nie przypadkiem: największym
ryzykiem tej architektury jest, że wiedza o systemie wsiąknie w powłokę, a nie w rdzeń.

---

## 5. Deklaracja treści

### 5.1 Typ treści to para: rekord + widok

```
Monster            rekord: Size, Type, Alignment, Ac, AcSource?, Hp, …, Description
MonsterCardView    zaprojektowany XAML — układ statbloku
```

Rekord deklaruje pola: nazwy, typy, opcjonalność. Widok deklaruje układ. **Żadne z dwojga nie
pochodzi z danych.**

Konsekwencje przyjęte świadomie:

* **Nowy rodzaj treści = nowy kod.** Rekord i widok. Cena jest zamierzona: nowa deklaracja modelu
  i tak zwykle zbiega się z potrzebą nowej logiki albo nowego okna.
* **Aplikacja wie, jak wyświetlić potwora.** Statblok jest zaprojektowany, nie składany.
* **Literówka w nazwie pola jest błędem kompilacji**, nie odrzuconą paczką przy starcie. Kompilator
  jest walidatorem treści i jego komunikaty są interfejsem autora.
* **Liczba widoków rośnie liniowo** z liczbą rodzajów razy liczbę systemów — realistycznie 6–8 na
  system. To praca projektowa, przyjęta świadomie.

**Wyzwalacz do widoku domyślnego:** pierwszy rodzaj treści, którego nie chce się zaprojektować.
Wcześniej widok domyślny jest zaproszeniem, żeby przestać projektować karty, i nie powstaje.

### 5.2 Kontrolki, nie katalog

Lista cech, blok prozy, lista pozycji, pasek zasobu, akcja rzutu, znacznik binarny **zostają** —
ale jako **kontrolki wielokrotnego użytku**, komponowane przez projektanta karty, a nie jako pozycje
katalogu wybierane przez dane. `MonsterCardView` użyje listy cech dla sześciu atrybutów i bloku
prozy dla opisu; to projektant decyduje, gdzie one stoją.

Ginie wyłącznie **wybór kontrolki przez dane**, nie kontrolka.

Konsekwencją jest, że zniknęły parametry w rodzaju `compact` czy `selfDescribing`. Były
deklarowane jako „stwierdzenia o treści, nie o układzie", a istniały wyłącznie po to, żeby wymusić
konkretny układ w rendererze, który układu nie znał. Zaprojektowany widok nie potrzebuje ich mówić —
on je po prostu ma.

### 5.3 Wartości wpisu na dysku

```json
{ "id": "goblin", "name": "Goblin", "template": "dnd5e:monster", "values": { "ac": 15, … } }
```

`values` deserializuje się **wprost w rekord**, ze ścisłym traktowaniem nieznanych kluczy. Znika
własny walidator wartości wobec deklaracji pól — robi to deserializator.

**Format pliku jest odtąd wyborem serializatora, nie decyzją architektoniczną.** Podmiana dotyka
jednej klasy. Patrz sekcja 19, pytanie 1.

**Konwencja nazewnicza:** identyfikatory po angielsku, etykiety i treść po polsku. Dotyczy
identyfikatorów wymyślanych przez autora treści, nie kluczy samego formatu. Ta sama linia obowiązuje
w kodzie: `DataBlockId` ma wartość `counter`, a komunikaty dla Mistrza Gry są po polsku.

---

## 6. Wpis, dokument, instancja, nakładka

> **Typ treści deklaruje kształt. Wpis deklaruje wartości. Instancja deklaruje odchylenia.**

Rozróżnienie jest tym samym, co **item** i **item stack**: wpis jest esencją, instancja egzemplarzem
istniejącym w świecie. Dotyczy każdego wpisu, nie tylko przedmiotów — potwór jest instancjonowany
z tego samego powodu, dla którego stos jest instancją przedmiotu: jego obrażenia są własnością
egzemplarza, nie gatunku. Trzy gobliny to trzy instancje jednego wpisu.

| Pytanie | Odpowiada | Przykład |
|---|---|---|
| Jaki *kształt* może mieć rzecz? | typ treści | „broń ma wagę, ilość i slot `zaklęcia`" |
| Jakie *wartości* ma konkretna rzecz? | wpis | „diamentowy miecz waży 3"; „goblin nosi tasak" |
| Co *odbiega* w tym egzemplarzu? | instancja | „ten ma Ostrość V i nazywa się Zgubą" |

Waga i ilość rozchodzą się dokładnie po tej linii: **waga jest we wpisie** (każdy diamentowy miecz
waży tyle samo — to esencja), **ilość jest w nakładce** (to własność stosu, nie przedmiotu).

**Czego nakładce nie wolno — reguła projektowa, nie blokada techniczna:** modelować *wariantu
rzeczy*. Miecz +1 to osobny wpis, nie miecz z nadpisanym polem. Różnica jest sprawdzalna: korekta
jest lokalna, nie da się jej użyć ponownie i nie ma jej w rejestrze; wariant jest treścią i należy
do paczki. Kryterium operacyjne brzmi: **czy chcesz tego użyć ponownie.** Goblin łucznik, który ma
być pod ręką w każdej przyszłej potyczce, jest wpisem. Ten jeden goblin, któremu MG dał dziś procę,
jest instancją.

### 6.1 Nakładka jest rzadką łatką nad wartościami wpisu

**Instancja nie materializuje wpisu.** Aktualizacja paczki działa jak patchnote balansujący grę,
a nie jak zdarzenie psujące istniejące kampanie.

* **Postać kanoniczna** to obiekt `values` wpisu.
* **Nakładka** to rzadki obiekt zawierający wyłącznie klucze, które MG zmienił.
* **Odczyt:** `values` wpisu, łatka na wierzchu, deserializacja scalenia w rekord. Dostęp jest
  w pełni typowany.
* **Zapis:** widok produkuje nowy rekord przez `with`, magazyn różnicuje go wobec wpisu i zapisuje
  samą różnicę.

Nazwa pola nie pojawia się w kodzie ani przy odczycie, ani przy zapisie. Dodanie pola do rekordu
nie wymaga zmiany nigdzie indziej.

Dwie konsekwencje przyjęte świadomie:

* **Wartość może zmienić się pod Mistrzem Gry** przy aktualizacji paczki w obrębie wersji major.
  To jest cena propagacji i jest tym, czego chcemy; wersja major chroni przed dużymi przypadkami.
* **Klucz łatki, który zniknął z rekordu**, oznacza instancję jako wymagającą uwagi — nigdy nie
  odrzuca kampanii i nigdy nie kasuje łatki po cichu.

### 6.2 Sloty i zagnieżdżanie

**Slot jest polem, nie pojemnikiem instancji.** Typ treści deklaruje, że pole istnieje i co
przyjmuje; slot wyróżnia się wyłącznie tym, że jego wartością jest lista referencji, a nie liczba.

We wpisie wartością slotu jest lista **referencji**; w kampanii lista **instancji**. To różne typy,
więc slot nie przechodzi przez scalanie z 6.1 — ma własną ścieżkę. Nie jest to wyjątek, tylko drugi
rodzaj pola.

Zaklinanie rozkłada się przez to na trzy warstwy bez nowego mechanizmu: silnik publikuje kontrakt
wkładu, typ treści deklaruje slot (czyli decyduje, że miecz da się zaklinać), a paczka dostarcza
Ostrość V jako zwykły wpis z własną kartą. Slot to ta sama lista pozycji, co ekwipunek bohatera,
o poziom niżej: miecz jest pojemnikiem na zaklęcia tak samo, jak plecak na miecze.

Zasady:

* **Silnik nie ma zdania o tym, co wpis powinien mieć w slocie.** Wymuszanie „wpisy muszą być gołe"
  wymagałoby wiedzy, że istnieje stworzenie i istnieje wyposażenie — czyli wiedzy domenowej
  w warstwie, której to zakazane. Co należy do esencji, decyduje autor treści.
* **Zawartość początkowa slotu może wskazywać wyłącznie wpisy z tej samej paczki.** Instancji to nie
  dotyczy — ona rozwiązuje referencje wobec wszystkich paczek kampanii, bo jest stanem świata, nie
  treścią.
* **Gdy instancja zmieni zawartość slotu, przejmuje całą listę**, nie różnicę.
* **Pozycje slotu same są instancjami** — stąd zagnieżdżenie: kampania → bohater → stos → zaklęcie.
* **Trwałość idzie za własnością.** Instancja należąca do kampanii dostaje własny plik; instancja
  zagnieżdżona jest zapisywana wewnątrz właściciela.
* **Cykl w grafie jest dozwolony** (torba w torbie); chroni nas twardy limit głębokości zagnieżdżenia
  w danych.

**Referencja nierozwiązana.** Paczki może zabraknąć, może zniknąć w niej wpis, może zmienić się
niekompatybilnie. Wtedy referencja jest **nierozwiązana, a nie martwa**: instancja zostaje, jest
jawnie oznaczona, jej nakładka **nigdy nie jest po cichu nadpisana ani skasowana**, a kampania
otwiera się dalej w trybie ograniczonym. Zainstalowanie brakującej paczki przywraca wszystko bez
utraty stanu.

### 6.3 Dokument

Dokument jest drugim kształtem treści, ale **nie drugim magazynem instancji**.

| | wpis | dokument |
|---|---|---|
| co jest w paczce | wartości | tekst z markerami |
| skąd przestrzeń kluczy | rekord (kompilacja) | markery (wczytanie) |
| jak się renderuje | zaprojektowana karta | renderer dokumentu |
| instancja | rzadka łatka | **rzadka łatka, identyczna w kształcie** |

Wiele grup grających tę samą przygodę to wiele instancji jednego dokumentu, tak samo jak trzy
gobliny. Poprawka literówki w tekście nie kasuje odhaczeń, bo klucz nieobecny w łatce bierze się
z aktualnej treści.

Autor pisze dokument w markdownie, wplatając w tekst markery wybierające pola. Wiążą cztery warunki:

1. **Podzbiór markdowna jest zamknięty i nie zawiera układu.** Markdown opisuje strukturę
   *dokumentu*, nigdy strukturę *aplikacji*: nagłówki, akapity, listy, cytaty, wyróżnienia, tabele —
   tak; surowy HTML, szerokości, kolumny, kolory, wymiarowanie obrazków — nie. Passthrough HTML jest
   wyłączony, bo to jedna furtka, przez którą wchodzi wszystko naraz.
2. **Marker jest walidowalny przy wczytaniu** — jednoznacznie ogranicznikowany, z własnym id.
   Dokument z markerem zniekształconym albo z id powtórzonym jest odrzucany jak każda inna wadliwa
   treść. Bez tego literówka w markerze nie jest błędem, tylko zwykłym tekstem.
3. **Markery znajduje się w drzewie, nie regeksem po pliku** — wyrażenie regularne trafi marker
   w bloku kodu, w linku i w komórce tabeli.
4. **Pola zadeklarowane przez dokument są prywatne dla dokumentu.** W chwili, gdy formuła albo inne
   okno ma przeczytać „czy brama została otwarta", to pole musi być zadeklarowane w typie treści jak
   każde inne.

Dokument nie ma karty — ma własny renderer. To jest różnica, która trzyma niezmiennik z sekcji 10
w mocy: **karta jest układem, dokument jest sekwencją.**

---

## 7. Kontrakty są interfejsami

**Wpis nie ma jednej reprezentacji.** Ten sam żelazny miecz pojawia się co najmniej w trzech: jako
karta, jako wiersz w ekwipunku bohatera (nazwa, waga, skrót obrażeń — bo miejsca jest na jedną
linię), jako uczestnik kolejki tur.

**Kształt skrótowej reprezentacji definiuje konsument, nie treść.** Konsument — narzędzie biurka
albo kontrolka — publikuje interfejs. Typ treści go implementuje albo nie.

```
ITurnParticipant { Name, SortKey }
Monster : ITurnParticipant
Gear                            ← nie implementuje
```

Co z tego wynika:

* **Kwalifikacja jest systemem typów.** Przedmiotu nie da się zarejestrować w kolejce tur nie
  dlatego, że narzędzie odrzuca go przy próbie, tylko dlatego, że **nigdy nie pojawia się na liście
  do wyboru**. Odrzucenie jest fizyczne, nie proceduralne — i nic go nie liczy, bo robi to kompilator.
* **Pola kontraktu są typowane per pole.** `SortKey` jest liczbą, bo tak jest zadeklarowany. Znika
  ryzyko, że kontrakt zamieni się w worek napisów wymagających parsowania u konsumenta.
* **Znika potrzeba rozgałęziania po systemie.** Narzędzie neutralne zależy od interfejsu;
  `Dnd5e.Monster` i `Pf2e.Creature` oba go implementują. Narzędzie nie wie, że systemy istnieją.
* **Projektowanie nowego narzędzia nie wymaga projektowania danych.** Narzędzie przychodzi z gotowym
  wyglądem i publikuje interfejs; treść tylko go implementuje.

**Reguła umiejscowienia narzędzia:**

> Narzędzie czytające pole po nazwie mieszka w zestawie, który to pole deklaruje.
> Narzędzie czytające interfejs jest neutralne i mieszka w zestawie neutralnym.

Wybór między jednym a drugim jest wyborem katalogu, w którym leży plik — nigdy gałęzią w kodzie.

**Zakaz zabezpieczający:**

> **Narzędzie nigdy nie introspekcjonuje typu treści.** Deklaruje interfejs i dostaje obiekty.

Skompilowany typ leży w tym samym procesie, więc „znajdź wszystkie typy mające pole `initiative`"
jest jedną linijką. Gdy typy były plikami danych, ta pokusa praktycznie nie istniała. Zakaz musi
więc być zapisany, a nie dorozumiany.

---

## 8. Paczki, wczytywanie, bezpieczeństwo

Paczka to katalog z manifestem, niosący wpisy i dokumenty. Nie niesie typów treści.

Co paczka kupuje: przestrzeń nazw dla id, jednostkę instalacji i deinstalacji, wersję, którą kampania
deklaruje. Ostatnie daje istotną własność: **zainstalowanie nowej paczki nie może po cichu zmienić
otwartej kampanii**, bo kampania rozwiązuje referencje wyłącznie wobec paczek, które sama deklaruje.

**Wszystko, co paczka wnosi, jest homebrew z definicji**, niezależnie od tego, czy stoi
w oficjalnym podręczniku. Aplikacja nie zna pojęcia autorytetu i nie musi go znać.

**Jedna przestrzeń nazw na paczkę.** Id wpisu i id dokumentu nie mogą kolidować w obrębie jednej
paczki — kolizja czyniłaby adres niejednoznacznym, a nie tylko powtórzonym.

### Trzy zakresy odrzucenia

| Co jest wadliwe | Skutek |
|---|---|
| `pack.json` | **paczka odrzucona** — tożsamość nieznana, nie ma czym adresować zawartości |
| plik wpisu lub dokumentu | **ta pozycja oznaczona**, paczka wczytana |
| dwie pozycje o tym samym id w paczce | **obie oznaczone** — żadna nie wygrywa po cichu |

Odrzucanie **całej** paczki za jeden wadliwy wpis obowiązywało wcześniej i odpada razem
z uzasadnieniem, które je trzymało: chodziło o to, żeby rejestr nie zawierał wpisu rozwiązanego
wobec szablonu z nieobecnej paczki. Typy treści nie pochodzą już z paczek, więc wadliwy plik nie
zagraża żadnemu innemu. Zostawało już tylko ukrywanie dwustu poprawnych potworów za jedną literówką —
wprost przeciw zasadzie, że **wadliwa treść zostaje widoczna i oznaczona, nigdy nie znika po cichu.**

### Model bezpieczeństwa

Paczka to **czyste dane**. Nie ma czego uruchamiać, więc nie ma czego izolować. Cały model
bezpieczeństwa to walidacja przy wczytaniu plus limity rozmiarowe:

* Wadliwa treść jest wykrywana **przy starcie**, nigdy przy pierwszym kliknięciu w środku sesji.
* Formuła jest walidowana nie tylko składniowo, ale i wobec pól, które realnie istnieją.
* Identyfikatory podlegają ograniczeniom znakowym. Żadna ścieżka na dysku nie jest budowana z danych —
  tożsamość paczki mieszka w jej manifeście, nie w nazwie katalogu — więc ograniczenie chroni schemat
  adresowania, a nie system plików.
* **Limity:** rozmiar paczki, głębokość zagnieżdżenia wyrażenia, głębokość zagnieżdżenia instancji,
  liczba i rozmiar kości w jednym rzucie, limit eskalacji eksplozji. Bez pętli w języku to wystarcza,
  żeby czas ewaluacji był ograniczony z góry.
* Odrzucona treść nie przewraca aplikacji — startuje bez niej i mówi, czego zabrakło i dlaczego.

---

## 9. Granica automatyzacji

To najcieńsza linia w projekcie: między asystentem MG przy grze papierowej a silnikiem cRPG. Pytanie
„co jest policzalne" nie wystarcza, bo kruszy się przy pierwszym efekcie modyfikującym klasę pancerza.
Wystarcza dopiero pytanie **kto podjął decyzję**:

> **Aplikacja sumuje. MG decyduje o składnikach.**
> Aplikacja nigdy nie podejmuje decyzji, której MG nie podjął.

KP z pancerza: MG założył ten pancerz, pancerz sam o sobie mówi, ile daje, aplikacja dodała liczby.
Efekt „+2 do KP": MG dopisał go do listy, aplikacja go zsumowała. Oba są księgowością — **pod
warunkiem**, że aplikacja nie wie, czym jest Tarcza Wiary, kiedy się zaczyna, kiedy wygasa, czy się
kumuluje i na kogo działa.

### Pięć zakazów

Kanoniczne brzmienie jest w [CLAUDE.md](CLAUDE.md). W skrócie: brak warunków, brak czasu, brak reguł
kumulacji, brak celowania, brak wyzwalaczy.

To nie jest przypadkowa lista — **to kanoniczny zestaw funkcji silnika cRPG, zanegowany.** Baldur's
Gate adaptuje ten sam system co my i musi mieć wszystkie pięć, bo gra bez MG. Ten sam podręcznik, inny
wykonawca, inna architektura. Daje to praktyczny test: **jeśli zapragniemy którejkolwiek z tych pięciu
rzeczy, nie zbliżamy się do granicy — przekraczamy ją.**

Zakazy nie mówią, że logiki nie ma. Formuły, sumowanie wkładów i wartości pochodne są logiką.
Niepotrzebne jest **wykonywanie reguł**.

### Dwie granice do narysowania, zanim ktoś je przekroczy

Obie dotyczą funkcji, które są w planie, i obie padłyby niezauważenie.

**Runda i zegar świata.** Tracker tur i zegar świata trzymają liczby wyglądające jak czas.

> Narzędziu wolno trzymać numer rundy i czas świata. **Nic poza widokiem nie ma prawa ich
> odczytać.** Licznik jest wyświetlany i przesuwany przez MG; nie jest wejściem żadnej formuły ani
> warunkiem żadnego wygaśnięcia.

**Kronika.** Dziennik jako subskrybent zdarzeń byłby dosłowną sprzecznością z zakazem piątym.

> Wpis do kroniki jest **częścią operacji**, nie reakcją na nią. `CampaignSession.ExecuteAsync`
> zapisuje stan i wpis kroniki w jednym zatwierdzeniu.

Przyczynowość brzmi wtedy „jedna operacja zapisuje dwie rzeczy", a nie „zapis wywołał zapis".

**Przy okazji:** `MaxEventsPerCommand` jest dziś opisany jako zabezpieczenie przed pętlą. Skoro nic
nie ma prawa pisać w reakcji na zdarzenie, kaskada jest niemożliwa — więc ten limit jest **jedynym
mechanicznym egzekwowaniem zakazu piątego, jakie istnieje**. Zostaje; jego uzasadnienie wymaga
przepisania.

### Efekt jest wkładem, nie mechanizmem

Efekt to zwykły wpis: ma typ treści, kartę, może mieć prozę. Leży na instancji jako pozycja slotu —
tak samo jak przedmiot w plecaku. Implementuje **kontrakt wkładu**: nazwa pola plus wartość.

Stąd wniosek usuwający całą klasę projektowania: **pancerz i efekt są dla aplikacji tym samym.** Oba
deklarują wkład do pola `kp`. Nie ma „systemu efektów" — są wkłady. Formuła KP brzmi „baza plus suma
wkładów do `kp`" i nie wie, skąd te wkłady przyszły ani czym są.

### Test asystenta

Do zastosowania przy każdej przyszłej funkcji:

1. Czy MG jawnie umieścił **każdy** składnik tego wyniku?
2. Czy wynik pozostanie niezmieniony, dopóki MG czegoś nie ruszy?
3. Czy aplikacja liczy, **nie wiedząc, czym** są składniki?

Trzy razy „tak" → księgowość, wolno automatyzować. Choć raz „nie" → aplikacja co najwyżej proponuje,
a nanosi MG.

### Wynik jest propozycją, nie zapisem

Dane kampanii są formularzem: MG dodaje, usuwa i poprawia ręcznie, a każde pole instancji jest
edytowalne, łącznie z wartością pochodną. Narzędzie liczy i **pokazuje** wynik; to MG decyduje, czy
i gdzie go nanieść. Bezpośredni zapis w następstwie wykonanej operacji jest rzadkim wyjątkiem,
zadeklarowanym jawnie — i tam, gdzie występuje, **nie pyta o potwierdzenie**: wywołanie akcji przez
MG samo w sobie jest intencją.

Zasada ta usuwa z projektu kaskady automatycznych zmian stanu, dialogi potwierdzeń i cofanie zmian
wywołanych regułą.

---

## 10. Niezmiennik interfejsu

> **Widok zna pełny kształt tego, co wyświetla — w czasie kompilacji. Z danych przychodzą wyłącznie
> wartości.**

Żaden widok nie odkrywa swojego kształtu w czasie wykonania. Widok, który czyta skądkolwiek, jakie
ma pola, jest naruszeniem — i jest to sprawdzalne z samego diffu.

Jedyne miejsce, w którym sekwencja pochodzi z treści, to **dokument** — i tam jest to poprawne, bo
dokument *jest* sekwencją. Karta jest układem. Dwa różne byty, dwa różne renderery; ta różnica jest
tym, co trzyma niezmiennik w mocy zamiast zamieniać go w wyjątek.

---

## 11. Poziomy logiki i formuły

Dwa poziomy, nie trzy:

1. **Czyste dane** — pole wskazuje wartość. Zero logiki.
2. **Deklaratywna formuła** — jeden współdzielony silnik za każdym polem obliczanym. Bez pętli, bez
   gałęzi, bez efektów ubocznych: bezpieczna z definicji, wymaga walidacji, nie piaskownicy.
   **Domyślna i jedyna ścieżka.**

Poziom skryptowy jest wycofany; co to usunęło i dlaczego — patrz [decisions.md](decisions.md).

**Zakaz gałęzi pełni ważniejszą rolę niż bezpieczeństwo: uniemożliwia treści napisanie silnika reguł.**
Autor nie może zapisać „jeśli ciężki pancerz, to zeruj zręczność", bo nie ma czym. Może napisać
`min(zręczność, pancerz.limit)` — ale to arytmetyka nad tym, co MG sam założył, a limit deklaruje ten
konkretny pancerz o sobie samym. `min` i `max` przenoszą trochę reguł, ale przenoszą je **do danych
konkretnego przedmiotu**, a nie do wiedzy aplikacji o kategoriach — i to jest różnica między tabelą
a silnikiem.

**Dwa konteksty ewaluacji:**

* **Wartość pochodna** — deterministyczna funkcja danych, przeliczana przy każdym odczycie
  (modyfikator cechy, maksimum zasobu, KP, klucz sortowania). **Nie może zawierać kości.**
* **Rzut** — wykonywany na żądanie; wynik jest zdarzeniem pokazywanym MG, nie wartością odczytywalną
  z danych. Kości dozwolone.

Bez tego rozdziału wartość pochodna zmieniałaby się przy każdym renderze, a kolejka tur
przetasowywałaby się sama.

**Zasięg: jeden skok, nieprzechodni.** Formuła czyta własną instancję oraz pola tego, do czego ta
instancja odwołuje się bezpośrednio — pozycje slotów — i nie idzie dalej. Odczytane przez skok pole
nie może samo być wynikiem skoku. Pokrywa to KP z pancerza i efektów oraz obrażenia z broni, a cykle
stają się niemożliwe strukturalnie: bez wykrywania cykli, bez kolejności przeliczania, bez inwalidacji
zależności.

**Eksplodujące kości** są własnością notacji kości z twardym limitem eskalacji, a nie pętlą pisaną
przez autora treści.

Formuła jest walidowana wobec właściwości rekordu. Odwołanie do nieistniejącego pola jest błędem
kompilacji, jeśli formuła jest deklarowana w zestawie.

**Kryterium powrotu do dyskusji:** jeśli okaże się, że poziom 2 nie pokrywa większości mechanik klasy
„trad", błędne jest założenie o klasie z sekcji 1 — wtedy wracamy do zakresu produktu, a nie
dopisujemy skryptów.

---

## 12. Rejestr i kampania — granica własności

Podział przebiega wzdłuż jednej linii: **co jest statyczne i wspólne, a co zmienne i własne.**

| Co | Gdzie | Charakter |
|---|---|---|
| Paczki | `Dokumenty\DungeonApp\Packs\<paczka>\` | Instalowane, tylko do odczytu, wspólne. Dokument użytkownika — ma być widoczny i kopiowalny. |
| Kampanie | `Dokumenty\DungeonApp\Campaigns\<id>\` | Instancje, nakładki, stan narzędzi, manifest. |
| Układy biurka | `%LocalAppData%\DungeonApp\layouts\` | Stan aplikacji, nie dokument. |

Kampania ma być widocznym, przenośnym, kopiowalnym dokumentem, a nie ukrytym stanem aplikacji.
Układ biurka nim nie jest i dlatego mieszka gdzie indziej.

Manifest kampanii niesie **jedną listę paczek** o kształcie `{ id, major }`.

**Dwa magazyny stanu, każdy z własnym źródłem kształtu:**

| Magazyn | Co trzyma | Kto waliduje kształt |
|---|---|---|
| bloki danych | stan narzędzi biurka | `DataBlockShape`, znany w czasie kompilacji |
| magazyn instancji | nakładki instancji, wraz z zagnieżdżonymi | typ treści z zestawu |

Dzięki rozdzieleniu walidacji `DataBlockRegistry` zostaje szczery: **„nieznany blok" nadal znaczy
„inny build aplikacji", a nie „brak zainstalowanej paczki".**

Oba magazyny stoją na tych samych prymitywach zapisu — zapis przez plik tymczasowy plus atomowe
przeniesienie, licznik generacji, wykrywanie przerwanego zapisu. Patrz sekcja 19, pytanie 3.

---

## 13. Powłoka, nawigacja, powierzchnie

**System okien zostaje bez zmian wizualnych i bez zmian w geometrii.** Okno hostuje **narzędzie
biurka**, nie kartę.

**Karta nie jest oknem.** Jest reprezentacją: widokiem szczegółowym, który pojawia się w rejestrze
po kliknięciu pozycji i który narzędzie może pokazać, gdy MG chce zobaczyć szczegóły instancji.

**Nawigacja ma dwa niezależne poziomy.** Globalna szyna mówi, w której części aplikacji jesteś.
Przełącznik powierzchni mówi, na której powierzchni otwartej kampanii. Rozdzielenie jest
rozstrzygnięciem, nie szczegółem układu — z niego wynika reszta tej sekcji.

**Globalna szyna ma trzy pozycje i to jest liczba docelowa**, nie stan przejściowy do zapełnienia:

* **Kampanie** — półka i wejście w kampanię.
* **Rejestr** — przeglądanie zainstalowanej treści **poza kampanią**. Tylko do odczytu.
* **Ustawienia.**

Sufit to pięć. Dojść mogą jeszcze zarządzanie paczkami (dziś widok podrzędny rejestru) oraz
autorstwo treści, jeśli pytanie 4 z sekcji 19 rozstrzygnie się na jego korzyść. Nic poza tym nie
przechodzi kryterium: wszystko inne jest albo powierzchnią kampanii, albo oknem biurka, albo
rodzajem treści.

**Zestaw nigdy nie wnosi powierzchni ani pozycji szyny.** Wnosi typy treści i narzędzia. Nowe
narzędzie — tracker ekonomii, zegar świata — jest oknem biurka, bo tak rozstrzyga kryterium poniżej.

### Kampania jest zbiorem powierzchni

**Kampania nie jest tożsama z biurkiem.** Powierzchnie są **kompilowane i policzalne w czasie
budowania** — nigdy wyprowadzane z treści — i jest ich kilka, nie kilkanaście. Przełącznik między
nimi należy do kampanii, nie do globalnej szyny; dzięki temu szyna pozostaje w pełni globalna.

* **Biurko** — okna narzędzi. Stan świata oglądany kątem oka, w wielu rzeczach naraz.
* **Świat** — przegląd instancji tej kampanii, lista plus karta.
* **Fabuła** — dokumenty przygody.
* **Kronika** — pełna historia zmian. Jednocześnie okno (ogon ostatnich zdarzeń) i powierzchnia
  (całość). To nie jest niespójność, tylko dwie długości tego samego.

### Okno czy powierzchnia — kryterium

Zasięg danych nie wystarcza, bo biurko i powierzchnie mają ten sam zasięg. Rozstrzyga **tryb
obcowania**:

> Czy patrzy się na to kątem oka obok innych rzeczy, czy się w tym przebywa?

**Peryferyjne i równoczesne → okno biurka.** Kolejka tur, drużyna, zegar świata, kostki, notatka
sesyjna, ekwipunek.

**Centralne i wyłączne → powierzchnia.** Długi tekst, w którym się czyta i scrolluje. Maksymalizacja
okna daje rozmiar, ale nie daje wyłączności — nadal jest ramką z paskiem tytułu i resztą biurka pod
spodem.

### Rejestr

Nie zyskuje wymiaru „system". Zyskuje trzy filtry:

* **po kategorii** — właściwość typu treści,
* **po paczce** — uczciwy wymiar, bo tam treść faktycznie mieszka,
* **po typie treści** — narzędziowo.

Jeśli build zawiera dwa zestawy, rejestr pokazuje oba i filtruje po paczce. Silnik nadal nie wie, że
istnieje coś takiego jak system.

**Grupowanie z danych jest legalne wewnątrz skompilowanego ekranu, nielegalne w nawigacji.** Ekran
rejestru jest skompilowany, jego układ nie pochodzi z treści, a z treści pochodzi wyłącznie
zawartość jednego wymiaru. Różnica jest sprawdzalna po skutku awarii: literówka psuje **etykietę
zakładki**, a nie nawigację. Nie da się nią zgubić drogi powrotnej.

**Pozycje nierozwiązane i paczki odrzucone muszą mieć na tym ekranie swoje miejsce** — rejestr jest
jedynym kanałem, którym MG dowiaduje się, czego zabrakło i dlaczego. Projekt ekranu, który tego nie
przewiduje, jest niekompletny funkcjonalnie, nie kosmetycznie.

**Rejestr nie jest miejscem wewnątrz kampanii.** Wybór wpisu w kampanii jest **momentem, nie
miejscem**: przywoływanym z narzędzia, filtrowanym do paczek kampanii, znikającym po wyborze.

**Tworzenie, edycja i usuwanie treści w rejestrze są poza pierwszą wersją.**

---

## 14. Narzędzia biurka i system okien

`PanelGeometry`, `WorkspaceSurface`, `WorkspaceLayoutStore`, `PanelWindow`, `PanelDeck`, rozdział
„desired" / „effective", debounce zapisu układu — **wszystko zostaje bez zmian.**

Zmienia się jedno: **`PanelCatalog` przestaje być listą wpisaną w powłoce, a staje się sumą tego, co
wnoszą zestawy.** Korzeń kompozycji bierze listę zestawów; każdy wnosi swoje narzędzia. Nic w `Core`
ani `Desktop` nie nazywa żadnego zestawu po imieniu.

Filtrowania narzędzi per kampania **nie budujemy.** Biurko pokazuje to, co build ma; MG otwiera, co
chce. **Wyzwalacz do powrotu:** drugi zestaw.

Stan narzędzia zostaje w blokach danych. Patrz sekcja 19, pytanie 2.

---

## 15. Wersjonowanie

Dwie niezależne osie.

1. **Wersja typu treści.** Wpis ją deklaruje i tą ścieżką migruje. Zmiana addytywna (nowe opcjonalne
   pole) nie wymaga bumpa; zmiana łamiąca wymaga, a wpis deklarujący starszą wersję jest **oznaczony,
   nie wczytany na oślep**.
2. **Wersja paczki.** Kampania deklaruje `{ id, major }`. Minor rozwiązuje się normalnie; major
   zostawia referencję nierozwiązaną, dopóki kampania świadomie nie zaktualizuje deklaracji.
   **Nigdy cicha podmiana treści pod otwartą kampanią.**

Wcześniejsza trzecia oś — wersja katalogu elementów i kontraktów — **znika**: katalog nie jest już
bytem, z którego wybierają dane, a kontrolki i interfejsy kompilują się razem z tym, co ich używa.

Aktualizacja paczki w obrębie tej samej wersji major może zmienić wartości we wpisie. To jest
akceptowane i jest jedyną formą propagacji, jakiej chcemy: nakładka należy do kampanii i przeżywa,
a MG widzi zmianę.

**Migracji nie budujemy**, dopóki jakaś wersja nie zostanie faktycznie podniesiona — spójnie
z decyzją, którą projekt już podjął dla bloków danych.

---

## 16. Przepływy

**(a) Start.** Zestawy są kompilacyjne i nie mogą zawieść. Krok startowy wczytuje paczki z dysku,
buduje rejestr, oznacza pozycje nierozwiązane. Wadliwa treść nie blokuje startu.

**(b) Między sesjami.** Rejestr: przeglądanie, filtry, widoczne pozycje nierozwiązane. Nowy przedmiot
to nowy plik w paczce — bez przebudowy.

**(c) Otwarcie kampanii.** Manifest → zadeklarowane paczki → rozwiązanie referencji → wczytanie
instancji (wartości wpisu scalone z łatką) i stanu narzędzi → biurko z zapisanego układu.

**(d) Wprowadzenie wpisu do świata.** Wybór z rejestru filtrowanego do paczek kampanii → nowa
instancja: nowe id, referencja, pusta łatka, zawartość slotów z wpisu. Trzy gobliny to trzy
wywołania tej ścieżki.

**(e) Wpis widziany przez cudzego konsumenta.** Konsument zna interfejs; typ treści go implementuje;
konsument dostaje obiekt. Tracker tur, wiersz ekwipunku i sumowanie wkładów to trzy zastosowania
jednego mechanizmu.

**(f) Zmiana stanu.** Edycja pola na karcie instancji → nowy rekord przez `with` →
`CampaignSession.ExecuteAsync` → różnicowanie wobec wpisu → zapis łatki → zdarzenie → odświeżenie
widoku. **To dokładnie ta ścieżka, którą dziś przechodzi licznik** — i po to licznik istniał.
Zmienia się magazyn docelowy, nie kształt przepływu.

**(g) Prowadzenie przygody.** Powierzchnia Fabuła → renderer dokumentu nad instancją → odhaczenie
zapisuje się jako klucz łatki tą samą ścieżką co (f).

---

## 17. Delta wobec dzisiejszego kodu

Stan faktyczny opisuje [hld.md](hld.md); tu jest wyłącznie różnica, którą trzeba pokonać.

**Ginie z `Core/Content`:** `Template`, `CardElement`, `TraitListElement`, `ProseElement`, `Trait`,
`FieldDeclaration`, `FieldName`, `FieldType`, `FieldValue`, `ContractFill`, `SummaryContract`,
`SummaryContractResolution`. Z `ContentPackLoader` — dwuprzebiegowe rozwiązywanie szablonów, reguła
kolizji id, walidacja wartości wobec deklaracji pól. Z 54 testów `ContentPackLoaderTests` zostaje
mniej więcej połowa: te pilnujące niezmienników, które przestają istnieć, odpadają razem z nimi.

**Przeżywa z `Core/Content`:** `ContentId`, `EntryAddress`, `TemplateReference`, `PackVersion`,
`Pack`, `Entry`, `RegisteredEntry`, `RejectedPack`, `ContentRegistry`, `EntryUnresolvedReason`,
`ContentPackLoader` — w wersji istotnie prostszej. Adresowanie `paczka:id`, rejestr i pozycje
nierozwiązane zostają i są dalej potrzebne.

**Przeżywa z `Desktop`:** `TraitListElementView`, `TraitRowViewModel`, `ProseElementView` — jako
kontrolki, ze zmienionym statusem (sekcja 5.2), nie jako katalog. `RegistryViewModel`
i `RegistryEntryRowViewModel` w większości. Cały framework okien bez zmian. `CardViewModel` zostaje
przepisany na dispatch po typie treści.

**Nowe:** projekt `DungeonApp.Content.<x>`, renderer dokumentu, magazyn instancji, prymityw zapisu
atomowego, test granicy słownictwa rozszerzony na `Desktop`.

**Usunięte poza warstwą treści:** `tools/MockupRenderer` i `design/mockups/*.axaml` — zastąpione
procesem projektowym prowadzonym poza repozytorium. Renderer weryfikował transkrypcję do XAML-a, nie
projekt; weryfikacją zostaje uruchomienie aplikacji.

**Bez zmian:** `Campaign`, `CampaignDataBlocks`, `CampaignEvents`, `CampaignSession`,
`JsonCampaignRepository`, `DataBlockRegistry`, `ITool`, `PanelGeometry`, `WorkspaceSurface`,
`WorkspaceLayoutStore`, `CoreIndependenceTests`, format pliku wpisu, pięć zakazów.

---

## 18. Granice mechaniczne

| Granica | Jak egzekwowana | Status |
|---|---|---|
| `Core` bez Avalonii | test po referencjach zestawu | istnieje |
| `Core` i `Desktop` bez słownictwa treści | skan źródeł po słowniku z zestawu | **do napisania — przed rozbiórką starego, nie po** |
| `Core` i `Desktop` nie referencują zestawu | test po referencjach | do napisania |
| Narzędzie nie introspekcjonuje typu treści | przegląd; kandydat na test | do rozstrzygnięcia |
| Brak kaskad zmian stanu | `MaxEventsPerCommand` jako tripwire | istnieje, uzasadnienie do przepisania |

**Kolejność wobec rozbiórki jest wiążąca:** mechanizm zastępczy powstaje **przed** usunięciem
mechanizmu, który zastępuje. Inaczej istnieje okno, w którym granicy nie pilnuje nic.

**Gdzie inwestować w testy:** w to, co przeżyje kolejną wersję architektury. Framework okien przeżył
już trzy. Warstwa treści nie przeżyła żadnej i nie zasługuje dziś na gęste pokrycie.

**`TreatWarningsAsErrors`** przestaje być higieną i staje się częścią historii walidacji: skoro
kompilator jest walidatorem treści, jego ostrzeżenia są ostrzeżeniami o treści.

---

## 19. Pytania otwarte

1. **Format pliku wpisu.** JSON zawodzi dokładnie w jednym miejscu — długi tekst. Po wejściu slotów
   potwór traci dwa z trzech bloków prozy (akcje i cechy szczególne stają się dołączonymi wpisami),
   więc zostaje jeden długi tekst na plik — a wtedy naturalnym kształtem jest **front-matter plus
   treść**, ten sam, który przyjęto dla dokumentu. **Rozstrzygnąć po slotach, nie przed.** Nie robić
   w międzyczasie: tablicy napisów udającej akapity, prozy we front-matterze, własnego DSL-a.
2. **Czy `DataBlockShape` nadal zarabia na siebie.** Istnieje, żeby sprawdzać kształt i serializować
   zgodnie z nim. Rekord daje pierwsze od kompilatora, deserializator drugie od typu. Zostaje
   potrzebne wyłącznie „ten build nie zna tego bloku / tej wersji", co nie wymaga kształtu.
   **Wyzwalacz:** pierwsze prawdziwe narzędzie biurka — zbudować je na typowanym rekordzie
   i sprawdzić. Nie ruszać wcześniej.
3. **Trzy własne implementacje zapisu atomowego.** `JsonCampaignRepository` i `WorkspaceLayoutStore`
   mają osobne; magazyn instancji byłby trzecią. Wydzielić jeden prymityw **zanim** powstanie trzeci
   użytkownik.
4. **Autorstwo treści w aplikacji.** Rejestr jest tylko do odczytu. Pytanie zmniejszyło się o połowę
   (typy treści pisze się w IDE, kompilator daje komunikaty), ale zostaje dla wpisów. Najmocniejszy
   motywator: MG ubiera goblina i nie ma jak zapisać tego jako czegoś wielokrotnego użytku.
   Obejście przez wpisy lokalne dla kampanii pozostaje odrzucone.
5. **Katalog pól formularza dokumentu i kształt markera.**
6. **Czy silnik formuł potrzebuje tablicy przeglądowej.** Modyfikator cechy to czysta arytmetyka, ale
   premia z biegłości i stopnie kości w Savage Worlds są tabelami. Tablica stała nie łamie zakazu
   gałęzi, ale poszerza język. Do rozstrzygnięcia przy pierwszym realnym typie treści.
7. **Jakie jeszcze pola trafiają do nakładki poza stanem i slotami.** Reguła rozstrzygająca jest
   zapisana; brakuje przejścia przez realny system.

### Nawyk do przerwania

Sześć rzeczy w repozytorium jest zbudowanych i nieużywanych: grupowanie rejestru (z testami, bez
konsumenta), `Category` / `Descriptor` rozwiązywane i niepokazywane, `SelfDescribing` parsowane
i nieczytane, `WarmPanelVisualStep`, `UnknownModule`, `Packs` w manifeście. Dwie z nich po tej
zmianie **umrą, zamiast doczekać konsumenta**.

Zasada przeciw temu jest już zapisana, ale wąsko, dla pól: *wożenie pola, dla którego świadomie nie
przewidujemy zastosowania, jest gorsze niż jego brak.* Rozszerzyć z pól na mechanizmy:
**nic nie wchodzi bez konsumenta w tym samym wycinku.**

---

## 20. Kolejność prac

| # | Krok | Dlaczego tu |
|---|---|---|
| 1 | Projekt `DungeonApp.Content.<x>`; `monster` i `gear` przeniesione do rekordów; `Core` dostaje typy przez wąski interfejs; rejestr wygląda identycznie | najmniejszy wycinek dowodzący tezy |
| 2 | Test granicy słownictwa rozszerzony na `Desktop`, słownik z zestawu | **mechanizm przed rozbiórką**, nigdy odwrotnie |
| 3 | Rozbiórka: `Template`, `CardElement`, `FieldValue`, dwuprzebiegowe rozwiązywanie, martwe testy | dopiero gdy 2 stoi |
| 4 | Zaprojektowana `MonsterCardView` — pierwsza prawdziwa karta | to jest cel całej operacji |
| 5 | Odrzucanie per plik wpisu; pozycje nierozwiązane widoczne w rejestrze | zamyka sekcję 8 |
| 6 | Jeden prymityw zapisu atomowego | **przed** magazynem instancji |
| 7 | Magazyn instancji + nakładki (sekcja 6.1) | pierwszy realny stan kampanii |
| 8 | Pierwsze prawdziwe narzędzie biurka; przy tej okazji weryfikacja pytania 2 z sekcji 19 i usunięcie licznika | |
| 9 | Formuły, sloty, dokument | kolejność do ustalenia osobno |

Kroki 1–5 są jednym spójnym przejściem i nie powinny być przerywane w środku — między 3 a 4
aplikacja nie ma czym renderować karty.
