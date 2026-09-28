# Zadanie: zakładki treści

Zakładki potworów i przedmiotów (i być może zaklęć) dostają nowy wygląd lista–szczegół złożony
z klocków fundamentu interfejsu: przeprojektowany szkielet zakładki z biblioteki wpisów i karty
projektowane per typ treści, bez ramy, gotowe do pokazania także w kampanii. Paczka dostarczana
z systemem dostaje kilkanaście wpisów na zakładkę, z których każdy pokazuje kartę z innej strony.
Zadanie kończy się jawnym werdyktem o Avalonii.

**Stan na: 2026-09-28, po `aa513fe`.** Na starcie sesji: `git log aa513fe..master` i `git worktree
list` — wszystko, co tam jest, a czego ten dokument nie wymienia, zdarzyło się poza nim.

*Dokument zadania — co to jest, jak go prowadzić i kiedy umiera: [collaboration.md](../collaboration.md),
*Dokument zadania*.*

---

## Gdzie stoimy

Porcja 3a (szkielet ekranu) z rundą W (rama szczegółu, ścieżka, odstępy, licznik, wygaszanie
„Wyczyść filtry”) scalona 2026-09-28 — **czeka na obejrzenie przez autora**. Kopii roboczych brak.
**Następny krok:** uwagi autora (kolejna runda do tabeli *Pomiar porcji*) albo przyjęcie 3a, potem
brief porcji 3 — w nim notki „Po porcji 3a”.

## Zakres i koniec

**Wchodzi** (krok 10 z kolejki, zakres z 2026-09-24, poszerzony przez autora 2026-09-26):
* **Zakładki potworów i przedmiotów** w kategorii System, z jednego szkieletu biblioteki wpisów:
  wyszukiwanie po nazwie; sortowanie po nazwie i po kluczu systemu, z odwracaniem; filtry po kategorii,
  po paczce i po wartości wskazanej przez system; czyszczenie filtrów; odznaka wiersza; licznik wpisów;
  nagłówek szczegółu (kategoria, nazwa, tagi) rysowany przez szkielet, pod nim karta systemu; sekcje po
  paczce z rzeczami zepsutymi na dole; powód błędu w miejscu karty; przycisk wczytania paczek od nowa.
* **Nowy projekt wyglądu lista–szczegół** (autor, 2026-09-26): praca przede wszystkim projektowa
  i twórcza — pełna swoboda w obrębie koncepcji aplikacji i sensu treści; mockup rejestru
  (`docs/images/mockup_rejestr.png`, `mockup_rejestr.html`) jako luźne odniesienie; składane z klocków
  fundamentu.
* **Sprawdzenie, co D&D 5e przechowuje** przy potworze, przedmiocie i zaklęciu — zrobione
  (wycinek 1, *Ustalenia*, *Rekordy*).
* **Wpisy przykładowe:** kilka–kilkanaście na zakładkę, każdy pokazuje kartę z innej strony (np. potwór
  z czarami, z akcjami legendarnymi, z odpornościami, bez akcji; przedmiot magiczny z ładunkami, zwykły
  ekwipunek, broń).
* **Zaklęcia — „ewentualnie”** (autor): nowy typ treści i zakładka; decyzja przy etapie; odczyt drugiego
  zakazu dla zaklęć — *Ustalenia*.
* **Punkt kontrolny o Avalonii** (*Ustalenia*).

**Nie wchodzi:** tworzenie, edycja, kopiowanie, usuwanie wpisów; NPC; filtr po typie treści (bez
zakładki z kilkoma typami nie ma konsumenta); pasek stanu i Ctrl+K z mockupu; akcje jako osobne rzeczy
z formułami (krok 11 — *Notki*); dodanie paczki przeciągnięciem (osobny etap w `tasks.md`).

**Koniec:** zakładki potworów i przedmiotów (i zaklęć, jeśli weszły) przyjęte przez autora, wpisy
przykładowe w paczce, werdykt o Avalonii zapisany w `decisions.md`. Wtedy ten dokument umiera.

## Do przeczytania

Na starcie:
* [architecture.md](../architecture.md), *Zakładki treści* — docelowy kształt zakładek.
* [architecture.md](../architecture.md), *Niezmiennik interfejsu* — reguły, którym podlega każdy widok.

Przed projektem (wycinek 2) — materiał projektanta, nic poza nim (*Ustalenia*, *Projekt*):
* `docs/images/mockup_rejestr.html` (wymiary) i `mockup_rejestr.png` — luźna inspiracja.
* Galeria kontrolek — klocki fundamentu: `src/DungeonApp.Desktop/Shell/Gallery/Sections/`, wzór listy
  z filtrami i karty w `CompositionsSection.axaml`. **Nie** widoki zakładki treści ani kart systemu.

Przed briefem zmieniającym rekord (porcje 4, 5, 8) i przed porcją 3b:
* [architecture.md](../architecture.md), *Deklaracja treści* i *Granica automatyzacji* — co wolno
  w rekordzie; pięć zakazów w `CLAUDE.md`.
* [architecture.md](../architecture.md), *Paczki, wczytywanie, bezpieczeństwo* — paczka dostarczana,
  pliki obrazów w paczce (porcja 3b i pole `image`).
* [code-state.md](../code-state.md), *Punkty rozszerzeń*, *Nowy typ treści* i *Nowa zakładka systemu*
  (zaklęcia).

Przed każdym briefem:
* [code-state.md](../code-state.md), *Pułapki* — zwłaszcza motyw ramy, okienka, pasek przewijania.
* [tasks.md](../tasks.md), *Poprawki czekające na obszar*.
* `.claude/agents/wykonawca.md`, *Rzemiosło interfejsu* — żeby brief jej nie powtarzał.

