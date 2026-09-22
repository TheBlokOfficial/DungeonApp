# DungeonApp — kolejka pracy

**Status: stan na 2026-09-22.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

**Zakres: wyłącznie to, co trzeba zrobić przed zamknięciem bieżącego etapu.** Nie jest spisem funkcji
aplikacji i nie zapisuje się tu pracy koncepcyjnej na zapas — całość docelowa mieszka
w [architecture.md](architecture.md), a pytania niezamknięte wraz z warunkami powrotu tam oraz
w [decisions.md](decisions.md). Pozycja wpisana tu przed swoim czasem starzeje się po cichu: nic nie
zmusza do jej przeliczenia, a sam fakt, że stoi zapisana, z czasem zaczyna uchodzić za uzasadnienie.

> **Ten dokument nie prowadzi archiwum.** Praca domknięta znika stąd, gdy tylko przestanie być
> potrzebna do zrozumienia następnego kroku. Co zostało zrobione, mówi historia gita; **dlaczego** —
> `decisions.md` i `architecture.md`; **jak jest teraz** — `code-map.md`. Do 2026-09-14 stała tu
> sesyjna kronika na dziewięćdziesiąt linii, wbrew temu zdaniu, które w tym dokumencie już wtedy było.

Gałąź: `master`. Build bez ostrzeżeń, 263 testy zielone (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

---

## Gdzie jesteśmy

Kroki 1–8 z sekcji „Kolejność prac" [architecture.md](architecture.md) są **zrobione** — krok 8
domknięty 2026-09-14 usunięciem licznika i całej warstwy bloków danych, a 2026-09-15 dogonieniem
go przez dokumenty. Było to przejście z szablonów jako plików danych na skompilowane typy treści,
a następnie osadzenie treści w konkretnej kampanii. Stan, do którego to doprowadziło:

* **Typy treści są kodem.** System `DungeonApp.Content.Dnd5e` niesie `Monster` i `Gear`, ich
  zaprojektowane karty i jedno okno biurka. Deserializator jest jedynym walidatorem.
* **Granice są pilnowane mechanicznie**, nie deklarowane — cztery testy architektoniczne, w tym skan
  słownictwa, który poszerza się sam wraz z przybywającymi typami treści.
* **Kampania ma własny świat.** Instancje z rzadką łatką, zapis całej generacji na dysk w jednej
  transakcji, rozwiązywanie wskazania wobec rejestru jako osobna warstwa odczytu.
* **System wnosi własne okno biurka** — mechanizm pasa narzędzi. Okno „Świat kampanii" jest
  pierwszym prawdziwym narzędziem biurka i pierwszym konsumentem magazynu instancji.
* **Jeden prymityw zapisu atomowego** zamiast trzech ręcznie pisanych kopii.
* **Instancje są jedynym magazynem stanu kampanii.** Warstwa bloków danych odeszła bez następcy;
  kształt, w jakim wróci razem ze swoim konsumentem, stoi w [decisions.md](decisions.md), pozycja
  „Utrzymanie warstwy bloków danych po odejściu jej jedynego konsumenta".

Szczegóły każdej z tych rzeczy — [code-map.md](code-map.md).

**2026-09-22 zapadła przebudowa, którą kod dogania etapami (niżej):** dawny silnik i powłoka stają się
ramą, wspólny kod wychodzi do biblioteki, a system — dawniej zestaw — przejmuje wygląd i zawartość
aplikacji. Model — [architecture.md](architecture.md), *Rama, biblioteka, system*.

---

## Następne

**Krok 9 z „Kolejność prac" [architecture.md](architecture.md): przebudowa na ramę, bibliotekę
i system.** Docelowy kształt stoi w architekturze — sekcje *Rama, biblioteka, system*, *Nawigacja:
ekran wyboru systemu i pasek boczny*, *Gdzie mieszka stan*, *Narzędzia biurka i system okien*.

**Plan etapów — zatwierdzony przez autora 2026-09-22.** Każdy etap dostaje osobne zielone światło;
jak przebiega jeden etap — [collaboration.md](collaboration.md), *Obieg jednego etapu*. Po każdym
etapie aplikacja działa, a testy przechodzą.

| # | Etap | Co widzi autor | Dlaczego w tym miejscu |
|---|---|---|---|
| 1 | **Ekran wyboru systemu i pasek boczny.** Rama wystawia systemowi deklarację zakładek; biurko i rejestr stają się zakładkami D&D; pozycja kampanii — półka albo strona kampanii; zakładki kampanii zamknięte bez otwartej kampanii; powrót do wyboru i zamknięcie kampanii jako przyciski ramy; zakładka kategorii System nie dostaje kampanii. | nowe wejście do aplikacji, pasek z grupami Kampania i System, strona kampanii, kłódki | rama musi przestać sama stawiać biurko, zanim da się je z niej wynieść |
| 2 | **Wyniesienie wspólnego kodu interfejsu do biblioteki:** biurko i system okien, kontrolki kart, widok listy z kartą. Najpierw testy granic, potem przeprowadzka. | nic — zachowanie jak po etapie 1 | mechanizm pilnujący granicy powstaje przed rozbiórką |
| 3 | **Rama zapisuje modele stanu systemu.** Instancje pierwszym takim modelem; kampania pamięta swój system; półka pokazuje kampanie aktywnego systemu. | kampanie w obrębie swojego systemu | rdzeń trzyma dziś instancje wewnątrz kampanii i musi przestać, zanim da się je wynieść |
| 4 | **Wyniesienie wspólnej logiki do biblioteki wpisów:** paczki, wpisy, rejestr, instancje, nakładki. Projekt z etapu 2 rozdziela się na bibliotekę biurka i część interfejsu biblioteki wpisów (kontrolki kart, lista z kartą); biblioteki nie referencują się nawzajem. Wczytywanie paczek staje się krokiem startowym, który rama uruchamia, nie wiedząc, co robi. | nic | wymaga etapu 3 |

**Ustalenia do etapów** — zapisane, żeby nie trzeba ich było tłumaczyć od nowa:

* **Etap 1 — zrobiony 2026-09-22.** Aplikacja startuje na ekranie wyboru systemu; pasek boczny ma
  grupy Kampania i System, kategorię Aplikacja z „Zmień system"; biurko i rejestr są zakładkami D&D;
  strona kampanii zastąpiła półkę po otwarciu i niesie „Zamknij kampanię". Interfejs jest technicznie
  poprawny, bez fajerwerków — wygląd autor przeprojektuje sam.
  - **Do weta autora** — rozstrzygnięte przez architekta przed etapem: nazwa `IGameSystem`; kłódka
    zamiast ikony zakładki; „Zmień system" w kategorii Aplikacja;
    bez rozgrzewki biurka przy pustej półce. W trakcie, przez wykonawcę: logika cyklu życia zakładek
    wydzielona do osobnej, testowalnej klasy (powłoka dalej bez testów); rozgrzewka i wczytanie półki
    przeszły z kroków startowych do wyboru systemu, bo od niego zależą; ikona zakładki „Biurko" —
    istniejąca ikona paska zminimalizowanych; test „biurko bez gestu nie zapisuje" sprawdza model
    biurka, nie samą zakładkę, bo projekt testowy nie ma harnessu Avalonii.
  - **Po uruchomieniu przez autora (2026-09-22):** awaria po wyborze systemu — widok biurka
    budowany poza wątkiem okna — poprawiona. W toku: brakująca na pasku pozycja kampanii (błąd
    etapu — nie da się wrócić do półki); **weto autora** na separator zamiast nagłówków grup —
    nagłówki, zwijanie i ich animacje wracają w dawnym wyglądzie; diagnoza skakania treści po wejściu
    w system. Rejestr jako zakładka jest przejściowy — do zakładek treści.
  - **Znane ograniczenie.** Zakładka rejestru rysuje karty prezentacją swojego systemu, więc przy
    drugim systemie wpis cudzego systemu nie dostanie karty — wraca z pytaniem „Paczka a system"
    w architekturze.
* **Etap 2 — zrobiony 2026-09-22.** Nowy projekt `DungeonApp.Library.Desktop` z biurkiem, systemem
  okien, kontrolkami kart, widokiem listy z kartą i oknem narzędzia na kampanię; etap 4 dołoży obok
  `DungeonApp.Library` bez Avalonii. Biblioteka referencuje ramę, rama biblioteki nie — pilnują tego
  nowe testy granic. Style okien biurka wyszły z motywu ramy dosłownie i włącza je sama biblioteka.
  Rozstrzygnięcia, do weta: nazwa projektu; pomocnicze kontrolki i konwerter ikon zostały w ramie,
  bo używa ich też rama; pomocnicze klasy testowe skopiowane do nowego projektu testowego.
* **Po etapach 1–2, stan na koniec sesji 2026-09-22 — czeka na uruchomienie przez autora.** Trzy
  przebiegi poprawek po jego testach: awaria po wyborze systemu (widok poza wątkiem okna), niewidoczne
  wiersze paska (pozycja kampanii i „Zmień system" rysowane poza mechanizmem list), weto na separator,
  zwijanie jednym ruchem, odstępy z `555802f`, rozgrzewka przeniesiona do startu. **Niezweryfikowane
  w aplikacji:** ostatni przebieg (rozgrzewka przy starcie, pasek bez klatki przejściowej) został
  przerwany na etapie weryfikacji subagenta — build i 263 testy zielone, okna nikt nie widział.
  **Do sprawdzenia najpierw:** czy zacięcie przy wejściu w system zniknęło, czy pasek pojawia się od
  razu w docelowym stanie i pamięta zwinięcie po „Zmień system", czy ekran ładowania zasłania
  rozgrzewkę — kurtyna startowa jest w kodzie, autor potwierdził 2026-09-22, że po tych poprawkach
  wejście w system działa płynnie; dowodu na brak zacięcia nie ma, bo pasek nie animuje się już przy
  wejściu (patrz niżej, pozycja o mierzeniu).
* **Zacięcia mierzyć, nie oglądać.** 2026-09-22 autor potwierdził, że po poprawkach wejście w system
  działa dobrze — i sam zauważył, że dowodu nie ma: animacja rozwijania paska, która dawniej czyniła
  zacięcie widocznym, już nie gra przy wejściu. Następnym razem: czas rozgrzewki i czas od kliknięcia
  systemu do pierwszej narysowanej klatki jako logowane liczby.
* **Odchudzić mapę kodu do części oceniającej.** Spis plików wypada, zostają granice, punkty styku
  i ocena stanu; zasada i powód — [collaboration.md](collaboration.md), *Jak pisać dokumenty tego
  repozytorium*, punkt o mapie kodu. Wersja pełna, dogoniona do etapów 1–2, powstała 2026-09-22, więc
  ta praca jest już tylko skracaniem.
* **Etap 3 — w toku od 2026-09-22.** Zielone światło dane; wykonanie dwoma briefami po kolei (silnik
  stanu i zapis, potem system w kampanii i półka), autor sprawdza na `master` po scaleniu. Docelowy kształt —
  [architecture.md](architecture.md), *Gdzie mieszka stan*; uzasadnienie — [decisions.md](decisions.md),
  ta sama sekcja. Do briefu:
  - **Stan dziś:** zakładka i narzędzie systemu dostają żywy magazyn instancji z metodami
    zmieniającymi; powiadomienia wychodzą w trakcie operacji, przed zapisem; wejście zmiany nie ma
    blokady przed wywołaniem z obsługi powiadomienia; manifest nie zna systemu i ignoruje nieznane pola.
  - **Po etapie:** modele stanu niezmienne, oznaczone jako zapisywalne, deklarowane przez system albo
    bibliotekę (identyfikator, wersja, rekord); instancje pierwszym modelem. Poza wejściem zmiany
    system widzi stan tylko do odczytu. Wejście przyjmuje nowe wersje konkretnych rzeczy (zmienione,
    nowe, usunięte), zapisuje je jednym zatwierdzeniem, dopiero potem powiadamia; odmawia w trakcie
    innej zmiany i w trakcie powiadomień. Zapis od razu, bez optymalizacji „tylko zmienione".
  - **Kampania pamięta system** — pole w manifeście; bez niego albo z nieobecnym systemem widoczna
    jako niedostępna. Półka pokazuje kampanie aktywnego systemu. Niezgodna wersja modelu — kampania
    niedostępna, bez migracji.
  - **Rozstrzygnięte przez architekta, przyjęte bez weta 2026-09-22:** nieudany zapis na dysk
    zostawia zmianę w pamięci z ostrzeżeniem na pasku, jak dziś; model to zbiór rzeczy z własnym
    identyfikatorem (rzecz pojedyncza — zbiór jednoelementowy); jeden plik na model; model
    zadeklarowany, a nieobecny na dysku jest pusty, plik modelu nieznanego zostaje nietknięty;
    wejście zmiany odmawia, nie kolejkuje; instancje do etapu 4 w rdzeniu, w wydzielonym miejscu,
    którego zapis i wejście zmiany nie znają (test granicy); kampanie nieprzypisywalne do żadnego
    obecnego systemu widać na każdej półce jako niedostępne, kampanie innego obecnego systemu są
    ukryte; wygląd kampanii niedostępnej bez fajerwerków.
  - **Czego struktura nie zatrzyma:** widok, który po powiadomieniu sam zaplanuje zmianę na później.
    Pilnuje tego przegląd, nie kod.
  - **Obieg:** etap dotyka zapisu stanu i granicy automatyzacji, więc wynik porównuje z briefem
    i z pięcioma zakazami drugi subagent.
* **Etap 3 — stare kampanie to dane testowe.** Decyzja autora: kampanie zapisane bez systemu stają
  się niedostępne — widoczne, nie znikają ([architecture.md](architecture.md), *Gdzie mieszka
  stan*) — i zakłada się je od nowa. Żadnego kodu przypisującego im system.
* **Między etapami nic nie wchodzi na zapas.** Dodatki powstają z pierwszym prawdziwym dodatkiem
  (niżej, „Odłożone"), formuły, sloty i dokument — po etapie 4.

Formuły, sloty i dokument są odtąd krokiem 10 i czekają na przebudowę, żeby powstać od razu
w bibliotece.

---

## Odłożone

### Czeka na pierwszy prawdziwy dodatek: mechanizm dodatków

Model i reguły — [architecture.md](architecture.md), *Dodatki*; od 2026-09-22 forma dodatku należy
do ramy, a jednostką włączania jest wariant pod nagłówkiem dodatku. Mechanizm powstaje razem
z pierwszym prawdziwym dodatkiem, nie wcześniej: dziś nie ma ani jednego. Przykład, na którym
rozmawiano — złoto w sakiewkach zamiast na postaci — wymaga licznika złota, którego też jeszcze nie
ma.

**Czekanie nie kosztuje nic, i to jest własność repozytorium, nie prognoza.** Deserializacja
manifestu kampanii jest celowo pobłażliwa, więc dołożenie listy włączonych dodatków później nie
podnosi wersji formatu i nie wymaga migracji — patrz [decisions.md](decisions.md), „Zarezerwowane
pola `Ruleset` i `ContentPacks` w manifeście kampanii".

**Wyzwalacz:** pierwszy wariant zasad, który autor chce mieć w konkretnej kampanii.

### Czekają na miejsce na ekranie — trzy pozycje o interfejsie

Wszystkie czekają, aż autor zechce zaprojektować dla nich miejsce na ekranie. Żadna nie blokuje
przebudowy — ale przebudowa zmienia ekran, na którym dwie pierwsze by stanęły: rejestr przechodzi
do zakładek treści systemu. Warto więc rozważyć je przy projektowaniu tych zakładek, nie osobno.

1. **Odrzucone paczki nigdzie się nie pokazują.** Loader je odnotowuje, ekran rejestru ich nie
   wyświetla — świadoma decyzja autora z 2026-09-12. Jedyna pozycja z tabeli „Co się dzieje, gdy
   treść jest zepsuta", o której Mistrz Gry nie dowiaduje się z aplikacji: paczka odrzucona za
   literówkę w manifeście znika dziś po cichu.
2. **Nagłówek `NIE WCZYTANE` czyni pierwszy zepsuty wiersz wyższym od pozostałych**, bo niesie go ten
   wiersz, a nie prawdziwy nagłówek sekcji. Cena za „jedna lista, jeden szablon", zostawiona
   świadomie 2026-09-13.
3. **Nie da się nazwać okazu.** Silnik umie zmienić nazwę własną instancji, jest to przetestowane
   i **nikt tego nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, mimo
   że lista pokazuje właśnie nazwę własną, gdy jest. Konsument jest jednym polem tekstowym stąd.

---

### Czeka na potrzebę: kopie zapasowe kampanii, zapis ręczny, wersjonowanie

Zapis od razu zostaje — decyzja autora z 2026-09-22, uzasadnienie w [decisions.md](decisions.md),
*Gdzie mieszka stan*. **Wyzwalacz:** MG chce wrócić do wcześniejszego stanu kampanii albo zapis po
każdej zmianie staje się odczuwalnie wolny. Wtedy pierwszym kandydatem są rotujące kopie zapasowe,
nie zapis ręczny.

---

## Archiwum

`archive/pre-pivot-content-layer` (`f5d5b7a`) — **nigdy nie scalana**. Praca sprzed
przeprojektowania warstwy treści: `SummaryContract`, `ContractFill`,
`SummaryContractResolution`, grupowanie rejestru, rozbudowane testy loadera i fixture'y.
Trzy fixture'y wpisów zostały stamtąd przywrócone; resztę trzyma się tylko po to, żeby nic
nie zginęło.
