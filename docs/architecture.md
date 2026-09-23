# DungeonApp — architektura

**Status: projekt docelowy.** Ten dokument deklaruje, **jak ma być** — w czasie teraźniejszym,
z konsekwencjami, bez argumentów. Przy rozbieżności z kodem prawdą o zamiarze jest ten dokument,
a prawdą o kodzie jest [code-map.md](code-map.md). Nad jednym i drugim stoi [CLAUDE.md](../CLAUDE.md).

**Dlaczego tak, co odrzucono i co obowiązywało wcześniej** — [decisions.md](decisions.md),
w sekcjach o tych samych nazwach co tutaj. Czyta się je **przed** zaproponowaniem zmiany, nie po.
Do rozeznania się w projekcie nie są potrzebne.

**Jak czytać.** Część I tłumaczy bez żargonu, czym aplikacja jest i jak działa — to wystarczy,
żeby się w projekcie rozeznać, i na jej końcu można przestać. Część II deklaruje projekt: co
obowiązuje i co z tego wynika. Część III mówi, jak pilnuje się granic, co jest otwarte i w jakiej
kolejności powstaje reszta.

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
efekty się nie kumulują albo że ognista kula trafiła tych czterech goblinów, a tamtego nie.

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
poprawisz w paczce błąd w gobliniej statystyce, poprawka przechodzi przez kalkę do wszystkich
twoich kampanii, a twoje notatki zostają nietknięte. Zmiana treści działa jak patch balansujący
grę, nie jak coś, co psuje zapisane sesje.

## 4. Co program wie, a czego wiedzieć nie może

To jest sedno całego projektu.

Program **wie, czym jest potwór i jak go pokazać**. Jego karta jest zaprojektowana, nie poskładana
— linia nad cechami jest tam, gdzie ma być, sześć atrybutów stoi w rzędzie, akcje są oddzielone.
Bo to jest kwestia projektu graficznego i ktoś musi ją podjąć świadomie.

Program **nie wie, czym jest D&D**. Nie zna klasy pancerza, nie zna pojęcia poziomu.

Jak to możliwe naraz? Bo wiedza o potworze mieszka w **jednym, wyraźnie odgrodzonym pudełku**
z napisem „to jest do D&D". Reszta programu tego pudełka nigdy nie otwiera. Za pół roku grupa mówi
„gramy w Pathfindera" — robisz drugie pudełko, obok. Program się nie zmienia, bo nigdy nie
wiedział, że to pierwsze dotyczyło D&D. Pudełka stoją obok siebie, a ty przy starcie wybierasz,
z którego dziś grasz.

Wiedza o systemie nie jest zakazana, tylko **umiejscowiona**.

## 5. Jak się z tego korzysta

**Na starcie** wybierasz system — D&D, Pathfinder, cokolwiek jest wkompilowane w program. Od tej
chwili program wygląda tak, jak ten system go urządził: pasek boczny wypełnia się jego zakładkami.
Do wyboru systemu wracasz przyciskiem, bez zamykania programu.

**Między sesjami** przeglądasz treść systemu — potwory, przedmioty, zaklęcia — w zakładkach, które
system zaprojektował tak, jak ty tę treść widzisz, a nie tak, jak program ją trzyma. Jeśli któraś
treść jest zepsuta, widzisz ją jako zepsutą; nic nie znika po cichu. Dopisanie nowego przedmiotu to
nowy plik i zero czekania.

**Na stronie kampanii** włączasz warianty zasad, w które gra twoja grupa — złoto trzymane
w sakiewkach zamiast zapisanego na postaci, inny sposób liczenia udźwigu — każdy osobnym
przełącznikiem, pogrupowane w dodatki systemu. Wariant zmienia to, jak wygląda księga tej jednej
kampanii.

**Przy stole** otwierasz kampanię: półka zamienia się w stronę tej kampanii, a zakładki kampanii —
widoczne na pasku od wyboru systemu, ale dotąd zamknięte — stają się dostępne. Jedną z nich zwykle jest biurko
z pływającymi oknami — kolejka tur, drużyna, notatka, zegar świata, co potrzeba. Patrzysz na nie
kątem oka, robiąc coś innego. Obok mogą stać zakładki, w których się przebywa: **świat** (co
w kampanii właściwie żyje), **fabuła** (tekst przygody z odhaczanymi krokami — kilka grup może grać
tę samą przygodę, każda z własnymi odhaczeniami), **kronika** (co się wydarzyło). Które z nich są,
decyduje system.

Podział między oknem a zakładką ma jedno kryterium: **czy patrzysz na to obok innych rzeczy, czy
w tym przebywasz.**

**W trakcie gry** wyciągasz goblina z paczki i staje się *tym* goblinem — z własnym życiem,
własnym imieniem, własnym stanem. Trzy gobliny to trzy takie gesty. Wszystko, co potem na nim
zmieniasz, to twoje notatki na kalce.

## 6. Czego program nigdy nie zrobi — i co zrobi za ciebie

Nigdy nie zapyta „jeżeli". Nigdy nie będzie wiedział, ile coś trwa i kiedy się kończy. Nigdy nie
rozstrzygnie, czy dwie rzeczy się kumulują. Nigdy nie wybierze za ciebie, kogo coś dotyczy.
I nigdy nie zrobi czegoś dlatego, że stało się coś innego.

Za to **całą księgowość twojej decyzji zrobi jednym kliknięciem.** Postanawiasz, że drużyna
sprzedaje miecz kupcowi — miecz przechodzi do kupca, a złoto do sakiewki, którą wskazałeś.
Postanawiasz, że kula ognia trafiła te cztery gobliny — zaznaczasz je, a obrażenia schodzą ze
wszystkich naraz. Program nie wie, czy handel był uczciwy ani kto stał w obszarze. Wie, jak
zaksięgować to, co postanowiłeś.

**Kanoniczne brzmienie tych pięciu zakazów jest w [CLAUDE.md](../CLAUDE.md)** i to ono obowiązuje.
Deklaracje i test do stosowania przy nowych funkcjach są w sekcji *Granica automatyzacji*.

---

> **Tu można przestać**, jeśli chodziło o rozeznanie się w projekcie. Dalej zaczyna się projekt
> techniczny: te same rzeczy, zadeklarowane precyzyjnie.

---
---

# CZĘŚĆ II — PROJEKT

## 7. Słownik

