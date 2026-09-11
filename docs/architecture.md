# DungeonApp — architektura

**Status: projekt docelowy.** Ten dokument rozstrzyga, **jak ma być**. Przy rozbieżności z kodem
prawdą o zamiarze jest ten dokument, a prawdą o kodzie jest [code-map.md](code-map.md). Nad jednym
i drugim stoi [CLAUDE.md](../CLAUDE.md).

Kierunki, które rozważono i odrzucono, wraz z uzasadnieniami, są w [decisions.md](decisions.md).
**Czyta się je przed zaproponowaniem zmiany, nie po.**

**Jak czytać.** Część I tłumaczy, czym aplikacja jest i jak działa, bez żargonu — to wystarczy,
żeby się w projekcie rozeznać, i na jej końcu można przestać. Część II jest projektem: rozstrzyga
i uzasadnia. Część III mówi, co z tego jest zbudowane i w jakiej kolejności powstaje reszta.

Odwołania między sekcjami są **po nazwie, nie po numerze** — numery się przesuwają, nazwy nie.

---
---

# CZĘŚĆ I — ORIENTACJA

## 1. Czym to jest

Siedzisz przy stole z ludźmi. Gracie w papierowe RPG — kości, kartki, wyobraźnia. Ty prowadzisz.
Przed tobą stoi laptop, ale nie po to, żeby grać na nim w grę — po to, żeby przestać gubić rzeczy.
Ile ten goblin ma jeszcze życia. Kto jest w kolejce. Co drużyna miała w plecaku trzy sesje temu.
Czy brama w tej przygodzie została już otwarta.

To jest cała aplikacja. Jedna osoba, jedna maszyna, żadnej sieci, żadnych kont. Gracze jej nigdy
nie widzą. Nie jest wirtualnym stołem, nie jest grą, nie jest generatorem przygód. Jest
**księgowym twojej kampanii**.

**Problem, który to rozwiązuje.** Programy dla prowadzących dzielą się na dwa gatunki i oba
zawodzą z przeciwnych stron. Jedne są napisane pod jeden system — działają świetnie, dopóki grasz
dokładnie w to, pod co powstały, a gdy chcesz własnego potwora z własną mechaniką, musisz czekać
na autora albo forkować cudzy projekt. Drugie próbują być konfigurowalne do końca — i wtedy
projektujesz własny interfejs w plikach tekstowych, a program staje się kiepskim edytorem stron.
Ta aplikacja szuka trzeciego wyjścia i płaci za to konkretną, przemyślaną cenę.

**Zakres** celuje w klasę systemów „trad": nazwane statystyki, rozstrzyganie akcji rzutem
z modyfikatorem, jakaś forma kolejności działania. D&D, Pathfinder, większość OSR, Call of
Cthulhu, Savage Worlds. Gry bez kości i systemy oparte w rdzeniu na puli sukcesów są poza
zakresem — świadomie, jako decyzja o produkcie, nie o technice.

**Kampania jest sesją, światem i zapisem gry naraz.** Utworzenie kampanii jest utworzeniem stanu
świata. Nie ma bytu pośredniego między kampanią a tym, co w niej żyje — warstwa scen nie powstaje.

## 2. Zasada rozstrzygająca

> **Aplikacja liczy. Ty decydujesz, co policzyć.**

Program nigdy nie podejmuje decyzji, której ty nie podjąłeś. Jeśli twój wojownik ma pancerz,
a pancerz mówi o sobie „daję trzy", to program doda trójkę — bo to ty założyłeś ten pancerz,
a dodawanie jest arytmetyką. Ale program nigdy nie stwierdzi, że zaklęcie właśnie wygasło, że dwa
efekty się nie kumulują albo że ognista kula trafiła tych czterech gobliów, a tamtego nie.

To nie jest brak funkcji. To jest granica, po której drugiej stronie zaczyna się gra komputerowa —
a gra komputerowa gra sama, i wtedy człowiek przy stole przestaje być potrzebny.

## 3. Notatnik i formularze

Metafora, która trzyma całą architekturę.

Wyobraź sobie, że masz **bardzo dobry notatnik** i **komplet zaprojektowanych formularzy**.

**Notatnik** wie, jak trzymać kartki, jak je przekładać, jak zsumować kolumnę liczb i jak niczego
nie zgubić, gdy zamkniesz go w połowie zdania. Nie wie nic o smokach. To jest rdzeń programu i jego
okna.

**Formularze** to karta potwora, karta przedmiotu, karta zaklęcia. Ktoś je narysował — z rozmysłem,
tak żeby statblok wyglądał jak statblok, a nie jak lista pól jedno pod drugim. Są wdrukowane
w notatnik: żeby zmienić projekt formularza, trzeba wydać nowy notatnik. I bardzo dobrze, bo
projekt formularza to decyzja projektowa, nie notatka.

**Wypełnione formularze** to twoja treść: ten goblin, tamten miecz, ta przygoda. Tych jest tysiąc,
dopisujesz je bez przerwy, czasem w środku sesji. To są zwykłe pliki tekstowe i mają takie zostać.

A **twoja kampania** to nie kopia tych formularzy, tylko warstwa notatek nad nimi. Kiedy
wprowadzasz goblina do gry, nie przepisujesz go — zakładasz na niego kalkę i zapisujesz wyłącznie
to, co go odróżnia: że ten ma trzy życia zamiast siedmiu i że nazywa się Krzywy. Jeśli kiedyś
poprawisz w bibliotece błąd w gobliniej statystyce, poprawka przechodzi przez kalkę do wszystkich
twoich kampanii, a twoje notatki zostają nietknięte. Zmiana treści działa jak patch balansujący
grę, nie jak coś, co psuje zapisane sesje.

## 4. Co program wie, a czego wiedzieć nie może

To jest sedno całego projektu.

Program **wie, czym jest potwór i jak go pokazać**. Jego karta jest zaprojektowana, nie poskładana
— linia nad cechami jest tam, gdzie ma być, sześć atrybutów stoi w rzędzie, akcje są oddzielone.
Bo to jest kwestia projektu graficznego i ktoś musi ją podjąć świadomie.

Program **nie wie, czym jest D&D**. Nie zna klasy pancerza, nie zna pojęcia poziomu, nie ma w sobie
listy systemów do wyboru.

Jak to możliwe naraz? Bo wiedza o potworze mieszka w **jednym, wyraźnie odgrodzonym pudełku**
z napisem „to jest do D&D". Reszta programu tego pudełka nigdy nie otwiera. Za pół roku grupa mówi
„gramy w Pathfindera" — robisz drugie pudełko, obok. Program się nie zmienia, bo nigdy nie
wiedział, że to pierwsze dotyczyło D&D.