Tylko gdy potrzeba:
* [decisions.md](../decisions.md), *Nawigacja i interfejs* (część B) i *Niezmiennik interfejsu* —
  zanim zaproponujesz zmianę konwencji.

**Na starcie nie czytaj** całej architektury, rejestru decyzji ani reszty kolejki.

## Plan

Wycinki w kolejności; każdy z osobnym zielonym światłem. Porcja wykonawcy = 20 minut.

- [x] **0** — fundament obejrzany i zamknięty 2026-09-26.
- [x] **1** — dane D&D 5e: rekordy przyjęte 2026-09-26 (*Ustalenia*, *Rekordy*).
- [x] **2** — projekt szkieletu i kart przyjęty 2026-09-28 (*Ustalenia*, *C*).
- [x] **3a** — szkielet ekranu, klocek szczegółu `EntryDetailView` (scalone 2026-09-28; czeka na
  obejrzenie).
- [ ] **3 — lista i filtry** (*Ustalenia*, *A*, *C*): chipy, nazwa chipa kategorii od systemu,
  semantyka filtrów, „Wyczyść filtry”, sortowanie z odwracaniem.
- [ ] **3b — paczka dostarczana z systemem** (*Ustalenia*): drugie źródło wczytywania, kopiowanie
  paczki obok programu, pusta paczka przykładowa z przypisaniem SRD; test formatu — zero odrzuceń.
- [ ] **4a — obrazek wpisu** (*Projekt*, *C*): ramka obrazka w bibliotece i w galerii, pole `image`
  u potwora i przedmiotu, odrzucenie ścieżki poza paczkę z testem, brak pliku.
- [ ] **4 — karta potwora** (*Ustalenia*, *B*, *C*) — z polami potwora z *Rekordów*. Jeśli nie zmieści
  się w porcji: górny blok z cechami i tokenami, potem pary i sekcje z nowymi polami.
- [ ] **5 — karta przedmiotu** (*C*) — z polami przedmiotu z *Rekordów*.
- [ ] **6 — wpisy przykładowe** w paczce dostarczanej — może iść razem z 4 i 5 (wpis pokazuje
  wariant karty, który porcja buduje).
- [ ] **7 — wczytanie paczek od nowa**: zwiad, jak żyje rejestr (kto go trzyma, kto dostaje przy
  zakładkach i biurku), potem zlecenie. Przesłankę sprawdzić przed zwiadem.
- [ ] **8 — zaklęcia** (decyzja przy etapie).
- [ ] **9 — punkt kontrolny** — werdykt o Avalonii.

**Pomiar porcji** — materiał do werdyktu. „Rundy" to poprawki od autora, każda oznaczona: **R** — błąd
rzemiosła albo błąd kontrolki fundamentu, **W** — zmiana wymagania po obejrzeniu. „Pomiar" — kroki /
minuty / odczyt z `tools/subagent-usage.py`, suma przebiegów porcji.

| # | Porcja | Rundy | Pomiar |
|---|---|---|---|
| 3a | szkielet ekranu | 1 R — zakładka się nie otwierała (zasób złego typu w widoku, nie kontrolka fundamentu; poprawił architekt z testem budującym widok); 1 W — rama szczegółu, ścieżka, odstęp pierwszej sekcji, licznik, „Wyczyść filtry” wygaszane (przeniesione z porcji 3) | 49 kroków / 10,3 min / 5,5 mln; runda W: 42 / 7,6 / 4,0 mln |

## Ustalenia

* **Punkt kontrolny: czy fundament rozwiązał problem** — autor, 2026-09-24; próg — autor, 2026-09-26.
  Po fundamencie i wdrożeniu go w aplikację architekt z autorem **explicite uznają albo odrzucają**
  kierunek „Avalonia w obecnej formie". Problem: interfejs od wykonawców był fuszerką, a jego poprawianie
  zjadało większość budżetu sesji. Autor: „chciałbym to na końcu nazwać […] i rozwiązać, czy zbudowanie
  tych fundamentów rozwiązało problem".
  * **Próg „uznany”** (ustalony przed pierwszą porcją, żeby nie dopasowywać go do wyniku): porcja
    ekranu przyjęta po najwyżej jednej rundzie; żadna runda **R** w kontrolce z fundamentu; żaden
    przebieg ponad limit 20 minut. Rundy **W** nie świadczą przeciw technologii.
  * **Odniesienie** (`tools/subagent-usage.py`, na zapisach od 22.09): typowe zlecenie z kodem — 124–131
    kroków, 17,5–18,1 minuty; trzy najdroższe przebiegi w historii to widoki (najdroższy: 362 kroki,
    53 minuty, 142 mln odczytu). Fundament: przebiegi 40–70 kroków, 6–14 minut — **ale** porcje są celowo
    małe (limit 20 minut od 2026-09-24), więc spadek na przebieg nie jest sam dowodem; tabela fundamentu
    (`decisions.md`, pozycja 39, *Pomiar fundamentu*) rund nie dzieli na R i W.
  * Wynik → `decisions.md`, wpis o interfejsie w HTML-u (część B, *Nawigacja i interfejs*): **uznany** —
    Avalonia zostaje z uzasadnieniem z liczb; **odrzucony** — wyzwalacz próby z interfejsem w HTML-u na
    jednej zakładce. Otwarte błędy okienek widoczne tylko w aplikacji (fundament) wchodzą do werdyktu,
    jeśli okażą się ograniczeniem biblioteki, nie naszym błędem.