| Pojęcie | Znaczenie |
|---|---|
| **rama** | Szkielet aplikacji: okno, pasek boczny i górny, stopka, ekran wyboru systemu, ustawienia; kampanie — tworzenie, wczytanie, usuwanie, zapis — i jedyna droga zmiany ich stanu. Nie wie, czym jest wpis. |
| **biblioteka** | Wspólny, neutralny kod, który wykonuje za systemy robotę wspólną dla wielu z nich, żeby żaden nie pisał jej od nowa — biblioteka w tym samym sensie co w programowaniu. **Bibliotek jest kilka**, każda o jednym temacie: biblioteka wpisów (paczki, wpisy, rejestr, instancje, nakładki, kontrolki kart, szkielet zakładki treści), biblioteka biurka (okna, ich układ, dolny panel), później silnik formuł. System bierze z nich dowolny podzbiór albo nic. Sama niczego nie robi; rama nie wie o jej istnieniu. |
| **system** | Skompilowany projekt wybierany przy starcie aplikacji: typy treści, ich widoki, zakładki, narzędzia biurka, dodatki. Jedyne miejsce w aplikacji, w którym wolno wiedzieć, czym jest potwór. |
| **dodatek** | Nagłówek na stronie kampanii grupujący warianty zasad zadeklarowane przez system. Sam niczego nie włącza. |
| **wariant** | Pojedyncza zmiana zasad z dodatku — jednej mechaniki albo kilku naraz — włączana osobnym przełącznikiem na stronie kampanii, z parametrami będącymi wartościami. Może dokładać i zastępować. |
| **strona kampanii** | Ekran ramy, w który zamienia się pozycja półki po otwarciu kampanii: to, co rama o kampanii wie, i przełączniki wariantów. |
| **zakładka** | Pozycja paska bocznego. Należy do jednej z trzech kategorii: Kampania, System, Aplikacja. |
| **typ treści** | Para: rekord opisujący wartości + zaprojektowany widok karty. Adresowany `system:id`. |
| **wpis** | Zarejestrowana treść z paczki: żelazny miecz, goblin, zaklęcie, efekt. Esencja — czym rzecz jest. Niezmienna, tylko do odczytu, adresowana `paczka:id`. |
| **dokument** | Drugi kształt treści: długi tekst z markerami deklarującymi pola interaktywne. Przygoda, scenariusz. Ma własny renderer, nie ma karty. |
| **instancja** | Egzemplarz żyjący w kampanii: referencja do wpisu lub dokumentu plus nakładka. |
| **nakładka** | Rzadka łatka nad wartościami wpisu — wyłącznie odchylenia. |
| **karta** | Zaprojektowany widok jednego typu treści. Nie jest składana z danych. |
| **kontrakt** | Interfejs publikowany przez konsumenta. Typ treści go implementuje albo nie. |
| **slot** | Pole, którego wartością jest lista referencji (we wpisie) lub lista instancji (w kampanii). |
| **narzędzie** | Backend okna na biurku, wnoszony przez system. |
| **panel / okno** | Pływające okno na biurku. Kontener; hostuje narzędzie. |
| **paczka** | Katalog z manifestem, niosący wpisy i dokumenty. Nie niesie typów treści. |
| **rejestr** | Co biblioteka wpisów wie o zainstalowanych paczkach po wczytaniu i zwalidowaniu. Wspólny, tylko do odczytu. |

## 8. Rama, biblioteka, system

| Część | Projekt | Charakter | Co wolno wiedzieć |
|---|---|---|---|
| **Rama** | `DungeonApp.Core` + `DungeonApp.Desktop` | kompilowana | okno, pasek boczny i górny, stopka, ekran wyboru systemu, ustawienia; kampanie, ich zapis, zdarzenia, jedyna droga zmiany stanu. **Nie zna `Entry`. Nie zna biurka.** |
| **Biblioteki** | biblioteka wpisów: `DungeonApp.Library.Entries` (logika, bez Avalonii) + `DungeonApp.Library.Entries.Desktop` (interfejs); biblioteka biurka: `DungeonApp.Library.Workspace` | kompilowana | biblioteka wpisów: paczki, rejestr, wpisy, instancje, nakładki, kontrolki kart, szkielet zakładki treści; biblioteka biurka: biurko i system okien; później silnik formuł. **Biblioteka wpisów zna `Entry`. Żadna nie zna `Monster`.** |
| **System** | `DungeonApp.Content.<x>` | kompilowana | typy treści, widoki kart, zakładki, narzędzia biurka, dodatki. **Jedyne miejsce, gdzie wolno być konkretnym.** |
| **Paczka** | `Dokumenty\DungeonApp\Packs\` | dane | wpisy i dokumenty. Zmienne, dodawane w trakcie sesji. |
| **Kampania** | `Dokumenty\DungeonApp\Campaigns\` | stan | modele stanu zadeklarowane przez system, jej system i włączone warianty z parametrami. |

```
        ┌──────────────────────────────────────────────┐
        │  SYSTEM               DungeonApp.Content.<x> │
        │  typy treści · karty · zakładki · dodatki    │
        │  ← jedyne miejsce, które zna D&D             │
        └──────────┬────────────────────────┬──────────┘
                   │ korzysta, jeśli chce   │ deklaruje zakładki,
                   ▼                        │ zapisuje stan
        ┌───────────────────────────┐       │
        │  BIBLIOTEKI               │       │
        │  wpisy: paczki · karty    │       │
        │  biurko: okna · układ     │       │
        └──────────┬────────────────┘       │
                   │                        │
        ┌──────────▼────────────────────────▼──────────┐
        │  RAMA                                        │
        │  okno · pasek boczny · wybór systemu         │
        │  kampanie · zapis · zdarzenia                │
        └──────────────────────────────────────────────┘

   paczka   ──wskazuje──►  typ treści w systemie
   kampania ──wskazuje──►  swój system i paczki, które sama zadeklarowała