**Wcześniej projekt szedł inną drogą i warto wiedzieć jaką**, bo ślady tamtej zostały w kodzie.
Zabraniał tej wiedzy *wszędzie* i pilnował tego w ten sposób, że karta była budowana z klocków
opisanych w pliku tekstowym. Cel był słuszny. Ale budowanie kart z plików było tylko *jednym ze
sposobów* dojścia do celu, a z czasem zaczęło się mylić z samym celem. Cena okazała się wysoka:
nikt nie mógł zaprojektować karty, a format zaczął przeciekać — pojawiały się w nim flagi, które
udawały, że mówią o treści, a naprawdę wymuszały układ.

Teraz cel jest ten sam, a sposób inny: wiedza nie jest zakazana, tylko **umiejscowiona**.

## 5. Jak się z tego korzysta

**Między sesjami** przeglądasz bibliotekę wszystkiego, co masz zainstalowane — potwory, przedmioty,
zaklęcia — pogrupowaną tak, jak ty je widzisz, a nie tak, jak program je trzyma. Jeśli któraś treść
jest zepsuta, widzisz ją jako zepsutą; nic nie znika po cichu. Dopisanie nowego przedmiotu to nowy
plik i zero czekania.

**Przy stole** masz biurko z pływającymi oknami — kolejka tur, drużyna, notatka, zegar świata, co
potrzeba. Patrzysz na nie kątem oka, robiąc coś innego. Obok biurka są trzy inne „pokoje" tej samej
kampanii: **świat** (co w niej właściwie żyje), **fabuła** (tekst przygody z odhaczanymi krokami —
kilka grup może grać tę samą przygodę, każda z własnymi odhaczeniami) i **kronika** (co się
wydarzyło).

Podział między oknem a pokojem ma jedno kryterium: **czy patrzysz na to obok innych rzeczy, czy
w tym przebywasz.**

**W trakcie gry** wyciągasz goblina z biblioteki i staje się *tym* goblinem — z własnym życiem,
własnym imieniem, własnym stanem. Trzy gobliny to trzy takie gesty. Wszystko, co potem na nim
zmieniasz, to twoje notatki na kalce.

## 6. Czego program nigdy nie zrobi

Nigdy nie zapyta „jeżeli". Nigdy nie będzie wiedział, ile coś trwa i kiedy się kończy. Nigdy nie
rozstrzygnie, czy dwie rzeczy się kumulują. Nigdy nie rozniesie efektu na kilka postaci naraz.
I nigdy nie zrobi czegoś dlatego, że stało się coś innego.

**Kanoniczne brzmienie tych pięciu zakazów jest w [CLAUDE.md](../CLAUDE.md)** i to ono obowiązuje.
Uzasadnienie, konsekwencje i test do stosowania przy nowych funkcjach są w sekcji *Granica
automatyzacji*.

---

> **Tu można przestać**, jeśli chodziło o rozeznanie się w projekcie. Dalej zaczyna się projekt
> techniczny: te same rzeczy, ale rozstrzygnięte i uzasadnione.

---
---

# CZĘŚĆ II — PROJEKT

## 7. Słownik

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

## 8. Warstwy i granice

| Warstwa | Projekt | Charakter | Co wolno wiedzieć |
|---|---|---|---|
| **Silnik** | `DungeonApp.Core` | kompilowana | kampania, bloki danych, zdarzenia, zapis, wczytywanie paczek, rejestr, magazyn instancji, silnik formuł. **Zna `Entry`. Nie zna `Monster`.** |
| **Powłoka** | `DungeonApp.Desktop` | kompilowana | szyna, powierzchnie, framework okien, kontrolki wielokrotnego użytku. **Też nie zna `Monster`.** |
| **Zestaw** | `DungeonApp.Content.<x>` | kompilowana | typy treści, widoki kart, narzędzia biurka. **Jedyne miejsce, gdzie wolno być konkretnym.** |
| **Paczka** | `Dokumenty\DungeonApp\Packs\` | dane | wpisy i dokumenty. Zmienne, dodawane w trakcie sesji. |
| **Kampania** | `Dokumenty\DungeonApp\Campaigns\` | stan | instancje, nakładki, stan narzędzi. |

```
        ┌──────────────────────────────────────────────┐
        │  DungeonApp.Content.<x>          ZESTAW      │
        │  typy treści · karty · narzędzia             │
        │  ← jedyne miejsce, które zna D&D             │
        └────────────────────┬─────────────────────────┘
                             │
        ┌────────────────────▼─────────────────────────┐
        │  DungeonApp.Desktop              POWŁOKA     │
        │  okna · powierzchnie · szyna · kontrolki     │
        └────────────────────┬─────────────────────────┘
                             │
        ┌────────────────────▼─────────────────────────┐
        │  DungeonApp.Core                 SILNIK      │
        │  kampania · zapis · zdarzenia · rejestr      │
        └──────────────────────────────────────────────┘

   paczka   ──wskazuje──►  typ treści w zestawie
   kampania ──wskazuje──►  paczki, które sama zadeklarowała
