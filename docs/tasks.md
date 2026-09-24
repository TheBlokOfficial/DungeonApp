# DungeonApp — kolejka pracy

**Status: stan na 2026-09-24.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-state.md](code-state.md) w jakim stanie jest kod,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

**Zakres: wyłącznie to, co trzeba zrobić przed zamknięciem bieżącego etapu.** Nie jest spisem funkcji
aplikacji i nie zapisuje się tu pracy koncepcyjnej na zapas — całość docelowa mieszka
w [architecture.md](architecture.md), a pytania niezamknięte wraz z warunkami powrotu tam oraz
w [decisions.md](decisions.md). Pozycja wpisana tu przed swoim czasem starzeje się po cichu: nic nie
zmusza do jej przeliczenia, a sam fakt, że stoi zapisana, z czasem zaczyna uchodzić za uzasadnienie.

> **Ten dokument nie prowadzi archiwum.** Praca domknięta znika stąd, gdy tylko przestanie być
> potrzebna do zrozumienia następnego kroku. Co zostało zrobione, mówi historia gita; **dlaczego** —
> `decisions.md` i `architecture.md`; **w jakim stanie jest kod** — `code-state.md`. Do 2026-09-14 stała tu
> sesyjna kronika na dziewięćdziesiąt linii, wbrew temu zdaniu, które w tym dokumencie już wtedy było;
> 2026-09-23 dokument znów miał 271 linii, z czego trzy czwarte było zamkniętą historią etapów.

Gałąź: `master`. Build bez ostrzeżeń, 344 testy zielone (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

---

## Następne: krok 10 — zakładki treści zamiast zakładki rejestru

Docelowy kształt — [architecture.md](architecture.md), *Zakładki treści*; wygląd — mockup autora
`docs/images/mockup_rejestr.png`. Zakres pierwszej wersji ustalony z autorem 2026-09-24.

* **Dwie zakładki w kategorii System: potwory i przedmioty**, z jednego szkieletu biblioteki wpisów.
  Zakładka „Rejestr" znika razem z nimi.
* **Wchodzi:** wyszukiwanie po nazwie; sortowanie po nazwie i po kluczu systemu; filtry po
  kategorii, po paczce i po wartości wskazanej przez system; czyszczenie filtrów; odznaka wiersza;
  licznik wpisów; nagłówek szczegółu (kategoria, nazwa, tagi) rysowany przez szkielet, pod nim karta
  systemu; sekcje po paczce z rzeczami zepsutymi na dole; powód błędu w miejscu karty; przycisk
  wczytania paczek od nowa.
* **Nie wchodzi:** tworzenie, edycja, kopiowanie, usuwanie; zaklęcia i NPC; filtr po typie treści
  (bez zakładki z kilkoma typami nie ma konsumenta); pasek stanu i Ctrl+K z mockupu.
* **Wymiary z `docs/images/mockup_rejestr.html`**, nie z obrazka. Pasek tytułu okna w mockupie nie
  jest projektem paska górnego. **Mockup jest prawie kwadratowy** — na ekranie 16:9 lista trzyma
  szerokość z mockupu, treść szczegółu swoją największą szerokość z mockupu, wyrównana do listy.
* **Kolory po znaczeniu:** treść niewczytana bierze kolor „niebezpieczeństwa"; kolory stanów
  dostają komentarze ze znaczeniem (niebezpieczeństwo — zepsute albo nieodwracalne; ostrzeżenie —
  wymaga uwagi, ale działa; sukces — udało się); kolory rzadkości — własne tokeny w systemie.

**Następne (ustalone z autorem 2026-09-24, po obejrzeniu aplikacji) — dwa wąskie zlecenia, po kolei,
na Opusie, według nowych zasad (limit czasu; wygląd sprawdza autor). Każde kończy się zdjęciem widoku
z przykładowymi danymi, które architekt kładzie obok mockupu przed scaleniem — odbiór zaproponowany,
autor nie odpowiedział.** Wymiary z CSS mockupu: brief przytacza selektory (`.entry`,
`.entry.selected::before`, `.sidebar-item.active::before`, `.filter-chip`, `.filter-chip.on`,
`.search`, `.clear-filters`, `.list-scroll`), nie każe czytać całego pliku.

*A. Lista i filtry*
* Kreska zaznaczenia wpisu **przed** wierszem, w odstępie (mockup: `left: -10px`) — lista ma wcięcie,
  kontener wiersza pełnej szerokości, kreska w wcięciu; bez ujemnych marginesów. Na pasku bocznym
  odstęp jak w mockupie (`left: -12px`) — dziś kilkakrotnie za mały.
* Pasek przewijania tylko, gdy jest co przewijać — dziś stały szary pas przy liście.
* **Wysokość i szerokość wiersza nadaje biblioteka**, nie zawartość — dziś przedmioty (pigułka
  rzadkości) mają wyższe wiersze niż potwory.
* **Filtr = lista wartości z polami wyboru**, bez pozycji „Wszystkie"; nic nie zaznaczone = wszystko.
  Wewnątrz filtra „lub", między filtrami „i" (architekt). Chip reaguje na najechanie, a otwarty ma
  inne tło i obrócony chevron; chip z zaznaczeniem — styl `.filter-chip.on`. Lista: tło i obramowanie
  jak pole wyszukiwania, wiersze czcionką wierszy listy. Istniejące testy filtrów dostosować do nowej
  semantyki.
* „Wyczyść filtry" wygaszone, gdy nie ma czego czyścić.
* Pole wyszukiwania: tekst zastępczy wyśrodkowany w pionie; kliknięcie poza polem i Escape zdejmują
  z niego fokus (dziś kursor miga dalej).

*B. Karta potwora*
* Nagłówki „Cechy szczególne"/„Akcje" jasne i wyraźne jak w mockupie; „CECHY" wersalikami; styl
  etykiet cech; liczby KP/PZ/szybkości i cech wyróżnione; ikonki przy KP, PZ, szybkości.

*Pytania do autora przed B:* (1) krój o stałej szerokości znaków dla liczb (mockup: IBM Plex Mono;
w aplikacji go nie ma)? (2) wyzwanie i PD — osobne pola w paczce teraz czy z akcjami w kroku 11? Od
tego zależy odznaka „samo wyzwanie" (dziś „1/2 (100 PD)" — jeden napis, wycinać nie wolno). Nazwy
akcji, pogrubione premie i nagłówki akcji z mockupu wymagają akcji jako osobnych rzeczy — krok 11.