```

- **Strzałki idą tylko w dół.** Rama nie wie, że istnieje jakakolwiek biblioteka albo system;
  biblioteka nie wie, że istnieje jakikolwiek system.
- **Biblioteki nie znają się nawzajem; składa je system.** Okno biurka pokazujące instancje to system
  łączący okno z biblioteki biurka z danymi z biblioteki wpisów. Zależność między bibliotekami jest
  wyjątkiem: jawnym, jednokierunkowym i tylko tam, gdzie jedna naprawdę potrzebuje drugiej w środku.
- **Systemy nigdy nie referencują się nawzajem.**
- **Ładowanie systemów jest statyczne** — referencją projektu, nigdy `Assembly.LoadFrom`.
- **System mówi ramie cztery rzeczy: kim jest, jakie ma zakładki, jakie modele stanu zapisuje
  i jakie kroki startowe zgłasza.** Rama nie zna jego typów treści ani sposobu rysowania kart.
  Kroki startowe uruchamia, nie wiedząc, co robią; tekst ostrzeżenia przy awarii kroku podaje
  system, a kroki samej ramy mają tekst ogólny.
- **Każdy system sam wczytuje paczki i ma własny rejestr.** Wspólnego rejestru ani wspólnego
  katalogu typów treści dla wszystkich systemów nie ma. Katalog paczek wskazuje systemowi korzeń
  kompozycji.
- **Biblioteka daje szkielet zakładki, system robi z niego swoją zakładkę.** Rama dostaje od systemu
  zakładkę i nie wie, czy powstała z biblioteki, czy od zera.
- **Jedna biblioteka może mieć dwa projekty** — logikę bez Avalonii i interfejs. Części jednej
  biblioteki mogą się znać; różne biblioteki nie.
- **Rama zachowuje drzwi zapisu.** System przejmuje wygląd i zawartość aplikacji, nigdy jedynej drogi
  zmiany stanu — sekcja *Gdzie mieszka stan*.
- **Dziś system jest jeden**, a ekran wyboru systemu istnieje mimo to. Drugi system powstaje
  w dniu, w którym naprawdę zmienia się gra, i jest wtedy równoległy, nie zależny.
- **Granica jest sprawdzalna mechanicznie**: rama i biblioteki nie referencują żadnego systemu, rama
  nie referencuje żadnej biblioteki, a źródła ramy i bibliotek nie zawierają słownictwa treści — sekcja
  *Granice mechaniczne*.

**Dlaczego →** [decisions.md](decisions.md), *Rama, biblioteka, system*.

### 8.1 Biblioteka, nie warstwa

- **Biblioteka jest wspólnym kodem, nie piętrem, przez które wszystko przechodzi.** Sama niczego nie
  robi: nie rejestruje się w ramie, nie wnosi zakładki, nie ma własnego cyklu życia.
- **Każda biblioteka ma jeden temat i wykonuje za systemy wspólną robotę** — wpisy, biurko, formuły —
  żeby ten sam kod nie powstawał w każdym systemie od nowa.
- **Biblioteka ma wobec ramy ten sam dostęp co system — ani więcej, ani mniej.** Te same kontrakty,
  ten sam stan kampanii tylko do odczytu, to samo jedyne wejście zmiany. Od systemu różni ją to,
  czego nie wie — żadnego systemu i niczego konkretnego — a nie to, co może. Nie ma drzwi, których
  system by nie miał; robi tylko za niego to, co każdy system musiałby napisać sam.
- **Biurko pojawia się na pasku bocznym wyłącznie wtedy, gdy system je tam postawi.**
- **System bierze z bibliotek, co chce** — którąkolwiek, kilka albo żadną.
- **Generyczny wpis mieszka w bibliotece wpisów, nie w ramie**: wpis, paczka, rejestr, instancja i nakładka
  są jednym wspólnym mechanizmem dla wszystkich systemów.
- **Silnik formuł jest drogą domyślną.** System, który z niego nie korzysta, liczy po
  swojemu — pod tymi samymi pięcioma zakazami, pilnowanymi wtedy wyłącznie przeglądem.

**Dlaczego →** [decisions.md](decisions.md), *Biblioteka, nie warstwa*.

### 8.2 Dodatki

Dodatek to nagłówek na stronie kampanii, pod którym stoją warianty zasad wbudowane w system — złoto
w sakiewkach zamiast zapisanego na postaci, inny sposób liczenia udźwigu. **Jednostką włączania jest
wariant, nie dodatek:** każdy wariant ma własny przełącznik, a nagłówek nie ma ani stanu, ani
przełącznika „wszystko".

**Forma należy do ramy, treść do systemu.** System deklaruje dodatki, ich warianty, wykluczenia
i parametry oraz dostarcza to, co wariant zmienia. Rama pokazuje przełączniki i parametry na stronie
kampanii, zapisuje wybór w manifeście, pilnuje wykluczeń i przekazuje wybór systemowi.

1. **Wariant może dokładać i zastępować** — pola, okna, zakładki, typy treści, formuły.
2. **System dowiaduje się o włączonych wariantach i ich parametrach w jednym miejscu: przy składaniu
   kampanii.** Rama przekazuje je wyłącznie tam — żaden kontekst zakładki ani okna ich nie niesie, więc
   pytanie „czy wariant jest włączony" jest niewykonalne, a nie tylko zabronione. Karta, okno
   i formuła są już takie, jakie mają być.
3. **Parametr jest wartością, nie wyborem zachowania.** Liczba wchodząca do rachunku — tak. Wybór
   między dwoma zachowaniami to dwa wykluczające się warianty.
4. **Warianty zastępujące tę samą rzecz system oznacza jako wykluczające się**; rama nie pozwala
   włączyć obu. To walidacja ustawień kampanii, nie reguła gry.
5. **Zmiana wariantu w otwartej kampanii składa ją od nowa** — rama zamyka ją i otwiera ponownie.
6. **Wyłączenie wariantu ukrywa jego dane, nigdy ich nie kasuje.**
7. **Kod wariantu podlega pięciu zakazom** jak każdy inny.

**Kryterium:** jeśli wariant potrzebuje przełącznika w środku logiki, to zwykle znak, że ta logika
wykonuje regułę, którą powinien wykonać Mistrz Gry.

**Mechanizm powstaje z pierwszym prawdziwym dodatkiem**, nie wcześniej.

**Dlaczego →** [decisions.md](decisions.md), *Dodatki*.

## 9. Gdzie biegnie linia między kodem a danymi

**Kształt jest kodem, wartości są danymi.**

- **Literówka w nazwie pola jest błędem kompilacji**, z podaną linią i podpowiedzią — nie odrzuconą
  paczką przy starcie.
- **Kompilator jest walidatorem treści.**

**Dlaczego →** [decisions.md](decisions.md), *Gdzie biegnie linia między kodem a danymi*.

## 10. Deklaracja treści

### 10.1 Typ treści to para: rekord + widok

```
Monster            rekord: Size, Type, Alignment, Ac, AcSource?, Hp, …, Description
MonsterCardView    zaprojektowany XAML — układ statbloku
```

Rekord deklaruje pola: nazwy, typy, opcjonalność. Widok deklaruje układ. **Żadne z dwojga nie
pochodzi z danych.**

* **Nowy rodzaj treści = nowy kod:** rekord i widok.
* **Statblok jest zaprojektowany, nie składany.**
* **Liczba widoków rośnie liniowo** z liczbą rodzajów razy liczbę systemów — realistycznie 6–8 na
  system.
* **Widok domyślny nie powstaje** przed pierwszym rodzajem treści, którego nie chce się
  zaprojektować.

### 10.2 Kontrolki, nie katalog

Lista cech, blok prozy, lista pozycji, pasek zasobu, akcja rzutu, znacznik binarny są
**kontrolkami wielokrotnego użytku**, komponowanymi przez projektanta karty. Dane nie wybierają
kontrolki i nie niosą parametrów układu — zaprojektowany widok swój układ po prostu ma.

### 10.3 Droga treści: z dysku na ekran

```
 goblin.json                             ← plik, który piszesz ręcznie
     │
     │  loader biblioteki wpisów sprawdza tożsamość, adres, wskazanie typu
     ▼
 Entry { id, nazwa, wskazanie typu, nierozpakowane wartości }
     │                                   ← tu biblioteka wpisów staje.
     │                                     Nie zagląda do wartości.
     │  system rozpakowuje wartości w swój rekord
     ▼
 Monster { Ac = 15, Hp = 7, Speed = "30 stóp", … }
     │                                   ← typowane; kompilator już to sprawdził
     │  biblioteka wpisów dobiera widok
     ▼
 MonsterCardView                         ← zaprojektowany układ statbloku