```

- **Strzałki idą tylko w dół.** Silnik nie wie, że powłoka istnieje; powłoka nie wie, że istnieje
  jakikolwiek zestaw.
- **Zestawy nigdy nie referencują się nawzajem.** Byłaby to krawędź wewnątrz jednej warstwy, z całym
  bagażem, którego unikamy gdzie indziej: problem diamentu, kolejność wczytywania, wersjonowanie
  kaskadowe.
- **Ładowanie zestawów jest statyczne — referencją projektu, nigdy `Assembly.LoadFrom`.** Ładowanie
  wtyczek w czasie wykonania przywróciłoby cały model piaskownicy, który wycofanie skryptów usunęło,
  i kupiłoby zero: instalującym jest autor repozytorium.

**Dziś zestaw jest jeden.** Liczba mnoga jest zdolnością, nie planem. Drugi powstaje w dniu,
w którym naprawdę zmienia się system, i jest wtedy równoległy, nie zależny.

**Granica jest sprawdzalna mechanicznie**, tak samo jak dzisiejszy zakaz Avalonii w `Core`: `Core`
i `Desktop` nie referencują żadnego zestawu, a ich źródła nie zawierają słownictwa treści — skan po
słowniku wyprowadzonym z zestawu, więc zakaz poszerza się sam wraz z treścią. Szczegóły w sekcji
*Granice mechaniczne*.

## 9. Gdzie biegnie linia między kodem a danymi

Cztery powody, dla których cokolwiek robi się danymi. Trzy z nich są tu martwe:

| Powód | Czy obowiązuje |
|---|---|
| Zmienia się często | **tak** — dla wpisów. Nowy przedmiot potrafi powstać w środku sesji |
| Zmienia to ktoś, kto nie ma kompilatora | nie — autor treści i autor programu to ta sama osoba |
| Zmiana nie może wymagać przebudowy | nie — przebudowa trwa sekundy, na tej samej maszynie |
| Wadliwa dana nie może wywrócić builda | nie — kod ma wersję mocniejszą: kompilator odrzuci to, zanim cokolwiek wystartuje |

Stąd linia: **kształt jest kodem, wartości są danymi.**

Skutek uboczny, który dużo załatwia: **literówka w nazwie pola przestaje być odrzuconą paczką przy
starcie, a staje się błędem kompilacji** — z podaną linią i podpowiedzią. Kompilator jest
walidatorem treści.

## 10. Deklaracja treści

### 10.1 Typ treści to para: rekord + widok

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
* **Liczba widoków rośnie liniowo** z liczbą rodzajów razy liczbę systemów — realistycznie 6–8 na
  system. To praca projektowa, przyjęta świadomie.

**Wyzwalacz do widoku domyślnego:** pierwszy rodzaj treści, którego nie chce się zaprojektować.
Wcześniej widok domyślny jest zaproszeniem, żeby przestać projektować karty, i nie powstaje.

### 10.2 Kontrolki, nie katalog

Lista cech, blok prozy, lista pozycji, pasek zasobu, akcja rzutu, znacznik binarny **zostają** — ale
jako **kontrolki wielokrotnego użytku**, komponowane przez projektanta karty, a nie jako pozycje
katalogu wybierane przez dane. Ginie wyłącznie **wybór kontrolki przez dane**, nie kontrolka.

Konsekwencją jest, że zniknęły parametry w rodzaju `compact` czy `selfDescribing`. Były deklarowane
jako „stwierdzenia o treści, nie o układzie", a istniały wyłącznie po to, żeby wymusić konkretny
układ w rendererze, który układu nie znał. Zaprojektowany widok nie potrzebuje ich mówić — on je po
prostu ma.

### 10.3 Droga treści: z dysku na ekran

```
 goblin.json                             ← plik, który piszesz ręcznie
     │
     │  loader silnika: sprawdza tożsamość, adres, wskazanie typu
     ▼
 Entry { id, nazwa, wskazanie typu, nierozpakowane wartości }
     │                                   ← tu silnik się zatrzymuje.
     │                                     Nie zagląda do wartości.
     │  zestaw rozpakowuje wartości w swój rekord
     ▼
 Monster { Ac = 15, Hp = 7, Speed = "30 stóp", … }
     │                                   ← typowane; kompilator już to sprawdził
     │  powłoka dobiera widok po typie
     ▼
 MonsterCardView                         ← zaprojektowany układ statbloku
```

Plik wpisu:

```json
{ "id": "goblin", "name": "Goblin", "template": "dnd5e:monster", "values": { "ac": 15, … } }
```

`values` deserializuje się **wprost w rekord**, ze ścisłym traktowaniem nieznanych kluczy. Znika
własny walidator wartości wobec deklaracji pól — robi to deserializator.

Kluczowy podział: **silnik niesie kopertę, zestaw otwiera list.** Silnik wie, że coś przyszło,
skąd, pod jakim adresem i do jakiego typu się odwołuje — i na tym kończy się jego wiedza. Dzięki
temu odrzucanie treści, rejestr i oznaczanie tego, co się nie rozwiązało, działają tak samo dla
każdego zestawu i istnieją w jednym miejscu.

**Format pliku jest odtąd wyborem serializatora, nie decyzją architektoniczną.** Podmiana dotyka
jednej klasy. Patrz *Pytania otwarte*, pytanie o format.

**Konwencja nazewnicza:** identyfikatory po angielsku, etykiety i treść po polsku. Ta sama linia
obowiązuje w kodzie: `DataBlockId` ma wartość `counter`, a komunikaty dla Mistrza Gry są po polsku.

## 11. Wpis, dokument, instancja, nakładka

> **Typ treści deklaruje kształt. Wpis deklaruje wartości. Instancja deklaruje odchylenia.**

Rozróżnienie jest tym samym, co **item** i **item stack**: wpis jest esencją, instancja egzemplarzem
istniejącym w świecie. Dotyczy każdego wpisu, nie tylko przedmiotów — potwór jest instancjonowany
z tego samego powodu, dla którego stos jest instancją przedmiotu: jego obrażenia są własnością
egzemplarza, nie gatunku.

| Pytanie | Odpowiada | Przykład |
|---|---|---|
| Jaki *kształt* może mieć rzecz? | typ treści | „broń ma wagę, ilość i slot `zaklęcia`" |
| Jakie *wartości* ma konkretna rzecz? | wpis | „diamentowy miecz waży 3"; „goblin nosi tasak" |
| Co *odbiega* w tym egzemplarzu? | instancja | „ten ma Ostrość V i nazywa się Zgubą" |

Waga i ilość rozchodzą się dokładnie po tej linii: **waga jest we wpisie** (każdy diamentowy miecz
waży tyle samo — to esencja), **ilość jest w nakładce** (to własność stosu, nie przedmiotu).

**Czego nakładce nie wolno — reguła projektowa, nie blokada techniczna:** modelować *wariantu
rzeczy*. Miecz +1 to osobny wpis, nie miecz z nadpisanym polem. Kryterium operacyjne brzmi: **czy
chcesz tego użyć ponownie.** Goblin łucznik, który ma być pod ręką w każdej przyszłej potyczce, jest
wpisem. Ten jeden goblin, któremu MG dał dziś procę, jest instancją.

### 11.1 Nakładka jest rzadką łatką nad wartościami wpisu

**Instancja nie materializuje wpisu.** Aktualizacja paczki działa jak patchnote balansujący grę,
a nie jak zdarzenie psujące istniejące kampanie.

```
 wartości wpisu     { ac: 15,  hp: 7,  speed: "30 stóp",  … }
                              +
 łatka instancji    {          hp: 3                        }
                              =
 scalenie           { ac: 15,  hp: 3,  speed: "30 stóp",  … }
                              ↓
                        Monster { … }        ← typowany, jak zawsze