**Czeka na autora:** ręczne przeniesienie kampanii do `Dokumenty\DungeonApp\dnd5e\campaigns\`
(paczka skopiowana 2026-09-24; stare `Packs` i `Campaigns` nie są czytane); dopisanie `group`
potworom.

**Zlecenia, po kolei:**
1. ~~Logika szkieletu bez okna~~ — zrobione 2026-09-24 (model listy i profile typów w bibliotece
   wpisów, profile D&D w systemie; porządek wyświetlania po polsku).
2. ~~Widok według mockupu i dwie zakładki systemu w miejsce rejestru~~ — scalone 2026-09-24.
   **Czeka na sprawdzenie przez autora w aplikacji.**
3. ~~Katalogi paczek i kampanii per system, reguła niezgodności, tło kampanii niedostępnej~~ —
   scalone 2026-09-24. Paczek dostarczanych z systemem nie ma w kodzie — `code-state.md`, *Dług*.
4. Zwiad: jak żyje rejestr (kto go trzyma, kto dostaje przy zakładkach i biurku); potem zlecenie
   wczytania paczek od nowa.

**Po kroku 10, osobnymi etapami:** dodanie paczki przeciągnięciem do okna (do rozstrzygnięcia: katalog
czy archiwum; kolizja nazwy — odmowa z komunikatem, nigdy nadpisanie); karta przedmiotu
według mockupu (karta potwora zrobiona 2026-09-24).

Po kroku 10 — krok 11: formuły, sloty i dokument, od razu w bibliotece. **Nic nie wchodzi na
zapas** między krokami; dodatki powstają z pierwszym prawdziwym dodatkiem (niżej).

---

## Poprawki czekające na obszar

Drobne poprawki nie dostają własnego zlecenia. Czekają, aż wykonawca będzie pracował w ich obszarze,
albo aż zbierze się ich tyle, że warto dać im osobnego — reguła w [collaboration.md](collaboration.md),
*Jak zapadają decyzje*. Zlecenie, które wchodzi w dany obszar, zabiera stąd wszystko, co do niego należy.

Pusto — obie poprawki zabrały zlecenia z 2026-09-24.

---

## Odłożone

### Czeka na pierwszy prawdziwy dodatek: mechanizm dodatków

Model i reguły — [architecture.md](architecture.md), *Dodatki*; forma dodatku należy do ramy,
a jednostką włączania jest wariant pod nagłówkiem dodatku. Dziś nie ma ani jednego dodatku.
Przykład, na którym rozmawiano — złoto w sakiewkach zamiast na postaci — wymaga licznika złota,
którego też jeszcze nie ma.

**Czekanie nie kosztuje nic, i to jest własność repozytorium, nie prognoza.** Deserializacja
manifestu kampanii jest celowo pobłażliwa, więc dołożenie listy włączonych dodatków później nie
podnosi wersji formatu i nie wymaga migracji — patrz [decisions.md](decisions.md), „Zarezerwowane
pola `Ruleset` i `ContentPacks` w manifeście kampanii".

**Wyzwalacz:** pierwszy wariant zasad, który autor chce mieć w konkretnej kampanii.

### Czeka na miejsce na ekranie: nazwa okazu

Czeka, aż autor zechce zaprojektować dla niej miejsce na ekranie.

* **Nie da się nazwać okazu.** Operacja zmiany nazwy własnej instancji istnieje, jest przetestowana
   i **nikt jej nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, mimo
   że lista pokazuje właśnie nazwę własną, gdy jest. Konsument jest jednym polem tekstowym stąd.

### Czeka na zacięcie: zacięcia mierzyć, nie oglądać

2026-09-22 autor potwierdził, że wejście w system działa płynnie — i sam zauważył, że dowodu nie ma:
animacja rozwijania paska, która dawniej czyniła zacięcie widocznym, już nie gra przy wejściu.
2026-09-23 etap 4 przestawił kolejność rozgrzewki przy starcie. **Wyzwalacz:** pierwsze zacięcie
zauważone przez autora. Wtedy najpierw logowane liczby — czas rozgrzewki i czas od kliknięcia
systemu do pierwszej narysowanej klatki — dopiero potem poprawka.

### Czeka na potrzebę: kopie zapasowe kampanii, zapis ręczny, wersjonowanie

Zapis od razu zostaje — decyzja autora z 2026-09-22, uzasadnienie w [decisions.md](decisions.md),
*Gdzie mieszka stan*. **Wyzwalacz:** MG chce wrócić do wcześniejszego stanu kampanii albo zapis po
każdej zmianie staje się odczuwalnie wolny. Wtedy pierwszym kandydatem są rotujące kopie zapasowe,
nie zapis ręczny.