```

Plik wpisu:

```json
{ "id": "goblin", "name": "Goblin", "template": "dnd5e:monster", "values": { "ac": 15, … } }
```

* **`values` deserializuje się wprost w rekord**, ze ścisłym traktowaniem nieznanych kluczy. Nie ma
  własnego walidatora wartości — robi to deserializator.
* **Biblioteka wpisów niesie kopertę, system otwiera list.** Biblioteka wie, że coś przyszło, skąd, pod
  jakim adresem i do jakiego typu się odwołuje — i nic więcej. Odrzucanie treści, rejestr
  i oznaczanie tego, co się nie rozwiązało, istnieją w jednym miejscu i działają tak samo dla
  każdego systemu.
* **Format pliku jest wyborem serializatora**, nie decyzją architektoniczną — podmiana dotyka jednej
  klasy. Sekcja *Pytania otwarte*, pytanie o format.
* **Konwencja nazewnicza:** identyfikatory po angielsku, etykiety i treść po polsku. Identyfikator
  typu treści ma wartość `monster`, a etykiety i komunikaty dla Mistrza Gry są po polsku.

**Dlaczego →** [decisions.md](decisions.md), *Deklaracja treści*.

## 11. Wpis, dokument, instancja, nakładka

> **Typ treści deklaruje kształt. Wpis deklaruje wartości. Instancja deklaruje odchylenia.**

Wpis jest esencją, instancja egzemplarzem istniejącym w świecie — jak **item** i **item stack**.
Dotyczy każdego wpisu, nie tylko przedmiotów: obrażenia potwora są własnością egzemplarza, nie
gatunku.

| Pytanie | Odpowiada | Przykład |
|---|---|---|
| Jaki *kształt* może mieć rzecz? | typ treści | „broń ma wagę, ilość i slot `zaklęcia`" |
| Jakie *wartości* ma konkretna rzecz? | wpis | „diamentowy miecz waży 3"; „goblin nosi tasak" |
| Co *odbiega* w tym egzemplarzu? | instancja | „ten ma Ostrość V i nazywa się Zgubą" |

**Waga jest we wpisie, ilość jest w nakładce.**

**Nakładka nie modeluje wariantu rzeczy.** Miecz +1 to osobny wpis, nie miecz z nadpisanym polem.
Kryterium: **czy chcesz tego użyć ponownie.** To reguła projektowa, nie blokada techniczna.

### 11.1 Nakładka jest rzadką łatką nad wartościami wpisu

**Instancja nie materializuje wpisu.**

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
* **Poprawka w paczce przechodzi do istniejących kampanii.** Klucza, którego nie ma w łatce, bierze
  się z aktualnej treści — więc wartość może zmienić się pod Mistrzem Gry przy aktualizacji
  w obrębie wersji major.
* **Notatki MG nigdy nie są po cichu nadpisane.** Klucz łatki, który zniknął z rekordu, oznacza
  instancję jako wymagającą uwagi — nigdy nie odrzuca kampanii i nigdy nie kasuje łatki.

### 11.2 Sloty i zagnieżdżanie

**Slot jest polem, nie pojemnikiem instancji.** We wpisie jest listą **referencji**, w kampanii
listą **instancji** — to różne typy, więc slot nie przechodzi przez scalanie opisane wyżej i ma
własną ścieżkę. To drugi rodzaj pola, nie wyjątek.

Zaklinanie nie wymaga nowego mechanizmu: biblioteka publikuje kontrakt wkładu, typ treści deklaruje
slot (czyli decyduje, że miecz da się zaklinać), a paczka dostarcza Ostrość V jako zwykły wpis
z własną kartą. Miecz jest pojemnikiem na zaklęcia tak samo, jak plecak na miecze.

* **Biblioteka wpisów nie ma zdania o tym, co wpis powinien mieć w slocie.** Decyduje autor treści.
* **Zawartość początkowa slotu wskazuje wyłącznie wpisy z tej samej paczki.** Instancja rozwiązuje
  referencje wobec wszystkich paczek kampanii.
* **Gdy instancja zmieni zawartość slotu, przejmuje całą listę**, nie różnicę.
* **Pozycje slotu same są instancjami** — stąd zagnieżdżenie: kampania → bohater → stos → zaklęcie.
* **Trwałość idzie za własnością.** Instancja kampanii dostaje własny plik; zagnieżdżona jest
  zapisywana wewnątrz właściciela.
* **Cykl w grafie jest dozwolony** (torba w torbie); chroni twardy limit głębokości zagnieżdżenia.

**Referencja nierozwiązana, nie martwa.** Gdy brakuje paczki, wpisu albo wpis zmienił się
niekompatybilnie, instancja zostaje, jest jawnie oznaczona, jej nakładka nigdy nie jest po cichu
nadpisana ani skasowana, a kampania otwiera się dalej w trybie ograniczonym. Zainstalowanie
brakującej paczki przywraca wszystko bez utraty stanu.

### 11.3 Dokument

Dokument jest drugim kształtem treści, ale **nie drugim magazynem instancji**.

| | wpis | dokument |
|---|---|---|
| co jest w paczce | wartości | tekst z markerami |
| skąd przestrzeń kluczy | rekord (kompilacja) | markery (wczytanie) |
| jak się renderuje | zaprojektowana karta | renderer dokumentu |
| instancja | rzadka łatka | **rzadka łatka, identyczna w kształcie** |

Wiele grup grających tę samą przygodę to wiele instancji jednego dokumentu. Poprawka literówki
w tekście nie kasuje odhaczeń.

Cztery warunki wiążące:

1. **Podzbiór markdowna jest zamknięty i nie zawiera układu.** Nagłówki, akapity, listy, cytaty,
   wyróżnienia, tabele — tak; surowy HTML, szerokości, kolumny, kolory, wymiarowanie obrazków — nie.
   Passthrough HTML jest wyłączony.
2. **Marker jest walidowalny przy wczytaniu** — jednoznacznie ogranicznikowany, z własnym id.
   Dokument z markerem zniekształconym albo z id powtórzonym jest odrzucany.
3. **Markery znajduje się w drzewie, nie regeksem po pliku.**
4. **Pola zadeklarowane przez dokument są prywatne dla dokumentu.** Pole, które ma przeczytać
   formuła albo inne okno, musi być zadeklarowane w typie treści.

**Dokument nie ma karty — ma własny renderer. Karta jest układem, dokument jest sekwencją.**

**Dlaczego →** [decisions.md](decisions.md), *Wpis, dokument, instancja, nakładka*.

## 12. Kontrakty są interfejsami

**Wpis nie ma jednej reprezentacji.** Ten sam żelazny miecz pojawia się jako karta, jako wiersz
w ekwipunku (nazwa, waga, skrót obrażeń) i jako uczestnik kolejki tur.

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
   Creature  ──implementuje──►  ✓  (z zupełnie innego systemu)
   Gear      ─── nie ────────►  ✗  nigdy się na niej nie pojawi
```

* **Kwalifikacja jest systemem typów.** Przedmiot nigdy nie pojawia się na liście do wyboru
  w kolejce tur — odrzucenie jest fizyczne, nie proceduralne.
* **Pola kontraktu są typowane per pole.** `KluczSortowania` jest liczbą, bo tak jest zadeklarowany.
* **Projektowanie nowego narzędzia nie wymaga projektowania danych.** Narzędzie przychodzi
  z gotowym wyglądem i publikuje interfejs; treść tylko go implementuje.

**Reguła umiejscowienia narzędzia:**

> Narzędzie czytające pole po nazwie mieszka w systemie, który to pole deklaruje.
> Narzędzie czytające interfejs jest neutralne i mieszka w bibliotece.

Wybór między jednym a drugim jest wyborem katalogu, w którym leży plik — **nigdy gałęzią w kodzie**.
Nigdzie nie ma rozgałęzienia „jeśli system to D&D".

> **Narzędzie nigdy nie introspekcjonuje typu treści.** Deklaruje interfejs i dostaje obiekty.

**Dlaczego →** [decisions.md](decisions.md), *Kontrakty są interfejsami*.

## 13. Paczki, wczytywanie, bezpieczeństwo

Paczka to katalog z manifestem, niosący wpisy i dokumenty. Nie niesie typów treści.

* **Paczka daje** przestrzeń nazw dla id, jednostkę instalacji i deinstalacji oraz wersję, którą
  kampania deklaruje.
* **Zainstalowanie nowej paczki nie zmienia po cichu otwartej kampanii** — kampania rozwiązuje
  referencje wyłącznie wobec paczek, które sama deklaruje.
* **Wszystko, co paczka wnosi, jest homebrew z definicji**, niezależnie od tego, czy stoi
  w oficjalnym podręczniku. Aplikacja nie zna pojęcia autorytetu.
* **Jedna przestrzeń nazw na paczkę.** Id wpisu i id dokumentu nie mogą kolidować w obrębie jednej
  paczki.

### 13.1 Co się dzieje, gdy treść jest zepsuta

Zasada nadrzędna: **wadliwa treść zostaje widoczna i oznaczona. Nigdy nie znika po cichu.**

| Co jest zepsute | Skutek |
|---|---|
| manifest paczki | **cała paczka odrzucona** — tożsamość nieznana, nie ma czym adresować zawartości |
| jeden plik wpisu lub dokumentu | **tylko ta pozycja oznaczona**, reszta paczki działa |
| dwie pozycje o tym samym id | **obie oznaczone** — żadna nie wygrywa po cichu |
| brakuje paczki, do której odwołuje się kampania | instancje oznaczone, notatki nietknięte, kampania otwiera się dalej |

**Zakładki treści systemu są jedynym kanałem, którym Mistrz Gry się o tym dowiaduje** — ich projekt
bez miejsca na rzeczy zepsute jest niekompletny **funkcjonalnie**, nie kosmetycznie. Gdzie pokazuje
się paczka odrzucona, której systemu nie da się poznać — sekcja *Pytania otwarte*.

### 13.2 Model bezpieczeństwa

Paczka to **czyste dane**. Cały model bezpieczeństwa to walidacja przy wczytaniu plus limity
rozmiarowe:

* Wadliwa treść jest wykrywana **przy starcie**, nigdy przy pierwszym kliknięciu w środku sesji.
* Formuła jest walidowana nie tylko składniowo, ale i wobec pól, które realnie istnieją.
* Identyfikatory podlegają ograniczeniom znakowym. Żadna ścieżka na dysku nie jest budowana
  z danych — tożsamość paczki mieszka w jej manifeście, nie w nazwie katalogu.
* **Limity:** rozmiar paczki, głębokość zagnieżdżenia wyrażenia, głębokość zagnieżdżenia instancji,
  liczba i rozmiar kości w jednym rzucie, limit eskalacji eksplozji.