```

Zapis idzie w drugą stronę: widok tworzy nowy rekord przez `with`, magazyn **różnicuje go wobec
wpisu** i zapisuje samą różnicę.

* **Nazwa pola nie pojawia się w kodzie** ani przy odczycie, ani przy zapisie. Dodajesz pole do
  rekordu — i nic więcej nie musisz zmieniać.
* **Poprawka w bibliotece przechodzi do istniejących kampanii.** Klucza, którego nie ma w łatce, po
  prostu nie ma — więc bierze się z aktualnej treści. Cena: wartość może zmienić się pod Mistrzem
  Gry przy aktualizacji w obrębie wersji major. To jest ta forma propagacji, której chcemy.
* **Twoje notatki nigdy nie są po cichu nadpisane.** Klucz łatki, który zniknął z rekordu, oznacza
  instancję jako wymagającą uwagi — nigdy nie odrzuca kampanii i nigdy nie kasuje łatki.

### 11.2 Sloty i zagnieżdżanie

**Slot jest polem, nie pojemnikiem instancji.** Wyróżnia się wyłącznie tym, że jego wartością jest
lista referencji, a nie liczba. We wpisie jest listą **referencji**, w kampanii listą **instancji** —
to różne typy, więc slot nie przechodzi przez scalanie opisane wyżej i ma własną ścieżkę. Nie jest
to wyjątek, tylko drugi rodzaj pola.

Zaklinanie rozkłada się przez to bez nowego mechanizmu: silnik publikuje kontrakt wkładu, typ treści
deklaruje slot (czyli decyduje, że miecz da się zaklinać), a paczka dostarcza Ostrość V jako zwykły
wpis z własną kartą. Miecz jest pojemnikiem na zaklęcia tak samo, jak plecak na miecze.

* **Silnik nie ma zdania o tym, co wpis powinien mieć w slocie.** Co należy do esencji, decyduje
  autor treści.
* **Zawartość początkowa slotu może wskazywać wyłącznie wpisy z tej samej paczki.** Instancji to nie
  dotyczy — ona rozwiązuje referencje wobec wszystkich paczek kampanii.
* **Gdy instancja zmieni zawartość slotu, przejmuje całą listę**, nie różnicę.
* **Pozycje slotu same są instancjami** — stąd zagnieżdżenie: kampania → bohater → stos → zaklęcie.
* **Trwałość idzie za własnością.** Instancja kampanii dostaje własny plik; zagnieżdżona jest
  zapisywana wewnątrz właściciela.
* **Cykl w grafie jest dozwolony** (torba w torbie); chroni nas twardy limit głębokości zagnieżdżenia.

**Referencja nierozwiązana.** Paczki może zabraknąć, może zniknąć w niej wpis, może zmienić się
niekompatybilnie. Wtedy referencja jest **nierozwiązana, a nie martwa**: instancja zostaje, jest
jawnie oznaczona, jej nakładka nigdy nie jest po cichu nadpisana ani skasowana, a kampania otwiera
się dalej w trybie ograniczonym. Zainstalowanie brakującej paczki przywraca wszystko bez utraty stanu.

### 11.3 Dokument

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

Cztery warunki wiążące:

1. **Podzbiór markdowna jest zamknięty i nie zawiera układu.** Nagłówki, akapity, listy, cytaty,
   wyróżnienia, tabele — tak; surowy HTML, szerokości, kolumny, kolory, wymiarowanie obrazków — nie.
   Passthrough HTML jest wyłączony, bo to jedna furtka, przez którą wchodzi wszystko naraz.
2. **Marker jest walidowalny przy wczytaniu** — jednoznacznie ogranicznikowany, z własnym id.
   Dokument z markerem zniekształconym albo z id powtórzonym jest odrzucany. Bez tego literówka
   w markerze nie jest błędem, tylko zwykłym tekstem.
3. **Markery znajduje się w drzewie, nie regeksem po pliku** — regeks trafi marker w bloku kodu,
   w linku i w komórce tabeli.
4. **Pola zadeklarowane przez dokument są prywatne dla dokumentu.** W chwili, gdy formuła albo inne
   okno ma przeczytać „czy brama została otwarta", to pole musi być zadeklarowane w typie treści.

Dokument nie ma karty — ma własny renderer. To jest różnica, która trzyma *Niezmiennik interfejsu*
w mocy: **karta jest układem, dokument jest sekwencją.**

## 12. Kontrakty są interfejsami

**Wpis nie ma jednej reprezentacji.** Ten sam żelazny miecz pojawia się co najmniej w trzech: jako
karta, jako wiersz w ekwipunku (nazwa, waga, skrót obrażeń — bo miejsca jest na jedną linię), jako
uczestnik kolejki tur.

**Kształt skrótowej reprezentacji definiuje konsument, nie treść.** Konsument publikuje interfejs.
Typ treści go implementuje albo nie.

```
      ITurnParticipant  { Nazwa, KluczSortowania }
                 ▲
                 │  czyta
         ┌───────┴────────┐
         │  tracker tur   │   ← neutralny, nie wie o istnieniu systemów
         └────────────────┘

   Monster   ──implementuje──►  ✓  trafia na listę uczestników
   Creature  ──implementuje──►  ✓  (z zupełnie innego zestawu)
   Gear      ─── nie ────────►  ✗  nigdy się na niej nie pojawi
