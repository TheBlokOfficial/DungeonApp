# DungeonApp — kolejka pracy

**Status: stan na 2026-09-24.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-state.md](code-state.md) w jakim stanie jest kod,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.
Duże zadanie w toku może mieć własny dokument w [zadania/](zadania/) — wtedy tutaj stoi o nim jedna
linia z odnośnikiem, a jego stan, plan i notki są tam (`collaboration.md`, *Dokument zadania*).

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

Gałąź: `master`. Build bez ostrzeżeń, 367 testów zielonych (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

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

**Następne — plan ustalony z autorem 2026-09-24, pod koniec sesji:**
1. ~~Reguła „gust autora, rzemiosło wykonawcy"~~ — zrobione 2026-09-24 (`collaboration.md`, lista
   kontrolna w definicji wykonawcy).
2. ~~Wpis o interfejsie w HTML-u~~ — zrobione 2026-09-24 (`decisions.md`, pozycja 39, z wyzwalaczem).
3. **Fundament interfejsu — zanim cokolwiek innego w interfejsie.** Prowadzony w dokumencie zadania
   [zadania/fundament-interfejsu.md](zadania/fundament-interfejsu.md): zakres, porcje, ustalenia,
   pomiar. Stąd zniknie, gdy zadanie się zamknie.
4. **Zlecenia A i B niżej, w małych porcjach** — składane z fundamentu; część punktów A (wiersz,
   pasek przewijania, lista rozwijana, pole wyszukiwania) zrobi już fundament (np. kreska i wiersz → filtry → karta). Po każdej
   porcji autor sprawdza w aplikacji, zanim ruszy następna; jego uwagi idą do briefu dosłownie.
   **Bez narzędzia podglądu dla wykonawcy** — autor wybrał częstsze sprawdzanie sam: taniej, a jego
   uwagi są lepszą informacją niż zdjęcia bez ekranu. Wymiary z CSS mockupu: brief przytacza
   selektory (`.entry`, `.entry.selected::before`, `.sidebar-item.active::before`, `.filter-chip`,
   `.filter-chip.on`, `.search`, `.clear-filters`, `.list-scroll`), nie każe czytać całego pliku.
5. **Punkt kontrolny: czy fundament rozwiązał problem — werdykt jawny, zapisany.** Po fundamencie
   i wdrożeniu go w aplikację (zakładka treści i karta potwora złożone z klocków) architekt z autorem
   **explicite uznają albo odrzucają** kierunek „Avalonia w obecnej formie". Problem, który
   rozwiązujemy: interfejs od wykonawców był fuszerką, a jego poprawianie zjadało większość budżetu
   sesji. Podstawa werdyktu, zmierzona skryptem `tools/subagent-usage.py` (napisany od nowa
   2026-09-24, bo pierwszy przepadł; na tych samych zapisach daje 131 kroków i 18,1 minuty dla
   typowego zlecenia z kodem od 22.09 — porównywać z tymi liczbami, nie z niżej przytoczonymi):
   * ile rund poprawek od autora potrzebowała każda porcja interfejsu (tabela *Pomiar porcji*
     w dokumencie zadania fundamentu; przy jego zamknięciu przechodzi tutaj),
   * czy wracają błędy rzemiosła (stany, fokus, wyrównanie, układ zależny od zawartości),
   * kroki, czas i odczyt na zlecenie interfejsu — wobec 2026-09-24: typowe zlecenie z kodem od 22.09
     to 124 kroki i 17,5 minuty, a trzy najdroższe przebiegi w historii projektu to widoki
     (najdroższy: 362 kroki, 53 minuty, 142 mln tokenów odczytu).
   Wynik idzie do `decisions.md`, do wpisu o technologii interfejsu (punkt 2): **uznany** — Avalonia
   zostaje z uzasadnieniem z liczb; **odrzucony** — to jest wyzwalacz próby z interfejsem w HTML-u na
   jednej zakładce. Autor: „chciałbym to na końcu nazwać […] i rozwiązać, czy zbudowanie tych
   fundamentów rozwiązało problem".

*A. Lista i filtry*
* ~~Kreska zaznaczenia wpisu przed wierszem, w odstępie~~ — zrobione w fundamencie (porcja 10:
  `ListRow` na liście wpisów i pasku bocznym).
* ~~Pasek przewijania tylko, gdy jest co przewijać~~ — zrobione w fundamencie (porcja 8b: nakładka).
* **Wysokość i szerokość wiersza nadaje biblioteka**, nie zawartość — dziś przedmioty (pigułka
  rzadkości) mają wyższe wiersze niż potwory.