**Dlaczego →** [decisions.md](decisions.md), *Paczki, wczytywanie, bezpieczeństwo*.

## 14. Granica automatyzacji

> **Aplikacja sumuje. MG decyduje o składnikach.**
>
> **Aplikacja księguje decyzje MG — także jednym kliknięciem i na kilku rzeczach naraz. Nie
> podejmuje ich za niego.**

KP z pancerza: MG założył ten pancerz, pancerz sam o sobie mówi, ile daje, aplikacja dodała liczby.
Efekt „+2 do KP": MG dopisał go do listy, aplikacja go zsumowała. Oba są księgowością — **pod
warunkiem**, że aplikacja nie wie, czym jest Tarcza Wiary, kiedy się zaczyna, kiedy wygasa, czy się
kumuluje i na kogo działa.

**Pięć zakazów — kanoniczne brzmienie w [CLAUDE.md](../CLAUDE.md).** Jeśli zapragniemy którejkolwiek
z tych pięciu rzeczy, nie zbliżamy się do granicy — przekraczamy ją.

Zakazy nie mówią, że logiki nie ma. Formuły, sumowanie wkładów i wartości pochodne są logiką.
Niepotrzebne jest **wykonywanie reguł**.

### 14.1 Runda, zegar świata i kronika

> Narzędziu wolno trzymać numer rundy i czas świata. **Nic poza widokiem nie ma prawa ich
> odczytać.** Licznik jest wyświetlany i przesuwany przez MG; nie jest wejściem żadnej formuły ani
> warunkiem żadnego wygaśnięcia.

> Wpis do kroniki jest **częścią operacji**, nie reakcją na nią. Jedna zmiana oddana wejściu zmiany
> niesie stan i wpis kroniki; rama zapisuje je w jednym zatwierdzeniu.

**Zakaz piąty egzekwuje mechanicznie wejście zmiany**: odmawia wywołania w trakcie rozsyłania
powiadomień — sekcja *Gdzie mieszka stan*. Limitu liczby zdarzeń ani rzeczy w jednej zmianie nie ma.

### 14.2 Efekt jest wkładem, nie mechanizmem

Efekt to zwykły wpis: ma typ treści, kartę, może mieć prozę. Leży na instancji jako pozycja slotu —
tak samo jak przedmiot w plecaku. Implementuje **kontrakt wkładu**: nazwa pola plus wartość.

**Pancerz i efekt są dla aplikacji tym samym.** Oba deklarują wkład do pola `kp`. Nie ma „systemu
efektów" — są wkłady. Formuła KP brzmi „baza plus suma wkładów do `kp`" i nie wie, skąd te wkłady
przyszły ani czym są.

### 14.3 Test asystenta

Do zastosowania przy każdej przyszłej funkcji:

1. Czy MG jawnie umieścił **każdy** składnik tego wyniku?
2. Czy wynik pozostanie niezmieniony, dopóki MG czegoś nie ruszy?
3. Czy aplikacja liczy, **nie wiedząc, czym** są składniki?

Trzy razy „tak" → księgowość, wolno automatyzować. Choć raz „nie" → aplikacja co najwyżej proponuje,
a nanosi MG.

### 14.4 Wyliczenie jest propozycją, akcja jest zapisem

Dwa rodzaje wyniku rozchodzą się po jednej linii: **kto go wywołał.**

* **To, co aplikacja wylicza sama** — wartość pochodna, rzut, podpowiedź ceny — jest
  **propozycją**. Narzędzie liczy i pokazuje; MG decyduje, czy i gdzie to nanieść. Dane kampanii są
  formularzem: każde pole instancji jest edytowalne, łącznie z wartością pochodną.
* **Akcja wywołana przez MG** — „sprzedaj", „zadaj obrażenia", „przełóż do skrzyni" — **zapisuje od
  razu i nie pyta o potwierdzenie**. Warunek: wszystko, co akcja zmieni, MG widzi i ustawia przed
  kliknięciem.

W projekcie nie ma kaskad automatycznych zmian stanu, dialogów potwierdzeń ani cofania zmian
wywołanych regułą.

### 14.5 Księgowanie decyzji na kilku rzeczach naraz

Jedna akcja MG może zmienić kilka rzeczy, jeśli każdą z nich MG wskazał.

* **Sprzedaż.** MG otwiera handel z kupcem, wybiera przedmiot, klika „sprzedaj" — przedmiot
  przechodzi z ekwipunku gracza do kupca, a złoto do sakiewki, którą MG wskazał (albo raz oznaczył
  jako domyślną).
* **Kula ognia.** MG zaznacza cztery gobliny, wpisuje obrażenia, tym, którzy się obronili, ustawia
  połowę — i klika raz.

**Wszystko, co operacja zmieni, MG widzi przed kliknięciem.** Ukryty skutek — sprzedaż, która przy
okazji po cichu podnosi reputację u kupca — jest zabroniony.

**Gdzie granica nadal obowiązuje — w tym samym sklepie:**

| Pokusa | Co łamie | Co zamiast |
|---|---|---|
| kupiec uzupełnia towar po tygodniu | czas | MG dopisuje towar |
| cena zależy od testu Charyzmy | warunek | aplikacja proponuje cenę z danych przedmiotu — także „połowa wartości", bo to arytmetyka — a MG poprawia ją przed kliknięciem |
| transakcja odrzucona, bo gracz nie ma dość złota | egzekwowanie reguły | aplikacja pokazuje brak, nie blokuje |
| złoto trafia do „najbliższej sakiewki" | cel wybrany regułą | MG wskazuje sakiewkę |
| aplikacja rozstrzyga, kto stał w obszarze kuli | cel wybrany regułą | MG zaznacza trafionych |

**Dlaczego →** [decisions.md](decisions.md), *Granica automatyzacji*.

## 15. Niezmiennik interfejsu

> **Widok zna pełny kształt tego, co wyświetla — w czasie kompilacji. Z danych przychodzą wyłącznie
> wartości.**

Widok, który czyta skądkolwiek, jakie ma pola, jest naruszeniem — sprawdzalnym z samego diffu.

Jedyne miejsce, w którym sekwencja pochodzi z treści, to **dokument** — bo dokument *jest*
sekwencją. Karta jest układem.

**Dlaczego →** [decisions.md](decisions.md), *Niezmiennik interfejsu*.

## 16. Poziomy logiki i formuły

Dwa poziomy, nie trzy:

1. **Czyste dane** — pole wskazuje wartość. Zero logiki.
2. **Deklaratywna formuła** — jeden współdzielony silnik, mieszkający w bibliotece, za każdym polem
   obliczanym. Bez pętli, bez gałęzi, bez efektów ubocznych: bezpieczna z definicji, wymaga
   walidacji, nie piaskownicy. **Domyślna i jedyna ścieżka.**

Poziomu skryptowego nie ma. `min` i `max` są arytmetyką i są dozwolone; limit, którego pilnują,
deklaruje konkretny przedmiot o sobie samym.

**Dwa konteksty ewaluacji:**

* **Wartość pochodna** — deterministyczna funkcja danych, przeliczana przy każdym odczycie
  (modyfikator cechy, maksimum zasobu, KP, klucz sortowania). **Nie może zawierać kości.**
* **Rzut** — wykonywany na żądanie; wynik jest zdarzeniem pokazywanym MG, nie wartością odczytywalną
  z danych. Kości dozwolone.

**Zasięg: jeden skok, nieprzechodni.** Formuła czyta własną instancję oraz pola tego, do czego ta
instancja odwołuje się bezpośrednio — pozycje slotów — i nie idzie dalej. Odczytane przez skok pole
nie może samo być wynikiem skoku. Pokrywa to KP z pancerza i efektów oraz obrażenia z broni; cykle są
niemożliwe strukturalnie — bez wykrywania cykli, bez kolejności przeliczania, bez inwalidacji
zależności.