```

* **Kwalifikacja jest systemem typów.** Przedmiotu nie da się zarejestrować w kolejce tur nie
  dlatego, że narzędzie odrzuca go przy próbie, tylko dlatego, że **nigdy nie pojawia się na liście
  do wyboru**. Odrzucenie jest fizyczne, nie proceduralne, i kompilator pilnuje go za darmo.
* **Pola kontraktu są typowane per pole.** `KluczSortowania` jest liczbą, bo tak jest zadeklarowany.
  Znika ryzyko, że kontrakt zamieni się w worek napisów wymagających parsowania u konsumenta.
* **Projektowanie nowego narzędzia nie wymaga projektowania danych.** Narzędzie przychodzi z gotowym
  wyglądem i publikuje interfejs; treść tylko go implementuje.

**Reguła umiejscowienia narzędzia:**

> Narzędzie czytające pole po nazwie mieszka w zestawie, który to pole deklaruje.
> Narzędzie czytające interfejs jest neutralne i mieszka w zestawie neutralnym.

Wybór między jednym a drugim jest wyborem katalogu, w którym leży plik — **nigdy gałęzią w kodzie**.
Nie ma nigdzie rozgałęzienia „jeśli system to D&D".

**Zakaz zabezpieczający:**

> **Narzędzie nigdy nie introspekcjonuje typu treści.** Deklaruje interfejs i dostaje obiekty.

Skompilowany typ leży w tym samym procesie, więc „znajdź wszystkie typy mające pole `initiative`"
jest jedną linijką. Gdy typy były plikami danych, ta pokusa praktycznie nie istniała. Zakaz musi
więc być zapisany, a nie dorozumiany.

## 13. Paczki, wczytywanie, bezpieczeństwo

Paczka to katalog z manifestem, niosący wpisy i dokumenty. Nie niesie typów treści.

Co paczka kupuje: przestrzeń nazw dla id, jednostkę instalacji i deinstalacji, wersję, którą
kampania deklaruje. Ostatnie daje istotną własność: **zainstalowanie nowej paczki nie może po cichu
zmienić otwartej kampanii**, bo kampania rozwiązuje referencje wyłącznie wobec paczek, które sama
deklaruje.

**Wszystko, co paczka wnosi, jest homebrew z definicji**, niezależnie od tego, czy stoi w oficjalnym
podręczniku. Aplikacja nie zna pojęcia autorytetu i nie musi go znać.

**Jedna przestrzeń nazw na paczkę.** Id wpisu i id dokumentu nie mogą kolidować w obrębie jednej
paczki — kolizja czyniłaby adres niejednoznacznym, a nie tylko powtórzonym.

### 13.1 Co się dzieje, gdy treść jest zepsuta

Zasada nadrzędna: **wadliwa treść zostaje widoczna i oznaczona. Nigdy nie znika po cichu.**

| Co jest zepsute | Skutek |
|---|---|
| manifest paczki | **cała paczka odrzucona** — tożsamość nieznana, nie ma czym adresować zawartości |
| jeden plik wpisu lub dokumentu | **tylko ta pozycja oznaczona**, reszta paczki działa |
| dwie pozycje o tym samym id | **obie oznaczone** — żadna nie wygrywa po cichu |
| brakuje paczki, do której odwołuje się kampania | instancje oznaczone, notatki nietknięte, kampania otwiera się dalej |

Odrzucanie **całej** paczki za jeden wadliwy wpis obowiązywało wcześniej i odpada razem
z uzasadnieniem, które je trzymało: chodziło o to, żeby rejestr nie zawierał wpisu rozwiązanego
wobec szablonu z nieobecnej paczki. Typy treści nie pochodzą już z paczek, więc wadliwy plik nie
zagraża żadnemu innemu. Zostawało już tylko ukrywanie dwustu poprawnych potworów za jedną literówką.

Rejestr jest jedynym kanałem, którym Mistrz Gry się o tym dowiaduje — więc projekt tego ekranu,
który nie przewiduje miejsca na rzeczy zepsute, jest niekompletny **funkcjonalnie**, a nie
kosmetycznie.

### 13.2 Model bezpieczeństwa

Paczka to **czyste dane**. Nie ma czego uruchamiać, więc nie ma czego izolować. Cały model to
walidacja przy wczytaniu plus limity rozmiarowe:

* Wadliwa treść jest wykrywana **przy starcie**, nigdy przy pierwszym kliknięciu w środku sesji.
* Formuła jest walidowana nie tylko składniowo, ale i wobec pól, które realnie istnieją.
* Identyfikatory podlegają ograniczeniom znakowym. Żadna ścieżka na dysku nie jest budowana
  z danych — tożsamość paczki mieszka w jej manifeście, nie w nazwie katalogu.
* **Limity:** rozmiar paczki, głębokość zagnieżdżenia wyrażenia, głębokość zagnieżdżenia instancji,
  liczba i rozmiar kości w jednym rzucie, limit eskalacji eksplozji. Bez pętli w języku to
  wystarcza, żeby czas ewaluacji był ograniczony z góry.

## 14. Granica automatyzacji

Pytanie „co jest policzalne" nie wystarcza, bo kruszy się przy pierwszym efekcie modyfikującym
klasę pancerza. Wystarcza dopiero pytanie **kto podjął decyzję**:

> **Aplikacja sumuje. MG decyduje o składnikach.**

KP z pancerza: MG założył ten pancerz, pancerz sam o sobie mówi, ile daje, aplikacja dodała liczby.
Efekt „+2 do KP": MG dopisał go do listy, aplikacja go zsumowała. Oba są księgowością — **pod
warunkiem**, że aplikacja nie wie, czym jest Tarcza Wiary, kiedy się zaczyna, kiedy wygasa, czy się
kumuluje i na kogo działa.

**Pięć zakazów — kanoniczne brzmienie w [CLAUDE.md](../CLAUDE.md).** To nie jest przypadkowa lista:
to **kanoniczny zestaw funkcji silnika cRPG, zanegowany**. Baldur's Gate adaptuje ten sam podręcznik
co my i musi mieć wszystkie pięć, bo gra bez MG. Ten sam system, inny wykonawca, inna architektura.
Praktyczny test: **jeśli zapragniemy którejkolwiek z tych pięciu rzeczy, nie zbliżamy się do granicy
— przekraczamy ją.**

Zakazy nie mówią, że logiki nie ma. Formuły, sumowanie wkładów i wartości pochodne są logiką.
Niepotrzebne jest **wykonywanie reguł**.

### 14.1 Dwie granice do narysowania, zanim ktoś je przekroczy

Obie dotyczą funkcji, które są w planie, i obie padłyby niezauważenie.

**Runda i zegar świata.** Tracker tur i zegar świata trzymają liczby wyglądające jak czas.

> Narzędziu wolno trzymać numer rundy i czas świata. **Nic poza widokiem nie ma prawa ich
> odczytać.** Licznik jest wyświetlany i przesuwany przez MG; nie jest wejściem żadnej formuły ani
> warunkiem żadnego wygaśnięcia.

**Kronika.** Dziennik jako subskrybent zdarzeń byłby dosłowną sprzecznością z zakazem piątym.

> Wpis do kroniki jest **częścią operacji**, nie reakcją na nią. `CampaignSession.ExecuteAsync`
> zapisuje stan i wpis kroniki w jednym zatwierdzeniu.

**Przy okazji:** `MaxEventsPerCommand` jest dziś opisany jako zabezpieczenie przed pętlą. Skoro nic
nie ma prawa pisać w reakcji na zdarzenie, kaskada jest niemożliwa — więc ten limit jest **jedynym
mechanicznym egzekwowaniem zakazu piątego, jakie istnieje**. Zostaje; jego uzasadnienie wymaga
przepisania.

### 14.2 Efekt jest wkładem, nie mechanizmem

Efekt to zwykły wpis: ma typ treści, kartę, może mieć prozę. Leży na instancji jako pozycja slotu —
tak samo jak przedmiot w plecaku. Implementuje **kontrakt wkładu**: nazwa pola plus wartość.

Stąd wniosek usuwający całą klasę projektowania: **pancerz i efekt są dla aplikacji tym samym.** Oba
deklarują wkład do pola `kp`. Nie ma „systemu efektów" — są wkłady. Formuła KP brzmi „baza plus suma
wkładów do `kp`" i nie wie, skąd te wkłady przyszły ani czym są.

### 14.3 Test asystenta

Do zastosowania przy każdej przyszłej funkcji:

1. Czy MG jawnie umieścił **każdy** składnik tego wyniku?
2. Czy wynik pozostanie niezmieniony, dopóki MG czegoś nie ruszy?
3. Czy aplikacja liczy, **nie wiedząc, czym** są składniki?

Trzy razy „tak" → księgowość, wolno automatyzować. Choć raz „nie" → aplikacja co najwyżej proponuje,
a nanosi MG.

### 14.4 Wynik jest propozycją, nie zapisem

Dane kampanii są formularzem: każde pole instancji jest edytowalne, łącznie z wartością pochodną.
Narzędzie liczy i **pokazuje** wynik; to MG decyduje, czy i gdzie go nanieść. Bezpośredni zapis
w następstwie wykonanej operacji jest rzadkim wyjątkiem, zadeklarowanym jawnie — i tam, gdzie
występuje, **nie pyta o potwierdzenie**: wywołanie akcji przez MG samo w sobie jest intencją.

Zasada ta usuwa z projektu kaskady automatycznych zmian stanu, dialogi potwierdzeń i cofanie zmian
wywołanych regułą.

## 15. Niezmiennik interfejsu

> **Widok zna pełny kształt tego, co wyświetla — w czasie kompilacji. Z danych przychodzą wyłącznie
> wartości.**

Żaden widok nie odkrywa swojego kształtu w czasie wykonania. Widok, który czyta skądkolwiek, jakie
ma pola, jest naruszeniem — i jest to sprawdzalne z samego diffu.

Jedyne miejsce, w którym sekwencja pochodzi z treści, to **dokument** — i tam jest to poprawne, bo
dokument *jest* sekwencją. Karta jest układem.

## 16. Poziomy logiki i formuły

Dwa poziomy, nie trzy:

1. **Czyste dane** — pole wskazuje wartość. Zero logiki.
2. **Deklaratywna formuła** — jeden współdzielony silnik za każdym polem obliczanym. Bez pętli, bez
   gałęzi, bez efektów ubocznych: bezpieczna z definicji, wymaga walidacji, nie piaskownicy.
   **Domyślna i jedyna ścieżka.**

Poziom skryptowy jest wycofany; co to usunęło i dlaczego — patrz [decisions.md](decisions.md).

**Zakaz gałęzi pełni ważniejszą rolę niż bezpieczeństwo: uniemożliwia treści napisanie silnika
reguł.** Autor nie może zapisać „jeśli ciężki pancerz, to zeruj zręczność", bo nie ma czym. Może
napisać `min(zręczność, pancerz.limit)` — ale to arytmetyka nad tym, co MG sam założył, a limit
deklaruje ten konkretny pancerz o sobie samym. `min` i `max` przenoszą trochę reguł, ale przenoszą
je **do danych konkretnego przedmiotu**, a nie do wiedzy aplikacji o kategoriach — i to jest różnica
między tabelą a silnikiem.

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
stają się niemożliwe strukturalnie: bez wykrywania cykli, bez kolejności przeliczania, bez
inwalidacji zależności.

**Eksplodujące kości** są własnością notacji kości z twardym limitem eskalacji, a nie pętlą pisaną
przez autora treści.

**Kryterium powrotu do dyskusji:** jeśli okaże się, że poziom 2 nie pokrywa większości mechanik klasy
„trad", błędne jest założenie o klasie z sekcji *Czym to jest* — wtedy wracamy do zakresu produktu,
a nie dopisujemy skryptów.

## 17. Gdzie mieszka stan

Podział przebiega wzdłuż jednej linii: **co jest statyczne i wspólne, a co zmienne i własne.**

| Co | Gdzie | Charakter |
|---|---|---|
| Paczki | `Dokumenty\DungeonApp\Packs\<paczka>\` | Instalowane, tylko do odczytu, wspólne. Dokument użytkownika — ma być widoczny i kopiowalny. |
| Kampanie | `Dokumenty\DungeonApp\Campaigns\<id>\` | Instancje, nakładki, stan narzędzi, manifest. |
| Układy biurka | `%LocalAppData%\DungeonApp\layouts\` | Stan aplikacji, nie dokument. |

**Kampania jest dokumentem, układ okien jest ustawieniem programu.** Dlatego kampania leży
w Dokumentach — widoczna, kopiowalna, przenoszalna na pendrivie — a układ biurka w danych aplikacji.

**Dwa magazyny stanu, każdy z własnym źródłem kształtu:**

| Magazyn | Co trzyma | Kto waliduje kształt |
|---|---|---|
| bloki danych | stan narzędzi biurka: kolejka tur, zegar świata | `DataBlockShape`, znany w czasie kompilacji |
| magazyn instancji | nakładki instancji, wraz z zagnieżdżonymi | typ treści z zestawu |

Dzięki rozdzieleniu walidacji `DataBlockRegistry` zostaje szczery: **„nieznany blok" nadal znaczy
„inny build aplikacji", a nie „brak zainstalowanej paczki".** To dwa różne problemy i mają dwa różne
komunikaty.

Wszystkie magazyny zapisują tak samo: plik tymczasowy, atomowe przeniesienie, licznik generacji
wykrywający zapis przerwany w połowie. Dziś są to **trzy osobne implementacje tego samego** — patrz
*Pytania otwarte*.

## 18. Powłoka, nawigacja, powierzchnie

**System okien zostaje bez zmian wizualnych i bez zmian w geometrii.** Okno hostuje **narzędzie
biurka**, nie kartę. **Karta nie jest oknem** — jest widokiem szczegółowym, który pojawia się
w rejestrze po kliknięciu pozycji i który narzędzie może pokazać.

**Nawigacja ma dwa niezależne poziomy.** Globalna szyna mówi, w której części aplikacji jesteś.
Przełącznik powierzchni mówi, na której powierzchni otwartej kampanii. Rozdzielenie jest
rozstrzygnięciem, nie szczegółem układu — z niego wynika reszta tej sekcji.

**Globalna szyna ma trzy pozycje i to jest liczba docelowa**, nie stan przejściowy:

* **Kampanie** — półka i wejście w kampanię.
* **Rejestr** — przeglądanie zainstalowanej treści **poza kampanią**. Tylko do odczytu.
* **Ustawienia.**

Sufit to pięć. Dojść mogą jeszcze zarządzanie paczkami (dziś widok podrzędny rejestru) oraz
autorstwo treści. Nic poza tym nie przechodzi kryterium: wszystko inne jest albo powierzchnią
kampanii, albo oknem biurka, albo rodzajem treści.

**Zestaw nigdy nie wnosi powierzchni ani pozycji szyny.** Wnosi typy treści i narzędzia.

### 18.1 Kampania jest zbiorem powierzchni

Powierzchnie są **kompilowane i policzalne w czasie budowania** — nigdy wyprowadzane z treści —
i jest ich kilka, nie kilkanaście. Przełącznik między nimi należy do kampanii, nie do globalnej
szyny; dzięki temu szyna pozostaje w pełni globalna.

* **Biurko** — okna narzędzi. Stan świata oglądany kątem oka, w wielu rzeczach naraz.
* **Świat** — przegląd instancji tej kampanii, lista plus karta.
* **Fabuła** — dokumenty przygody.
* **Kronika** — pełna historia zmian. Jednocześnie okno (ogon ostatnich zdarzeń) i powierzchnia
  (całość). To nie jest niespójność, tylko dwie długości tego samego.

### 18.2 Okno czy powierzchnia — kryterium

Zasięg danych nie wystarcza, bo biurko i powierzchnie mają ten sam zasięg. Rozstrzyga **tryb
obcowania**:

> Czy patrzy się na to kątem oka obok innych rzeczy, czy się w tym przebywa?

**Peryferyjne i równoczesne → okno biurka.** Kolejka tur, drużyna, zegar świata, kostki, notatka,
ekwipunek.

**Centralne i wyłączne → powierzchnia.** Długi tekst, w którym się czyta i scrolluje. Maksymalizacja
okna daje rozmiar, ale nie daje wyłączności — nadal jest ramką z paskiem tytułu i resztą biurka pod
spodem.

### 18.3 Rejestr

Nie zyskuje wymiaru „system". Zyskuje trzy filtry: **po kategorii** (właściwość typu treści), **po
paczce** (uczciwy wymiar, bo tam treść faktycznie mieszka) i **po typie treści** (narzędziowo).
Jeśli build zawiera dwa zestawy, rejestr pokazuje oba i filtruje po paczce.

**Grupowanie z danych jest legalne wewnątrz skompilowanego ekranu, nielegalne w nawigacji.** Ekran
rejestru jest skompilowany, jego układ nie pochodzi z treści, a z treści pochodzi wyłącznie
zawartość jednego wymiaru. Różnica jest sprawdzalna po skutku awarii: literówka psuje **etykietę
zakładki**, a nie nawigację. Nie da się nią zgubić drogi powrotnej.

**Rejestr nie jest miejscem wewnątrz kampanii.** Wybór wpisu w kampanii jest **momentem, nie
miejscem**: przywoływanym z narzędzia, filtrowanym do paczek kampanii, znikającym po wyborze.

**Tworzenie, edycja i usuwanie treści w rejestrze są poza pierwszą wersją.**

## 19. Narzędzia biurka i system okien

`PanelGeometry`, `WorkspaceSurface`, `WorkspaceLayoutStore`, `PanelWindow`, `PanelDeck`, rozdział
„desired" / „effective", debounce zapisu układu — **wszystko zostaje bez zmian.**

Zmienia się jedno: **`PanelCatalog` przestaje być listą wpisaną w powłoce, a staje się sumą tego, co
wnoszą zestawy.** Korzeń kompozycji bierze listę zestawów; każdy wnosi swoje narzędzia. Nic w `Core`
ani `Desktop` nie nazywa żadnego zestawu po imieniu.

Filtrowania narzędzi per kampania **nie budujemy.** Biurko pokazuje to, co build ma; MG otwiera, co
chce. **Wyzwalacz do powrotu:** drugi zestaw.

## 20. Wersjonowanie

Dwie niezależne osie.

1. **Wersja typu treści.** Wpis ją deklaruje i tą ścieżką migruje. Zmiana addytywna (nowe opcjonalne
   pole) nie wymaga bumpa; zmiana łamiąca wymaga, a wpis deklarujący starszą wersję jest
   **oznaczony, nie wczytany na oślep**.
2. **Wersja paczki.** Kampania deklaruje `{ id, major }`. Minor rozwiązuje się normalnie; major
   zostawia referencję nierozwiązaną, dopóki kampania świadomie nie zaktualizuje deklaracji.
   **Nigdy cicha podmiana treści pod otwartą kampanią.**

Wcześniejsza trzecia oś — wersja katalogu elementów i kontraktów — **znika**: katalog nie jest już
bytem, z którego wybierają dane, a kontrolki i interfejsy kompilują się razem z tym, co ich używa.

**Migracji nie budujemy**, dopóki jakaś wersja nie zostanie faktycznie podniesiona — spójnie
z decyzją, którą projekt już podjął dla bloków danych.

## 21. Przepływy

### 21.1 Start aplikacji

```
 1.  zestawy          wpięte na sztywno — nie mogą zawieść
 2.  paczki z dysku   wczytane i sprawdzone; zepsute oznaczone, nie blokują
 3.  rejestr          zbudowany raz, tylko do odczytu
 4.  półka kampanii   lista, bez wczytywania zawartości
 5.  rozgrzewka       dane i widok biurka przygotowane, zanim klikniesz