* **Filtr = lista wartości z polami wyboru**, bez pozycji „Wszystkie"; nic nie zaznaczone = wszystko.
  Wewnątrz filtra „lub", między filtrami „i" (architekt). Chip reaguje na najechanie, a otwarty ma
  inne tło i obrócony chevron; chip z zaznaczeniem — styl `.filter-chip.on`. Lista: tło i obramowanie
  jak pole wyszukiwania, wiersze czcionką wierszy listy. Istniejące testy filtrów dostosować do nowej
  semantyki.
* „Wyczyść filtry" wygaszone, gdy nie ma czego czyścić.
* **Chipy filtrów na chip z listą** (`DropDownPicker` w stroju chipa — fundament, runda 1c): jedne
  reguły napisu (nazwa filtra / wartość / pierwsza + „+N”); pismo chipów z `ContentChipFontSize` (11,5
  — poza skalą pisma) na motyw.
* **Odwracanie sortowania** — klocek `SortPicker` (fundament, runda 1d) stoi w zakładce treści
  z wygaszonym przyciskiem kierunku (`CanReverse=False`): model widoku dostaje kierunek i rodzaj pól
  (`NumericOptions` — napis „A–Z” / „rosnąco” w menu i podpowiedzi).
* ~~Pole wyszukiwania: tekst zastępczy wyśrodkowany, fokus zdejmowany kliknięciem obok i Escape~~ —
  zrobione w porcji 2 fundamentu.

*B. Karta potwora*
* Wyzwanie — zwykły tekst krojem liczb, samo „1/2" (bez zakreślenia — `decisions.md`, *Wyzwanie bez
  zakreślenia*); PD obok, przygaszone. Tagi kategorii
  („Humanoid") — odznaka/tag z porcji 8. Reguła: `architecture.md`, *Konwencje interakcji*.
* Opisy, akcje i cechy szczególne stylem tekstu do zaznaczenia (porcja 2); powód błędu w miejscu karty
  także — *Konwencje interakcji* w `architecture.md`.
* Nagłówki „Cechy szczególne"/„Akcje" jasne i wyraźne jak w mockupie; „CECHY" wersalikami; styl
  etykiet cech; liczby KP/PZ/szybkości i cech wyróżnione; ikonki przy KP, PZ, szybkości.

*Rozstrzygnięte przez autora 2026-09-24:* **wyzwanie i PD to osobne pola w paczce** — dziś jeden napis
„1/2 (100 PD)"; odznaka i chip pokazują samo wyzwanie, PD osobno i przygaszone. Autor przerabia swoje
paczki po zmianie. Krój liczb — w fundamencie (typografia). Nazwy akcji, pogrubione premie
i nagłówki akcji z mockupu wymagają akcji jako osobnych rzeczy — krok 11.
**Cechy potwora — projekt autora 2026-09-24:** kwadratowa tabela z klocka tabeli, trzy kolumny,
sześć wierszy (po jednym na cechę), ostre narożniki; komórka modyfikatora malowana: dodatni — tło
zielone, ujemny — czerwone. Kolory **nie** z tokenów stanów (sukces/niebezpieczeństwo mają inne
znaczenie) — system dostaje własne tokeny „modyfikator dodatni"/„modyfikator ujemny" z zapisanym
znaczeniem, jak kolory rzadkości (propozycja architekta). Malowanie to prezentacja w skompilowanym
kodzie karty, nie wyrażenie w danych — pierwszego zakazu nie dotyczy.

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

* **Pola tekstowe (motyw ramy, `DungeonControls.axaml`, `Controls/IconField.cs`)** — uwagi autora po
  rundzie 1 porcji 2, 2026-09-25:
  * Zaznaczanie jak w przeglądarce: przeciągnięcie **rozpoczęte obok tekstu** (np. w pustym miejscu
    karty) i przeprowadzone nad tekstem do zaznaczenia ma go zaznaczać — dziś trzeba trafić w sam
    tekst. **Zabiera zlecenie B** (autor, 2026-09-26) i tam rozstrzyga zasięg (obszar wokół jednego
    bloku tekstu czy cała karta; zaznaczenie przez kilka bloków naraz to osobna, większa rzecz).
* **Suwak — tylko notka, bez korekty teraz** (autor, po rundzie 2 porcji 5, 2026-09-25): uchwyt
  w spoczynku (kolor tekstu drugorzędnego) lekko za ciemny. Pomysł autora na później: pod myszą
  obwódka wokół uchwytu zamiast rozjaśnienia — do zderzenia z wpisem o otoczce w `decisions.md`
  (otoczka tylko na przyciskach głównym i niszczącym, jej wygląd zarezerwowany dla fokusu klawiatury).

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