**Eksplodujące kości** są własnością notacji kości z twardym limitem eskalacji, a nie pętlą pisaną
przez autora treści.

**Dlaczego →** [decisions.md](decisions.md), *Poziomy logiki i formuły*.

## 17. Gdzie mieszka stan

| Co | Gdzie | Charakter |
|---|---|---|
| Paczki | `Dokumenty\DungeonApp\Packs\<paczka>\` | Instalowane, tylko do odczytu, wspólne. Dokument użytkownika — widoczny i kopiowalny. |
| Kampanie | `Dokumenty\DungeonApp\Campaigns\<id>\` | Manifest — w nim system kampanii oraz włączone warianty z parametrami — oraz modele stanu zadeklarowane przez system. |
| Układy biurka | `%LocalAppData%\DungeonApp\layouts\` | Stan aplikacji, nie dokument. |

**Kampania jest dokumentem, układ okien jest ustawieniem programu.** Kampania leży w Dokumentach —
widoczna, kopiowalna, przenoszalna na pendrivie — a układ biurka w danych aplikacji.

**Rama zapisuje, system deklaruje.** System — albo biblioteka, z której korzysta — deklaruje modele
stanu, które kampania trzyma; rama zapisuje je wszystkie w jednym zatwierdzeniu i jest jedyną drogą
ich zmiany. Model to rekord z `required`, deserializator jako jedyny walidator, identyfikator, numer
wersji. Niezgodna wersja modelu oznacza, nie migruje. Instancje z nakładkami są pierwszym takim
modelem — dostarcza go biblioteka wpisów.

**Rama jest właścicielem jedynej drogi zmiany stanu i powiadomień o zmianie. System nigdy nie
dostaje drzwi zapisu.** Kształt tej drogi ma czynić czwarty i piąty zakaz **niewykonalnymi**,
a nie tylko zabronionymi.

**Stanu nie zmienia się w miejscu.** Model stanu jest niezmienny; zmiana to oddanie ramie nowych
wersji konkretnych rzeczy — jednej albo kilku, razem z tworzonymi i usuwanymi — przez jedyne wejście
zmiany. Zmienia się dokładnie to, co oddano, i nic poza tym: czwarty zakaz. Powiadomienie o zmianie
wychodzi po zapisie i niesie wyłącznie odczyt, a wejście zmiany odmawia w trakcie innej zmiany
i w trakcie rozsyłania powiadomień: piąty zakaz. Zdolność do zapisu jest oznaczeniem typu, nie
wspólnym przodkiem z logiką. **Każda zatwierdzona zmiana trafia na dysk od razu.**

**Kampania należy do jednego systemu.** Kampania, której systemu nie ma w programie, jest widoczna
jako niedostępna — nie znika.

Magazyn kampanii i magazyn układu biurka zapisują tak samo: plik tymczasowy, atomowe przeniesienie,
licznik generacji wykrywający zapis przerwany w połowie — jeden prymityw zapisu atomowego, z którego
korzystają oba.

**Dlaczego →** [decisions.md](decisions.md), *Gdzie mieszka stan*.

## 18. Nawigacja: ekran wyboru systemu i pasek boczny

**Aplikacja startuje na ekranie wyboru systemu** — pełnoekranowym, z systemami wkompilowanymi
w program. Istnieje także przy jednym systemie. **Wybór systemu poprzedza kampanię**, a nie z niej
wynika.

**Powrót do wyboru** — przycisk „Zmień system" w pasku górnym, należący do ramy — działa bez restartu. Rama umie
w całości rozebrać aktywny system: zamknąć kampanię, zwolnić jego zakładki i biurko. Nic się przy tym
nie traci, bo każda zmiana stanu trafia na dysk od razu. **Sprzątanie po sobie przy powrocie jest
częścią kontraktu zakładki i okna.**

### 18.1 Pasek boczny: trzy kategorie

| Kategoria | Kto ją wypełnia | Kiedy istnieje | Co zakładka dostaje od ramy |
|---|---|---|---|
| **Kampania** | rama (pozycja kampanii) + system (zakładki kampanii) | od wyboru systemu; zakładki systemu zamknięte, dopóki żadna kampania nie jest otwarta | stan kampanii i drogę zapisu |
| **System** | system | od wyboru systemu | wyłącznie treść systemu — **kampanii nie widzi wcale** |
| **Aplikacja** | rama | zawsze | ustawienia i inne rzeczy ramy |

Nazwy kategorii są słownikiem dokumentów; etykiety na ekranie ustala projekt interfejsu. **Uwaga
na zderzenie nazw:** na ekranie kategoria System ma etykietę „Biblioteka", a kategoria Aplikacja —
„System".

* **Kategoria wyznacza nie tylko miejsce zakładki, ale to, co zakładka dostaje.** Zakładka kategorii
  System nie widzi otwartej kampanii. Co potrzebuje stanu kampanii, należy do kategorii Kampania.
* **System wypełnia pasek wedle zasad ramy, nie rysuje go.** Deklaruje zakładki kategorii Kampania —
  pod pozycją kampanii — i kategorii System; rama je wyświetla. Pozycja kampanii i kategoria
  Aplikacja należą do ramy.
* **Kategoria Aplikacja niesie zakładkę „Ustawienia".** Pusta, dopóki rama nie ma czego ustawiać.
* **Pasek boczny niesie zakładki; akcje ramy, które niczego nie otwierają, stoją w pasku górnym.**
  Pasek górny należy do ramy i stoi nad obszarem treści, obok paska bocznego, od wyboru systemu.
  Z lewej pokazuje nazwę systemu i otwartej kampanii — tekst, nie nawigację; z prawej przyciski akcji
  ramy, dziś jeden: „Zmień system".
* **Pozycja kampanii zmienia się razem ze stanem.** Bez otwartej kampanii jest półką — wczytanie,
  tworzenie, usuwanie. Po otwarciu kampanii zamienia się w **stronę kampanii** i nosi jej nazwę: to,
  co rama o kampanii wie, i przełączniki wariantów. Ani biblioteka, ani system nic na niej nie
  stawiają. **Kampanię zamyka się z wnętrza strony kampanii**, nie z paska — zamknięcie przywraca
  półkę.
* **Po otwarciu kampanii pokazana jest strona kampanii**, nie którakolwiek zakładka systemu — rama
  nie wybiera za system jego „pierwszej" zakładki.
* **Zakładki kampanii są na pasku od wyboru systemu; bez otwartej kampanii są zamknięte** —
  wyszarzone, z kłódką zamiast ikony, nieklikalne. Blokada należy do ramy. System deklaruje
  zakładki kampanii raz, przy wyborze systemu; ich zawartość powstaje dla konkretnej otwartej
  kampanii.
* **Zakładki są kompilowane i policzalne w czasie budowania** — deklaruje je skompilowany system,
  nigdy paczka. Literówka w pliku treści nie ma jak zepsuć paska.
* **Ile zakładek wnosi system i jak je grupuje, jest decyzją jego projektanta**, nie architektury.

### 18.2 Okno czy zakładka — kryterium

> Czy patrzy się na to kątem oka obok innych rzeczy, czy się w tym przebywa?

**Peryferyjne i równoczesne → okno biurka.** Kolejka tur, drużyna, zegar świata, kostki, notatka,
ekwipunek.

**Centralne i wyłączne → zakładka.** Długi tekst, w którym się czyta i scrolluje.

Kryterium stosuje projektant systemu. Typowe zakładki kampanii:

* **Biurko** — okna narzędzi. Stan świata oglądany kątem oka, w wielu rzeczach naraz.
* **Świat** — przegląd instancji tej kampanii, lista plus karta.
* **Fabuła** — dokumenty przygody.
* **Kronika** — pełna historia zmian. Jednocześnie okno (ogon ostatnich zdarzeń) i zakładka
  (całość) — dwie długości tego samego.

Biblioteka dostarcza do nich klocki; które z nich są, decyduje system.

**Karta nie jest oknem** — jest widokiem szczegółowym, który pojawia się w zakładce treści po
kliknięciu pozycji i który narzędzie może pokazać.

### 18.3 Zakładki treści

Treść systemu przegląda się w zakładkach kategorii System — np. osobno przedmioty, potwory,
zaklęcia — zaprojektowanych przez system. Biblioteka wpisów daje do nich wspólny widok listy z kartą i trzy
filtry: **po kategorii** (właściwość typu treści), **po paczce** i **po typie treści**. Źródłem
wszystkich jest rejestr.

* **Szkielet zakładki treści należy do biblioteki wpisów, system go wypełnia.** Biblioteka wpisów — jedna z bibliotek — jest właścicielem
  wpisów i całego ekranu wokół nich: lista, wyszukiwanie po nazwie, sortowanie, filtry, tagi, odznaka
  wiersza, widok szczegółu po wybraniu wpisu. System tworzy z tego szkieletu zakładkę i stawia ją na
  pasku — sama biblioteka wpisów żadnej nie wnosi. Dla każdego swojego typu treści system podaje, **co**
  wstawić w te miejsca: które wartości są tagami, po czym się filtruje i sortuje, co stoi w odznace
  wiersza, oraz zaprojektowaną kartę szczegółu. Podaje to skompilowanym kodem czytającym własny
  rekord, nie ścieżką do pola w danych paczki. Miejsca są zamkniętym zestawem zaprojektowanym przez
  bibliotekę wpisów; system je wypełnia, nie dokłada nowych.
* **Grupowanie z danych jest legalne wewnątrz skompilowanego ekranu, nielegalne w nawigacji.**
  Sprawdzian po skutku awarii: literówka psuje **etykietę filtra**, a nie nawigację.
* **Zakładka treści nie jest miejscem wewnątrz kampanii.** Wybór wpisu w kampanii jest **momentem,
  nie miejscem**: przywoływanym z narzędzia, filtrowanym do paczek kampanii, znikającym po wyborze.
* **Tworzenie, edycja i usuwanie treści w zakładkach treści są poza pierwszą wersją.**

**Dlaczego →** [decisions.md](decisions.md), *Nawigacja: ekran wyboru systemu i pasek boczny*.

## 19. Narzędzia biurka i system okien

`PanelGeometry`, `WorkspaceSurface`, `WorkspaceLayoutStore`, `PanelWindow`, `PanelDeck`, rozdział
„desired" / „effective", debounce zapisu układu — **bez zmian wizualnych i bez zmian w geometrii.**
Okno hostuje **narzędzie biurka**, nie kartę.

* **System okien mieszka w bibliotece biurka.** Biurko jest jedną z zakładek, które system może postawić
  w kategorii Kampania — bez pisania od nowa okien, ich przesuwania i zamykania.
* **Katalog okien biurka jest tym, co deklaruje system** wraz z włączonymi wariantami, składanym przy
  otwarciu kampanii. Nic w ramie ani w bibliotece nie nazywa żadnego systemu po imieniu.
* **System wnosi okno jako gotową kontrolkę i sam składa je z tego, czego potrzebuje.** Biblioteka
  biurka daje okno, jego miejsce i układ — nie dane; nie wie, że wpisy istnieją. Instancje, rejestr
  i rozwiązywanie wskazań system bierze z biblioteki wpisów, a stan kampanii i jedyne drzwi zapisu —
  z ramy, przez kontekst zakładki kampanii. Nie sesję i nie samą kampanię — nic więcej osiągalnego
  stamtąd. Żaden szablon w bibliotece nie zna typu z systemu.
* **Okno może udostępniać widżety do gniazd dolnego panelu biurka** — miniatury pokazujące pojedynczą
  daną, jak czas w fikcji ([mockup](images/mockup_biurko_nowe.png)). Widżet jest częścią deklaracji
  okna, nie osobnym katalogiem, nie ma własnego stanu i czyta stan kampanii wyłącznie do odczytu —
  także wtedy, gdy jego okno jest schowane albo zamknięte. Zmiana idzie przez okno i tę samą jedyną
  drogę zapisu. Kierunek na później; powstaje z pierwszym widżetem.

**Dlaczego →** [decisions.md](decisions.md), *Narzędzia biurka i system okien*.

## 20. Wersjonowanie

Dwie niezależne osie.

1. **Wersja typu treści.** Wpis ją deklaruje i tą ścieżką migruje. Zmiana addytywna (nowe opcjonalne
   pole) nie wymaga bumpa; zmiana łamiąca wymaga, a wpis deklarujący starszą wersję jest
   **oznaczony, nie wczytany na oślep**.
2. **Wersja paczki.** Kampania deklaruje `{ id, major }`. Minor rozwiązuje się normalnie; major
   zostawia referencję nierozwiązaną, dopóki kampania świadomie nie zaktualizuje deklaracji.
   **Nigdy cicha podmiana treści pod otwartą kampanią.**

**Migracji nie budujemy**, dopóki jakaś wersja nie zostanie faktycznie podniesiona — niezgodna wersja
**oznacza** pozycję, a nie uruchamia migrację.

**Dlaczego →** [decisions.md](decisions.md), *Wersjonowanie*.

## 21. Przepływy

### 21.1 Start aplikacji

```
 1.  systemy          wpięte na sztywno — nie mogą zawieść
 2.  kroki systemów   każdy system: swoje paczki (zepsute oznaczone, nie blokują),
                      swój rejestr — zbudowany raz, tylko do odczytu — i rozgrzewka kart
 3.  kroki ramy       półka, dane kampanii z półki, rozgrzewka ekranów ramy i zakładek systemów
 4.  wybór systemu    ekran pełnoekranowy, interaktywny dopiero po krokach 2–3
 5.  po wyborze       półka kampanii tego systemu — nic nie jest budowane od zera