```

Kolejność nie jest przypadkowa: **treść jest sprawdzana przed kampaniami.** Jeśli czegoś brakuje,
dowiadujesz się przy starcie, a nie przy pierwszym kliknięciu w środku sesji. Żaden krok nie ma
prawa zatrzymać wejścia do programu — awaria dowolnego degraduje do leniwego wczytywania i zostawia
ostrzeżenie na pasku.

### 21.2 Zmiana stanu: z kliknięcia na dysk

```
 edycja pola na karcie
     │
     ▼  widok tworzy nowy rekord:  monster with { Hp = 3 }
     │
     ▼  CampaignSession.ExecuteAsync — jedyne wejście do zmiany stanu
     │
     ▼  magazyn różnicuje rekord wobec wpisu → na dysk leci  { "hp": 3 }
     │
     ▼  zapis: plik tymczasowy → atomowe przeniesienie → licznik generacji
     │
     ▼  zdarzenie „zatwierdzone"
     │
     ▼  widoki odczytują stan na nowo
```

* **Jest dokładnie jedno wejście** do zmiany otwartej kampanii. Nie ma drugiej drogi.
* **Zdarzenia powiadamiają, nigdy nie zapisują.**
* **Ta ścieżka już działa i jest przetestowana od początku do końca.** Udowadnia ją licznik —
  narzędzie celowo bez sensu, które po to właśnie powstało.

### 21.3 Pozostałe

**Przeglądanie rejestru.** Sekcja `Rejestr` → lista → karta wybranej pozycji. Nie wymaga otwartej
kampanii.

**Otwarcie kampanii.** Manifest → zadeklarowane paczki → rozwiązanie referencji → wczytanie
instancji (wartości wpisu scalone z łatką) i stanu narzędzi → biurko z zapisanego układu.

**Wprowadzenie wpisu do świata.** Wybór z rejestru filtrowanego do paczek kampanii → nowa instancja:
nowe id, referencja, pusta łatka, zawartość slotów z wpisu.

**Wpis widziany przez cudzego konsumenta.** Konsument zna interfejs; typ treści go implementuje;
konsument dostaje obiekt. Tracker tur, wiersz ekwipunku i sumowanie wkładów to trzy zastosowania
jednego mechanizmu.

**Prowadzenie przygody.** Powierzchnia Fabuła → renderer dokumentu nad instancją → odhaczenie
zapisuje się jako klucz łatki tą samą ścieżką co zmiana stanu.

---
---

# CZĘŚĆ III — WYKONANIE

## 22. Delta wobec dzisiejszego kodu

Stan faktyczny opisuje [code-map.md](code-map.md); tu jest wyłącznie różnica do pokonania.

**Ginie z `Core/Content`:** `Template`, `CardElement`, `TraitListElement`, `ProseElement`, `Trait`,
`FieldDeclaration`, `FieldName`, `FieldType`, `FieldValue`, `ContractFill`, `SummaryContract`,
`SummaryContractResolution`. Z `ContentPackLoader` — dwuprzebiegowe rozwiązywanie szablonów, reguła
kolizji id, walidacja wartości wobec deklaracji pól. Z 54 testów `ContentPackLoaderTests` zostaje
mniej więcej połowa.

**Przeżywa z `Core/Content`:** `ContentId`, `EntryAddress`, `TemplateReference`, `PackVersion`,
`Pack`, `Entry`, `RegisteredEntry`, `RejectedPack`, `ContentRegistry`, `EntryUnresolvedReason`,
`ContentPackLoader` — w wersji istotnie prostszej.

**Przeżywa z `Desktop`:** `TraitListElementView`, `TraitRowViewModel`, `ProseElementView` — jako
kontrolki, nie jako katalog. `RegistryViewModel` i `RegistryEntryRowViewModel` w większości. Cały
framework okien bez zmian. `CardViewModel` zostaje przepisany na dispatch po typie treści.

**Nowe:** projekt `DungeonApp.Content.<x>`, renderer dokumentu, magazyn instancji, prymityw zapisu
atomowego, test granicy słownictwa rozszerzony na `Desktop`.

**Bez zmian:** `Campaign`, `CampaignDataBlocks`, `CampaignEvents`, `CampaignSession`,
`JsonCampaignRepository`, `DataBlockRegistry`, `ITool`, `PanelGeometry`, `WorkspaceSurface`,
`WorkspaceLayoutStore`, `CoreIndependenceTests`, format pliku wpisu, pięć zakazów.

## 23. Granice mechaniczne

| Granica | Jak egzekwowana | Status |
|---|---|---|
| `Core` bez Avalonii | test po referencjach zestawu | istnieje |
| `Core` i `Desktop` bez słownictwa treści | skan źródeł po słowniku z zestawu | **do napisania — przed rozbiórką starego, nie po** |
| `Core` i `Desktop` nie referencują zestawu | test po referencjach | do napisania |
| Narzędzie nie introspekcjonuje typu treści | przegląd; kandydat na test | do rozstrzygnięcia |
| Brak kaskad zmian stanu | `MaxEventsPerCommand` jako tripwire | istnieje, uzasadnienie do przepisania |

Skan słownictwa jest wart uwagi: **słownik zakazanych słów nie jest wpisany ręcznie — jest wyciągany
z zestawu.** Dodajesz typ „Zaklęcie" z polem „szkoła" i zakaz sam się o te słowa poszerza. Nikt nie
musi pamiętać, żeby dopisać je do listy.

**Kolejność wobec rozbiórki jest wiążąca:** mechanizm zastępczy powstaje **przed** usunięciem
mechanizmu, który zastępuje. Inaczej istnieje okno, w którym granicy nie pilnuje nic.

**Gdzie inwestować w testy:** w to, co przeżyje kolejną wersję architektury. Framework okien przeżył
już trzy. Warstwa treści nie przeżyła żadnej i nie zasługuje dziś na gęste pokrycie.

**`TreatWarningsAsErrors`** przestaje być higieną i staje się częścią historii walidacji: skoro
kompilator jest walidatorem treści, jego ostrzeżenia są ostrzeżeniami o treści.

## 24. Pytania otwarte

1. **Format pliku wpisu.** JSON zawodzi dokładnie w jednym miejscu — długi tekst. Po wejściu slotów
   potwór traci dwa z trzech bloków prozy (akcje i cechy szczególne stają się dołączonymi wpisami),
   więc zostaje jeden długi tekst na plik — a wtedy naturalnym kształtem jest **front-matter plus
   treść**, ten sam, który przyjęto dla dokumentu. **Rozstrzygnąć po slotach, nie przed.** Nie robić
   w międzyczasie: tablicy napisów udającej akapity, prozy we front-matterze, własnego DSL-a.
2. **Czy `DataBlockShape` nadal zarabia na siebie.** Istnieje, żeby sprawdzać kształt i serializować
   zgodnie z nim. Rekord daje pierwsze od kompilatora, deserializator drugie od typu. Zostaje
   potrzebne wyłącznie „ten build nie zna tego bloku / tej wersji", co nie wymaga kształtu.
   **Wyzwalacz:** pierwsze prawdziwe narzędzie biurka. Nie ruszać wcześniej.
3. **Trzy własne implementacje zapisu atomowego.** Wydzielić jeden prymityw **zanim** powstanie
   trzeci użytkownik.
4. **Autorstwo treści w aplikacji.** Rejestr jest tylko do odczytu. Pytanie zmniejszyło się o połowę
   (typy treści pisze się w IDE, kompilator daje komunikaty), ale zostaje dla wpisów. Najmocniejszy
   motywator: MG ubiera goblina i nie ma jak zapisać tego jako czegoś wielokrotnego użytku.
   Obejście przez wpisy lokalne dla kampanii pozostaje odrzucone.
5. **Katalog pól formularza dokumentu i kształt markera.**
6. **Czy silnik formuł potrzebuje tablicy przeglądowej.** Modyfikator cechy to czysta arytmetyka, ale
   premia z biegłości i stopnie kości w Savage Worlds są tabelami. Tablica stała nie łamie zakazu
   gałęzi, ale poszerza język.
7. **Jakie jeszcze pola trafiają do nakładki poza stanem i slotami.** Reguła rozstrzygająca jest
   zapisana; brakuje przejścia przez realny system.

**Nawyk do przerwania.** Sześć rzeczy w repozytorium jest zbudowanych i nieużywanych: grupowanie
rejestru (z testami, bez konsumenta), `Category` / `Descriptor` rozwiązywane i niepokazywane,
`SelfDescribing` parsowane i nieczytane, `WarmPanelVisualStep`, `UnknownModule`, `Packs`
w manifeście. Dwie z nich po tej zmianie **umrą, zamiast doczekać konsumenta**. Zasada przeciw temu
jest już zapisana, ale wąsko, dla pól: *wożenie pola, dla którego świadomie nie przewidujemy
zastosowania, jest gorsze niż jego brak.* Rozszerzyć z pól na mechanizmy: **nic nie wchodzi bez
konsumenta w tym samym wycinku.**

## 25. Kolejność prac

| # | Krok | Dlaczego tu |
|---|---|---|
| 1 | Projekt `DungeonApp.Content.<x>`; `monster` i `gear` przeniesione do rekordów; `Core` dostaje typy przez wąski interfejs; rejestr wygląda identycznie | najmniejszy wycinek dowodzący tezy |
| 2 | Test granicy słownictwa rozszerzony na `Desktop`, słownik z zestawu | **mechanizm przed rozbiórką**, nigdy odwrotnie |
| 3 | Rozbiórka: `Template`, `CardElement`, `FieldValue`, dwuprzebiegowe rozwiązywanie, martwe testy | dopiero gdy 2 stoi |
| 4 | Zaprojektowana `MonsterCardView` — pierwsza prawdziwa karta | to jest cel całej operacji |
| 5 | Odrzucanie per plik wpisu; pozycje nierozwiązane widoczne w rejestrze | zamyka sekcję o paczkach |
| 6 | Jeden prymityw zapisu atomowego | **przed** magazynem instancji |
| 7 | Magazyn instancji + nakładki | pierwszy realny stan kampanii |
| 8 | Pierwsze prawdziwe narzędzie biurka; weryfikacja pytania 2 i usunięcie licznika | |
| 9 | Formuły, sloty, dokument | kolejność do ustalenia osobno |

**Kroki 1–5 są jednym spójnym przejściem i nie powinny być przerywane w środku** — między 3 a 4
aplikacja nie ma czym renderować karty.