* **Projektuje architekt, buduje wykonawca** (architekt, 2026-09-26; `collaboration.md`, *Deleguj kod,
  nie decyzje*): układ list i kart to decyzja — architekt proponuje ją na piśmie, autor przyjmuje,
  wykonawca składa z klocków. Swoboda twórcza (autor) dotyczy projektu, nie pominięcia zgody na etap.
* **„Instancje” z polecenia autora to wpisy** (autor, 2026-09-26): pozycje w paczce, nie egzemplarze
  w kampanii (`architecture.md`, *Słownik*).
* **Drugi zakaz czyta się według intencji** (autor, 2026-09-26, na propozycję architekta): czas
  rzucania, zasięg, czas trwania, „3/dzień”, „odnawia się o świcie” mogą być polami rekordu, ale
  wyłącznie jako napisy, które pokazuje karta. Nic ich nie rozbiera, nie liczy, nie filtruje ani nie
  sortuje. Filtry i sortowanie zaklęć — po poziomie i szkole. **Koncentracja i rytuał to tagi
  zaklęcia** (właściwości, nie odmierzanie czasu). Brzmienie zakazu w `CLAUDE.md` zmienione
  tego dnia za zgodą autora; uzasadnienie — `decisions.md`, pozycja 40.
* **Wierność D&D 5e nie jest celem** (autor, 2026-09-26): SRD to punkt wyjścia, nie wzorzec 1:1 —
  pole wchodzi, gdy pomaga MG. Przykład autora: **każdy przedmiot ma rzadkość** (miara elitarności
  i cenności), także zwykły ekwipunek — rzadkość zostaje wymagana.
* **Rekordy** (architekt, przyjęte przez autora 2026-09-26). Wszystko addytywne: nowe pola
  opcjonalne, nic usuniętego ani przemianowanego, bez podniesienia wersji typu (`architecture.md`,
  *Wersjonowanie*); wszystko płaskie — napisy, liczby, znaczniki (`decisions.md`, pozycja 7). Pola
  wchodzą z porcją karty, która je pokazuje. Czas — wyłącznie napis czytany przez kartę.
  * **Potwór:** `xp` (liczba; wyzwanie zostaje samym napisem „1/2”); rzuty obronne; podatności,
    odporności, niewrażliwości na obrażenia i na stany; akcje dodatkowe, reakcje, akcje legendarne
    („3 na turę” w tekście); rzucanie czarów — osobne bloki prozy.
  * **Przedmiot — jeden typ dla zwykłego i magicznego:** kategoria (filtr kategorii listy: „Broń”,
    „Zbroja”, „Mikstura”, „Cudowny przedmiot”, „Ekwipunek”…), podtyp („dowolny miecz”), dostrojenie
    (napis; filtr wymaga / nie wymaga), cena (napis), waga z ułamkami (dziś liczba całkowita),
    obrażenia i właściwości broni (napisy), KP zbroi (napis), wymagana Siła, utrudnienie do Ukrywania
    się, ładunki (liczba) i odnawianie (napis). Sekcje broni i zbroi na karcie — gdy pola wypełnione.
  * **Obrazek** (`image`) u potwora i przedmiotu — *Projekt*.
  * **Zaklęcie** (jeśli wejdzie): poziom (0 = sztuczka), szkoła, czas rzucania, zasięg, komponenty,
    materiały, czas trwania, opis, na wyższych poziomach, klasy (napis); tagi koncentracja i rytuał;
    filtry poziom i szkoła; sortowanie nazwa i poziom. Filtr po klasie — decyzja o formacie przy
    etapie 8.
* **Wpisy przykładowe w paczce dostarczanej z systemem** (autor, 2026-09-26): mechanizm zadeklarowany
  w `architecture.md`, *Paczki, wczytywanie, bezpieczeństwo* — obok programu, tylko do odczytu, ten sam
  loader i rejestr, logika nie rozróżnia źródła. W kodzie go nie ma (`code-state.md`, *Dług*) — porcja
  3b. Paczka niesie przypisanie SRD (CC-BY 4.0).
* **SRD to wyłącznie dane testowe** (autor, 2026-09-26) — źródło wpisów przykładowych, nie treść
  traktowana na stałe ani wzorzec dla rekordów.
* **Projekt — decyzje autora z 2026-09-26** (część na rekomendację architekta, przyjęte w całości):
  * **Szkielet zakładki też przechodzi przeprojektowanie** — lista, filtry, nagłówek szczegółu.
    Szkielet należy do biblioteki i nie wie nic o D&D.
  * **Karta bez ramy, o stałej szerokości** z biblioteki wpisów, z wysokością z treści. Rama,
    przewijanie i najmniejsza wysokość należą do tego, kto kartę pokazuje (reguła — `architecture.md`,
    *Zakładki treści*). Szerokość ustala projekt z wycinka 2. Karta ma być czytelna w okienku podglądu
    w kampanii, nie tylko w zakładce.
  * **Obrazek:** przedmiot — kwadratowe pole w lewym górnym rogu; potwór — pole portretowe (pionowy
    prostokąt); zaklęcie — bez obrazka. Bez pliku pole pokazuje ramkę zastępczą z dużą wyszarzoną
    ikoną typu. Ramka jest wspólnym klockiem kart (dwa użycia od początku). Pole rekordu `image`
    (opcjonalne) u potwora i przedmiotu wskazuje plik względem katalogu paczki. Ścieżka poza paczkę
    lub bezwzględna odrzuca wpis z powodem. Brak pliku daje ramkę zastępczą z informacją o brakującym
    pliku w kolorze niebezpieczeństwa. Formaty: PNG, JPG, WebP. Wpisy przykładowe są bez obrazków.
  * **Ikony zastępcze** z zestawu ikon motywu, a gdy go brak — z zestawu na licencji niewymagającej
    przypisania (MIT, ISC). Jedna ikona na typ treści.
  * **Kontrolki na karcie — tylko widokowe.** Sekcje rozwijane tam, gdzie treść jest długa (akcje,
    akcje legendarne, opis, rzucanie czarów). Domyślnie rozwinięte, stan niezapisywany; zaznaczanie
    tekstu jak dotąd. Nic na karcie nie zapisuje.
  * **Bez makiety HTML** — od razu w aplikacji. Przed kodem architekt daje autorowi projekt na piśmie:
    co gdzie stoi, co wyróżnione, szkic układu tekstem. Autor przyjmuje, wykonawca składa z klocków.
  * **Projektant nie ogląda obecnych widoków zakładki ani kart** — ani kodu, ani opisu ich wyglądu.
    Materiał projektu: `mockup_rejestr.png`/`.html` (luźna inspiracja — „wygląda ładnie i dobrze”),
    klocki fundamentu (galeria kontrolek, *Kompozycje*), *Niezmiennik interfejsu*, *Rekordy*,
    wymagania z *A* i *B* niżej. Brief wykonawcy: stary widok jest do zastąpienia, nie do
    przerabiania.