```

**Treść jest sprawdzana przed kampaniami.** Żaden krok nie ma prawa zatrzymać wejścia do programu —
awaria dowolnego degraduje do leniwego wczytywania i zostawia ostrzeżenie na pasku.

**Po ekranie ładowania nic się nie zacina.** Start rozgrzewa, za ekranem ładowania i zanim cokolwiek
da się kliknąć, każdy widok, który użytkownik może zobaczyć po raz pierwszy w typowej ścieżce —
ekrany ramy i zakładki, karty i okna narzędzi każdego wkompilowanego systemu. Długość startu nie jest
miarą; zacięcie jest. Wybór systemu, otwarcie kampanii i wejście w zakładkę nie budują niczego, czego
typ nie został rozgrzany.

### 21.2 Zmiana stanu: z kliknięcia na dysk

```
 edycja pola na karcie
     │
     ▼  widok tworzy nowy rekord:  monster with { Hp = 3 }
     │
     ▼  różnica wobec wpisu → nowa wersja instancji z łatką  { "hp": 3 }
     │
     ▼  CampaignSession.ChangeAsync — jedyne wejście do zmiany stanu;
     │  przyjmuje nowe wersje konkretnych rzeczy, odmawia w trakcie innej
     │  zmiany i w trakcie powiadomień
     │
     ▼  zapis: plik tymczasowy → atomowe przeniesienie → licznik generacji
     │
     ▼  powiadomienie z nową migawką — tylko odczyt
     │
     ▼  widoki odczytują stan na nowo
```

* **Jest dokładnie jedno wejście** do zmiany otwartej kampanii. Nie ma drugiej drogi.
* **Jedna operacja może mieć kilka skutków** — sekcja *Granica automatyzacji* — i wszystkie idą
  jednym zatwierdzeniem.
* **Powiadomienia informują, nigdy nie zapisują** — wejście zmiany odmawia w trakcie ich rozsyłania.
  Każdy subskrybent dostaje powiadomienie, nawet gdy inny rzuci wyjątek.

### 21.3 Pozostałe

**Przeglądanie treści.** Zakładka kategorii System → lista → karta wybranej pozycji. Nie wymaga
otwartej kampanii.

**Otwarcie kampanii.** Manifest → system kampanii (nieobecny w programie: kampania niedostępna) →
włączone warianty → złożenie zakładek i okien → zadeklarowane paczki → rozwiązanie referencji →
wczytanie instancji (wartości wpisu scalone z łatką) → zakładki kampanii otwarte, pozycja półki
zamieniona w stronę kampanii i pokazana.

**Powrót do wyboru systemu.** Zamknięcie kampanii → zwolnienie zakładek i okien systemu → ekran
wyboru.

**Wprowadzenie wpisu do świata.** Wybór z rejestru filtrowanego do paczek kampanii → nowa instancja:
nowe id, referencja, pusta łatka, zawartość slotów z wpisu.

**Wpis widziany przez cudzego konsumenta.** Konsument zna interfejs; typ treści go implementuje;
konsument dostaje obiekt. Tracker tur, wiersz ekwipunku i sumowanie wkładów to trzy zastosowania
jednego mechanizmu.

**Prowadzenie przygody.** Zakładka fabuły → renderer dokumentu nad instancją → odhaczenie zapisuje
się jako klucz łatki tą samą ścieżką co zmiana stanu.

**Dlaczego →** [decisions.md](decisions.md), *Przepływy*.

---
---

# CZĘŚĆ III — WYKONANIE

## 22. Granice mechaniczne

| Granica | Jak egzekwowana | Status |
|---|---|---|
| logika ramy bez Avalonii | test po referencjach | istnieje (`Core`) |
| logika bibliotek bez Avalonii | test po referencjach | istnieje (`Library.Entries`) |
| biblioteki nie referencują się nawzajem, poza jawnym wyjątkiem | test po referencjach | istnieje; części jednej biblioteki nie są wyjątkiem, tylko jedną biblioteką |
| rama i biblioteka bez **nazw rodzajów** treści | skan źródeł po słowniku z systemu | istnieje dla ramy i bibliotek |
| rama i biblioteka bez **nazw pól** treści | koperta: nie ma API przyjmującego nazwę pola | wchodzi z kopertą, nie testem |
| rama nie referencuje biblioteki | test po referencjach | istnieje (`Core`, `Desktop`) |
| rama i biblioteka nie referencują systemu | test po referencjach | istnieje |
| system nie referencuje innego systemu | test po referencjach | istnieje |
| zakładka kategorii System nie widzi kampanii | kształt API ramy: deklaracja zakładki tej kategorii tworzy ją bez żadnego argumentu; test po refleksji na deklaracji | istnieje |
| o wariantach system dowiaduje się w jednym miejscu | kształt API ramy: konteksty zakładek i okien ich nie niosą | powstaje z pierwszym dodatkiem |
| narzędzie nie introspekcjonuje typu treści | przegląd; kandydat na test | do rozstrzygnięcia |
| brak kaskad zmian stanu | `MaxEventsPerCommand` jako tripwire | istnieje, uzasadnienie do przepisania |

* **Słownik zakazanych słów jest wyciągany z systemu, nie wpisany ręcznie** — dodajesz typ
  „Zaklęcie" i zakaz sam się o to słowo poszerza.
* **Skan obejmuje nazwy rodzajów, nie nazwy pól.** Nazw pól pilnuje **kształt kodu**: wartości wpisu
  przechodzą do systemu jako koperta, a w ramie ani w bibliotece nie istnieje API przyjmujące nazwę
  pola. **Kryterium powrotu do dyskusji:** pierwszy interfejs w ramie albo w bibliotece przyjmujący
  nazwę pola jako napis.
* **Dopasowanie w skanie respektuje granicę CamelCase**, nie tylko granicę słowa, i czyta pliki
  `.axaml`.
* **Mechanizm zastępczy powstaje przed usunięciem mechanizmu, który zastępuje.**
* **Testy inwestuje się w to, co przeżyje kolejną wersję architektury** — framework okien tak,
  warstwa treści dziś nie zasługuje na gęste pokrycie.
* **`TreatWarningsAsErrors`** jest częścią walidacji treści: ostrzeżenia kompilatora są
  ostrzeżeniami o treści.

**Dlaczego →** [decisions.md](decisions.md), *Granice mechaniczne*.

## 23. Pytania otwarte i reguła „nic bez konsumenta"

1. **Format pliku wpisu.** Kandydat: front-matter plus treść, ten sam, który przyjęto dla dokumentu.
   **Rozstrzygnąć po slotach, nie przed.** W międzyczasie nie robić: tablicy napisów udającej
   akapity, prozy we front-matterze, własnego DSL-a.
2. **Autorstwo treści w aplikacji.** Zakładki treści są tylko do odczytu; pytanie zostaje otwarte dla
   wpisów. Obejście przez wpisy lokalne dla kampanii pozostaje odrzucone.
3. **Katalog pól formularza dokumentu i kształt markera.**
4. **Czy silnik formuł potrzebuje tablicy przeglądowej** — premia z biegłości, stopnie kości
   w Savage Worlds. Tablica stała nie łamie zakazu gałęzi, ale poszerza język.
5. **Jakie jeszcze pola trafiają do nakładki poza stanem i slotami.** Reguła rozstrzygająca jest
   zapisana; brakuje przejścia przez realny system.
6. **Widok domyślny karty.** Czy rodzaj treści, którego nikt nie zechce zaprojektować, dostaje
   jakąkolwiek kartę zastępczą. **Wyzwalacz:** pierwszy taki rodzaj treści.
7. **Paczka a system.** Czy paczka ma deklarować swój system, i gdzie pokazuje się paczka odrzucona
   za zepsuty manifest, której systemu nie da się poznać. „Nigdzie" nie jest odpowiedzią.
   Wspólnego rejestru nie ma: każdy system czyta katalog paczek sam i każdy odrzuca
   zepsutą paczkę osobno, więc przy drugim systemie ta sama paczka byłaby zgłaszana tyle razy, ile
   jest systemów, a wpisy paczki pisanej pod cudzy system — oznaczane jako wadliwe w rejestrze
   każdego systemu poza jej własnym.
   **Wyzwalacz:** pierwsze miejsce na ekranie dla odrzuconych paczek ([tasks.md](tasks.md),
   „Czekają na miejsce na ekranie").
8. **Ziarnistość dodatków.** Czy wariant jako jednostka włączania wytrzymuje zderzenie z prawdziwym
   dodatkiem, czy któryś wariant okaże się sensowny wyłącznie w pakiecie z innymi. **Wyzwalacz:**
   pierwszy prawdziwy dodatek.
9. **Wariant zmieniający zakładki kampanii.** Zamknięte zakładki pokazują listę systemu bez
   wariantów; kampania z wariantem, który dokłada albo podmienia zakładkę, po otwarciu pokaże inną.
   **Wyzwalacz:** pierwszy taki wariant.

**Reguła: nic nie wchodzi bez konsumenta w tym samym wycinku** — ani pole, ani mechanizm.
Rusztowanie, którego kod czytający istnieje i działa (dziś: `AllowsMultipleInstances`), nie jest
rezerwacją.

**Dlaczego →** [decisions.md](decisions.md), *Pytania otwarte i reguła „nic bez konsumenta"*.

## 24. Kolejność prac

Kroki 1–8 są zrobione. Otwarte:

| # | Krok | Uwagi |
|---|---|---|
| 9 | Przebudowa: rama, biblioteka, system — ekran wyboru systemu, pasek boczny z trzema kategoriami, biurko jako zakładka systemu, zapis modeli stanu systemu przez ramę | przed 10 i 11, bo oba powstają w bibliotece; etapy w [tasks.md](tasks.md) |
| 10 | Zakładki treści zamiast zakładki rejestru | po 9, bo szkielet należy do biblioteki wpisów; wygląd — mockup autora |
| 11 | Formuły, sloty, dokument | kolejność do ustalenia osobno |

**Dlaczego →** [decisions.md](decisions.md), *Kolejność prac*.