* **Treść przykładowa z SRD albo własna** (architekt, 2026-09-26): SRD 5.1/5.2 jest na licencji CC-BY
  4.0 — paczka niesie przypisanie autorstwa; nazwy i opisy tłumaczymy sami. Nie przepisujemy potworów
  ani przedmiotów spoza SRD.
* **Wymiary z `docs/images/mockup_rejestr.html`**, nie z obrazka — tam, gdzie projekt z wycinka 2
  bierze coś z mockupu. Pasek tytułu okna w mockupie nie jest projektem paska górnego. **Mockup jest
  prawie kwadratowy** — na ekranie 16:9 lista trzyma szerokość z mockupu, karta swoją stałą
  szerokość (*Projekt*), wyrównana do listy.
* **Kolory po znaczeniu:** treść niewczytana bierze kolor „niebezpieczeństwa"; kolory rzadkości
  i modyfikatorów — własne tokeny w systemie.
* **A. Lista i filtry** (z kolejki; część zrobił fundament):
  * **Wysokość i szerokość wiersza nadaje biblioteka**, nie zawartość — dziś przedmioty (pigułka
    rzadkości) mają wyższe wiersze niż potwory.
  * **Filtr = lista wartości z polami wyboru**, bez pozycji „Wszystkie"; nic nie zaznaczone = wszystko.
    Wewnątrz filtra „lub", między filtrami „i" (architekt). Istniejące testy filtrów dostosować do nowej
    semantyki.
  * **Chipy filtrów na chip z listą** (`DropDownPicker` w stroju chipa — fundament, runda 1c): jedne
    reguły napisu (nazwa filtra / wartość / pierwsza + „+N”); pismo chipów z `ContentChipFontSize`
    (11,5 — poza skalą pisma) na motyw. Chip „Kolor” w galerii (`DungeonChipOpener` z `ListBox`) nie
    odznacza — przechodzi tu razem z chipami.
  * „Wyczyść filtry" wygaszone, gdy nie ma czego czyścić.
  * **Odwracanie sortowania** — klocek `SortPicker` stoi w zakładce z wygaszonym przyciskiem kierunku
    (`CanReverse=False`): model widoku dostaje kierunek i rodzaj pól (`NumericOptions` — napis „A–Z” /
    „rosnąco” w menu i podpowiedzi).
* **B. Karta potwora** (z kolejki):
  * Wyzwanie — zwykły tekst krojem liczb, samo „1/2" (bez zakreślenia — `decisions.md`, *Wyzwanie bez
    zakreślenia*); PD obok, przygaszone. Tagi kategorii („Humanoid") — tag z fundamentu.
  * Opisy, akcje i cechy szczególne stylem tekstu do zaznaczenia; powód błędu w miejscu karty także.
  * Nagłówki „Cechy szczególne"/„Akcje" jasne i wyraźne; liczby KP/PZ/szybkości i cech wyróżnione;
    ikonki przy KP, PZ, szybkości — punkt wyjścia, projekt z wycinka 2 może to zmienić.
  * **Wyzwanie i PD to osobne pola w paczce** (autor, 2026-09-24) — dziś jeden napis „1/2 (100 PD)";
    odznaka i chip pokazują samo wyzwanie. Autor przerabia swoje paczki po zmianie.
  * **Cechy potwora — projekt autora 2026-09-24, doprecyzowany 2026-09-26:** dwie tabele z klocka
    tabeli obok siebie z odstępem: SIŁ, ZRC, KON — INT, MDR, CHA. Każda ma trzy wiersze i trzy kolumny:
    skrót cechy, wartość (np. 20), modyfikator (np. +5). Wszystkie komórki kwadratowe (szerokość =
    wysokość), ostre narożniki (wzór: galeria, `AbilityScoresSample`). Komórka modyfikatora dodatniego
    ma zielone tło, ujemnego czerwone. Pozostałe komórki, także modyfikator +0, mają neutralne szare.
    Kolory **nie** z tokenów stanów — system dostaje własne tokeny „modyfikator dodatni"/„modyfikator ujemny" z zapisanym znaczeniem. Malowanie
    to prezentacja w kodzie karty, nie wyrażenie w danych — pierwszego zakazu nie dotyczy.
* **C. Projekt szkieletu i kart (wycinek 2)** — propozycja architekta z 2026-09-28, **przyjęta przez
  autora w całości tego dnia** („zobaczymy, jak to wyjdzie” — zmiany po obejrzeniu idą jako rundy W).
  Wiąże briefy porcji 3–5 i 8. Wymiary ze skali motywu
  (odstępy 6, 8, 12, 16, 20, 24, 32), pismo z ról galerii (*Typografia*), klocki z galerii.
  ```
  ┌──────────────────────────────────────────────────────────────────────────────────┐
  │ Potwory  12 z 37 wpisów                                      [⟳ Wczytaj od nowa] │
  ├───────────────────────────────┬──────────────────────────────────────────────────┤
  │ [⌕ Szukaj po nazwie…        ] │   GOBLINOIDY · BESTIARIUSZ SRD                   │
  │ [Grupa ▾] [Typ ▾]             │   Goblin                                         │
  │ [Wyzwanie ▾] [Paczka ▾]       │   (Mały) (humanoid (goblinoid)) (neutralny zły)  │
  │ Sortuj: Nazwa A–Z ⇅  Wyczyść… │                                                  │
  ├───────────────────────────────┤   karta systemu, 560 szerokości,                 │
  │ BESTIARIUSZ SRD  9            │   wysokość z treści                              │
  │▌Goblin                   1/4  │                                                  │
  │ Hobgoblin                1/2  │                                                  │
  │ zepsuty-wpis.json          ⚠  │                                                  │
  │ MOJA PACZKA  3                │                                                  │
  │ …                             │                                                  │
  │ ⚠ ODRZUCONA PACZKA            │                                                  │
  │ stara-paczka                  │                                                  │
  └───────────────────────────────┴──────────────────────────────────────────────────┘
    320                            32 + 560, reszta szerokości pusta
  ```
  * **Nagłówek zakładki** (biblioteka): nazwa zakładki rolą `display-lg`, obok na linii bazowej
    licznik, przygaszony — liczby krojem liczb, słowa pismem interfejsu (po obejrzeniu 3a) — „37 wpisów”, przy zawężeniu „12 z 37 wpisów” (odmiana wpis /
    wpisy / wpisów). Z prawej przycisk „Wczytaj od nowa” z ikoną odświeżania i podpowiedzią „Wczytuje
    paczki z dysku od nowa” — staje z porcją 7, wcześniej go nie ma. Odstępy 20 u góry, 24 po bokach,
    16 nad linią oddzielającą nagłówek od ciała. Paska typów z mockupu nie ma — zakładka ma jeden typ.
  * **Panel listy** — stała szerokość 320, pełna wysokość, pionowa linia od szczegółu.
    * Kontrolki (odstęp 16 wokół, 8 między rzędami): pole wyszukiwania z ikoną; zawijany rząd chipów
      z listą; rząd — wybór sortowania z lewej, „Wyczyść filtry” z prawej; pod nimi linia.
    * Chipy w kolejności: kategoria, filtry systemu, Paczka. **Nazwę chipa kategorii podaje system**:
      potwór „Grupa” (pole `group`), przedmiot „Kategoria”, zaklęcie „Szkoła”. Filtry systemu: potwór
      — Typ, Wyzwanie; przedmiot — Rzadkość, Dostrojenie (wymaga / nie wymaga); zaklęcie — Poziom.
    * „Wyczyść filtry” zeruje chipy **i** wyszukiwanie; wygaszone, gdy nic nie zawęża listy.
    * Sortowanie: Nazwa (domyślne, A–Z) i klucz systemu — potwór Wyzwanie, przedmiot Rzadkość,
      zaklęcie Poziom; z odwracaniem. Sortuje wewnątrz sekcji; rzeczy zepsute zawsze na dole sekcji.
    * Sekcje po paczce, w kolejności nazw paczek, odrzucone na końcu. Nagłówek rolą `overline-sm`,
      przygaszony, obok liczba wpisów sekcji krojem liczb; 12 nad każdym nagłówkiem,
      także pierwszym (po obejrzeniu 3a: 4 kleiło go do linii kontrolek), 6 pod.
    * Wiersz — wiersz-polecenie z kreską zaznaczenia (galeria, *Listy*), wysokość 32 ze stałej
      biblioteki, niezależna od odznaki. Nazwa rolą `body`, przycinana wielokropkiem, pełna
      w podpowiedzi; z prawej odznaka wiersza. Ikony typu z mockupu nie ma — w zakładce jednego typu
      nic nie mówi.
    * Odznaka wiersza (podaje system): potwór — wyzwanie, zwykły tekst krojem liczb, przygaszony;
      przedmiot — odznaka rzadkości w kolorze z systemu; zaklęcie — „Sztuczka” / „Poz. 3” krojem liczb,
      przygaszone. Kolory rzadkości: 4,5:1 także na tle wybranego wiersza, żaden nie jest akcentem
      (*Notki*, odznaka akcentu).
    * Rzeczy zepsute: nazwa (albo nazwa pliku, gdy nazwy nie odczytano) kolorem treści niewczytanej,
      w miejscu odznaki ikona ostrzeżenia tym samym kolorem. Odrzucona paczka — ostatnia sekcja: jej
      nagłówek kolorem treści niewczytanej z ikoną ostrzeżenia, pod nim **jeden wiersz** z nazwą
      katalogu paczki, wybieralny jak każdy — pokazuje powód. Architektura mówi „nagłówek bez wpisów”;
      wiersz jest potrzebny, bo nagłówek sekcji się nie wybiera, a zamieniony w przycisk łamałby
      konwencję listy.
    * Puste stany (klocek pustej listy): brak wpisów tego typu w żadnej paczce — „Żadna paczka nie ma
      jeszcze potworów.” z podpowiedzią, gdzie leżą paczki; nic nie pasuje — „Nic nie pasuje do
      wyszukiwania i filtrów.” z odnośnikiem „Wyczyść filtry” (galeria, *Kompozycje*).
  * **Obszar szczegółu** — przewija się pionowo, jedyna warstwa przewijania po tej stronie; odstęp 32
    u góry, z lewej i u dołu. Szczegół stoi przy liście; szerszy ekran daje pusty pas z prawej, nie
    szerszą kartę. **Szczegół stoi w ramie zakładki** (autor po obejrzeniu 3a — ramę daje host, a
    projekt jej nie dał): tło powierzchni, linia motywu, narożnik karty, wcięcie 24 — szerokość z klocka: 560 + 2 × 24 + linia =
    610, wysokość z treści. Przy najmniejszym oknie mieści się bez przewijania w poziomie: 224 (pasek
    boczny) + 320 + 32 + 610 + 32 = 1218 z 1280.
    * Nic nie wybrane — pusty stan „Wybierz wpis z listy.” w miejscu nagłówka. Na starcie zakładki nic
      nie jest wybrane. Wybrany wpis, który zniknie z listy po zmianie filtra, zdejmuje wybór. Po
      wczytaniu od nowa wybór zostaje, jeśli wpis o tym id nadal jest.
  * **Szczegół wpisu = nagłówek biblioteki + karta systemu, jeden klocek biblioteki, 560 szerokości.**
    Stawia go każdy, kto pokazuje wpis: zakładka treści i przyszłe okienko podglądu w kampanii (560
    + 2 × 24). Dzięki temu nagłówek rysuje szkielet, a ta sama karta stoi wszędzie razem z nazwą.
    * Nagłówek: **ścieżka** (kontrolka ścieżki z fundamentu) „{Typ} › {Kategoria} › {Nazwa}” — typ to
      tytuł zakładki, bez kategorii dwa segmenty; paczki w nagłówku nie ma, widać ją w sekcji listy
      (autor po obejrzeniu 3a, 2026-09-28, zamiast linii „{KATEGORIA} · {PACZKA}”); nazwa rolą `display-xl`, zawijana, nie przycinana; rząd tagów, zawijany, odstęp 6 —
      nie ma go, gdy tagów brak. Odstęp 6 wewnątrz nagłówka, 20 do karty. Kategoria nie akcentem, jak
      w mockupie — akcent jest dla wybranego i głównego.
    * Tagi: potwór — rozmiar, typ, charakter; przedmiot — brak; zaklęcie — Koncentracja, Rytuał.
    * Rzecz zepsuta w miejscu karty: linia „WPIS NIEWCZYTANY · {PACZKA}” (odrzucona paczka: „PACZKA
      ODRZUCONA”) kolorem treści niewczytanej, nazwa jak na liście, pod nią powód tekstem do
      zaznaczenia i ścieżka pliku rolą `caption`, przygaszona.
  * **Wspólne klocki kart.** Ramka obrazka — klocek biblioteki wpisów (zna pojęcie obrazka wpisu);
    ikonę typu podaje system. Rozmiar daje karta; linia motywu, narożnik karty, tło powierzchni sekcji;
    obrazek wypełnia ramkę przycięty do proporcji, bez zniekształcenia. Bez pliku — duża (48)
    wyszarzona ikona typu na środku: potwór smok, przedmiot plecak (obie są w motywie). Brak pliku —
    ta sama ikona, pod nią rolą `caption` kolorem niebezpieczeństwa „Brak pliku” i ścieżka z wpisu.
    Poza ramką karty składają się z istniejących klocków, bez nowych:
    * para etykieta–wartość — kolumna etykiet o stałej szerokości podanej przez kartę, etykieta rolą
      `label`, drugorzędna, wartość `body`, zawijana; 6 między parami; rząd tylko dla wypełnionego pola;
    * sekcja prozy — sekcja rozwijana z motywu, domyślnie rozwinięta, treść tekstem do zaznaczenia,
      akapity jak w danych; tylko dla niepustego pola;
    * linia sekcji (`Separator` odmiany sekcji) między blokami karty, 16 nad i pod.
  * **Karta potwora.**
    ```
    ┌──────────┐  KP          15  skórzana zbroja, tarcza
    │          │  PZ           7  2k6
    │ portret  │  Szybkość    9 m, wspinaczka 9 m
    │ 168×224  │
    │          │  ┌───┬───┬───┐   ┌───┬───┬───┐
    │          │  │SIŁ│  8│ −1│   │INT│ 10│ +0│
    │          │  │ZRC│ 14│ +2│   │MDR│  8│ −1│
    └──────────┘  │KON│ 10│ +0│   │CHA│  8│ −1│
                  └───┴───┴───┘   └───┴───┴───┘
    ─────────────────────────────────────────────────────────
    Rzuty obronne               Zrc +4, Mdr +2
    Umiejętności                Skradanie się +6
    Podatność na obrażenia      …
    Odporność na obrażenia      …
    Niewrażliwość na obrażenia  …
    Niewrażliwość na stany      …
    Zmysły                      widzenie w ciemności 18 m, bierna Percepcja 9
    Języki                      wspólny, goblini
    Wyzwanie                    1/4  50 PD
    ─────────────────────────────────────────────────────────
    ▾ Cechy szczególne   ▾ Akcje   ▾ Rzucanie czarów   ▾ Akcje dodatkowe
    ▾ Reakcje   ▾ Akcje legendarne   ▾ Opis            (jedna pod drugą)
    ```
    * Górny blok: portret 168 × 224 (3:4) z lewej, 24 odstępu, prawa kolumna (368) od góry. KP, PZ,
      szybkość: etykiety `label` w kolumnie 72, wartości rolą `mono-value` (wyróżnione), dopiski
      (źródło KP, kości PZ) `caption` przygaszone; szybkość to napis i się zawija. Bez ikonek przy KP,
      PZ, szybkości — etykieta mówi to samo, a obok stoi ikona portretu. Aktualne PZ z nakładki
      instancji (w kampanii) — „5 / 7”.
    * Cechy: dwie tabele 3 × 3 z komórek 40 × 40 (*B*), 24 odstępu — razem 264; 16 pod wartościami.
    * Pary: kolumna etykiet 168 — „Niewrażliwość na obrażenia” mieści się w jednej linii. Kolejność
      jak na szkicu; Zmysły i Wyzwanie zawsze. Wyzwanie krojem liczb, PD obok, `caption` przygaszone.
    * Sekcje prozy w kolejności ze szkicu. Kolorowych kresek przy nagłówkach z mockupu nie ma —
      czerwień przy „Akcjach” mówiłaby „niebezpieczeństwo”.
    * Opis rolą `display-prose` — kursywa kroju nagłówków, jak w mockupie: u potwora to tekst
      klimatu, nie zasady.
  * **Karta przedmiotu.**
    ```
    ┌────────┐  Rzadkość       Rzadki                  (kolor rzadkości)
    │ 128 ×  │  Podtyp         dowolny miecz
    │ 128    │  Dostrojenie    wymaga dostrojenia przez paladyna
    └────────┘  Cena           500 sz
                Waga (lb)      3
                Ładunki        7  odnawia 1k6+1 o świcie
    ─────────────────────────────────────────────────────────
    BROŃ
    Obrażenia      1k8 cięte
    Właściwości    uniwersalna (1k10)
    ─────────────────────────────────────────────────────────
    ZBROJA
    KP             14 + mod. Zr (maks. 2)
    Wymagana Siła  13
    Ukrywanie się  utrudnienie
    ─────────────────────────────────────────────────────────
    ▾ Opis
    ```
    * Górny blok: kwadrat 128 × 128 w lewym górnym rogu, 24 odstępu, pary z kolumną etykiet 112;
      kwadrat od góry, pary mogą być wyższe od niego.
    * Rzadkość zawsze — `body-strong` tekstową odmianą koloru rzadkości; reszta, gdy wypełniona. Waga
      z przecinkiem dziesiętnym, jednostka w etykiecie, bo wartość to liczba. Cena i ładunki krojem
      liczb; odnawianie obok ładunków, `caption` przygaszone, tak jak zapisane.
    * Broń i Zbroja — gdy któreś z ich pól wypełnione; nagłówek `overline-sm` przygaszony, sekcje
      nierozwijane, bo krótkie.
    * Opis — sekcja rozwijana, `body` do zaznaczenia: u przedmiotu to zasady, nie klimat.
    * Bez tagów: rzadkość i dostrojenie stoją w parach, tag by je powtórzył.
  * **Karta zaklęcia** (szkic; rozstrzyga etap 8): bez obrazka. Pary z kolumną 112: Poziom
    („Sztuczka” / „3”), Czas rzucania, Zasięg, Komponenty (materiały pod spodem, `caption`), Czas
    trwania, Klasy — wszystkie napisami; potem sekcje rozwijane Opis i Na wyższych poziomach (`body`).
  * **Czego projekt dokłada do motywu i systemu:** ikona odświeżania (porcja 7); w systemie kolory
    rzadkości w dwóch odmianach (tło odznaki, tekst na karcie) i modyfikatory (*B*). Kolor treści
    niewczytanej już jest. Akcje jako osobne rzeczy (krok 11) nie są potrzebne — akcje zostają prozą.
  * **Rozstrzygnięcia przy briefie 3a** (architekt, 2026-09-28; do weta autora):
    * Wyszukiwanie, filtry i sortowanie przechodzą w 3a do nowego panelu bez zmiany działania;
      przeprojektowanie ich — porcja 3.
    * Licznik zakładki i liczby sekcji liczą wpisy wczytane i zepsute; wiersz odrzuconej paczki nie
      jest wpisem i się nie liczy.
    * Zdanie pustego stanu z nazwą typu („Żadna paczka nie ma jeszcze potworów.”) podaje system —
      biblioteka nie zna nazw typów.
    * Kontrast odznaki rzadkości sprawdza 3a, bo wiersz należy do niej: tekst odznaki co najmniej
      4,5:1 na odznace położonej na tle wiersza w spoczynku, pod myszą i wybranego; poprawka
      w kolorach systemu.

## Notki

* **Akcje jako osobne rzeczy to krok 11** (kolejka, 2026-09-24): nazwy akcji, pogrubione premie
  i nagłówki akcji z mockupu wymagają akcji jako osobnych pozycji rekordu, a formuły i sloty przyszły
  po kroku 10. Jeśli projekt z wycinka 2 ich potrzebuje — *Do sesji głównej* (kolejność kolejki).
* **Wpis JSON nie zmienia formatu ani o znak** (`collaboration.md`, *Warstwa treści*): koperta wpisu
  stoi; zmieniają się najwyżej pola `values` rekordu typu (tak jak wyzwanie i PD). Sprawdzić
  z *Deklaracją treści* przed propozycją zmian.
* **Zwiad danych, 2026-09-26** (39 kroków, 4 min) — do briefów porcji 3b–5:
  * Deserializacja `values` ścisła, ale **`"rarity": null` przechodzi** (brak `RespectNullableAnnotations`);
    podejrzenie, niemierzone: wysypie zakładkę przy kolorze odznaki. Porcja z rekordami potwierdza
    testem i poprawia.
  * Nowe pole `required` psuje kompilację testów budujących `new Monster{…}`/`new Gear{…}`
    (`ContentTabsTests`, `CampaignInstancesToolViewModelTests`) — dlatego pola opcjonalne.
  * Komentarz `Monster.cs:11` wskazuje nieistniejący plik szablonu — poprawić przy okazji.
  * Kolory rzadkości to pędzle w kodzie systemu (`Dnd5eSystem`), nie tokeny motywu — zgodnie
    z *Kolorami po znaczeniu*.
  * Paczki autora = kopia paczki testowej `dnd5e` (hobgoblin, eliksir) plus nieczytany katalog
    `templates/` ze starego formatu. **Id paczki dostarczanej nie może być `dnd5e`** — zderzyłoby się
    z kopią u autora; zachowanie loadera przy powtórzonym id paczki sprawdzić w porcji 3b.
* **Czeka na autora** (z kolejki, 2026-09-24): ręczne przeniesienie kampanii do
  `Dokumenty\DungeonApp\dnd5e\campaigns\` (stare `Packs` i `Campaigns` nie są czytane); dopisanie
  `group` potworom.
* **Okienka tylko w aplikacji** (fundament, duża runda; nieodtworzone bez okna): menu sortowania
  wyśrodkowane pod odnośnikiem i okienko listy zmieniające szerokość przy przewijaniu. Porcja 9a ich nie
  dotknęła; czy są nadal — autor nie zgłosił przy przyjęciu porcji 9. Przy porcji listy (wycinek 3)
  zapytać; jeśli są — diagnoza w działającej aplikacji: `FlyoutPresenter.Width` i `Bounds` korzenia
  okienka przy otwarciu i po przewinięciu, czy `FixListWidth` w `Themes/PopupOpenLayout.cs` nie kończy
  się wcześnie, położenie menu względem odnośnika — np. tymczasowy zapis do pliku wywołany jednym
  otwarciem; autor podaje skalę ekranu Windows. Duży prawy margines menu w zakładce to najpewniej
  najmniejsza szerokość menu (160) przy krótkich nazwach. Wchodzi do werdyktu, jeśli to ograniczenie
  biblioteki.
* **Położenie `MenuFlyout` pod przyciskiem ustawia widok** (fundament, porcja 7a) — okienko wysuwane nie
  jest kontrolką, motyw go nie dosięga. Brief ma to podawać.
* **Okno potwierdzenia i powiadomienie wywołane z pozycji menu rzucą wyjątkiem** (fundament, porcja 7b)
  — szukają warstwy przez okno elementu wywołującego. Brief podaje: wywołanie z elementu okna albo
  poprawka.
* **Odznaka akcentu nie stoi w wierszu listy** (fundament, porcja 8b): na wybranym wierszu jej tekst ma
  4,00:1, a akcent konkuruje z samym wyborem. W wierszu — odznaka neutralna, stanu albo kolor systemu;
  akcent na karcie. Brief listy ma to podać.

* **Po porcji 3a** (raport wykonawcy, 2026-09-28) — do briefów 3, 5 i 7:
  * Zdanie pustej zakładki podaje system polem `ContentTabDefinition.EmptyText` (opcjonalne; bez niego
    zdanie bez nazwy typu). Podpowiedź „gdzie leżą paczki” jest bez ścieżki — zakładka jej nie zna,
    potrzebny nowy przewód; naturalnie z porcją 7 (rejestr i wczytywanie).
  * Kolory rzadkości w systemie zmieniły odcienie (rzadki był równy akcentowi; pospolity i artefakt
    miały 2,4–3,7:1); dziś to jedna odmiana — tekst na pigułce o kryciu 16%. Odmianę tekstową na kartę
    dokłada porcja 5.
  * „Wyczyść filtry” wygasza runda W porcji 3a (przeniesione z porcji 3).
  * Wybór zdjęty przez filtr nie wraca po poszerzeniu filtra — zgodnie z *C*.
  * Niesprawdzone: czy role `body`/`caption` działają na `SelectableTextBlock` (powód i ścieżka
    w szczególe rzeczy zepsutej) — autor ogląda; jeśli nie, poprawka w motywie.
  * Odstęp nazwa zakładki–licznik to dwie spacje krojem liczb (ok. 14, poza skalą) — cena wspólnej
    linii bazowej.

## Do sesji głównej

* **Budżet długości dokumentu czytanego na starcie każdej sesji** (propozycja architekta, 2026-09-25,
  przeniesiona z fundamentu): `collaboration.md` ma ok. 530 linii i jest czytany prawie w całości co
  sesję (start sesji: ok. 55 tys. tokenów sama rama, +28 tys. `collaboration.md` z dokumentem zadania).
  Propozycja: limit długości dla dokumentów czytanych zawsze, rzadko potrzebne — do części czytanej
  „gdy potrzeba”. Limit ustalić po przejrzeniu, co sesja rzeczywiście używa na starcie. Autor: omówić
  w sesji głównej (przebudowuje dokument wspólny dla wszystkich zadań).

## Przy zamknięciu

* Werdykt o Avalonii → `decisions.md` (*Ustalenia*, *Punkt kontrolny*); tabela *Pomiar porcji* razem
  z nim.
* Rozstrzygnięcia o kartach i liście, które przeżywają zadanie → `architecture.md`, *Zakładki treści*.
* Zasada „wierność D&D 5e nie jest celem” → `architecture.md`, jeśli autor uzna ją za ogólną dla
  systemu (zapytać przy zamknięciu).
* Odnośnik w `tasks.md` znika; kolejka przechodzi do kroku 11.
