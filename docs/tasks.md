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

Gałąź: `master`. Build bez ostrzeżeń, 347 testów zielonych (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

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
3. **Fundament interfejsu — zanim cokolwiek innego w interfejsie** (autor: „najpierw od tego zaczął").
   Każda używana kontrolka dostaje w motywie ramy **własny, kompletny szablon zamiast domyślnego**,
   nie poprawki nałożone na domyślny motyw — to one dziś przepuszczają niechciane efekty, tekst
   „prawie" na środku i globalne wysokości. W jednym miejscu, raz: stany (spoczynek, najechanie,
   zaznaczenie, wyłączenie — nic więcej), tekst wyśrodkowany w pionie z założenia, wymiary
   jako zasoby motywu. **Kontrolki: profesjonalność, czytelność i intuicyjność przed gustem i wiernością
   mockupowi** (autor, 2026-09-24) — mockup jest makietą ekranu rejestru, nie projektem całej
   aplikacji; wymiary i zachowanie kontrolek bierze się z konwencji platformy i czytelności, mockup
   jest odniesieniem dla zakładek treści. Autor: „jeżeli chcemy potem w jakimś miejscu w interfejsie zrobić
   dropdown albo listę […] masz już od razu gotowy styl zadeklarowany przez styl aplikacji".
   * **Galeria kontrolek** — zakładka ramy nad „Ustawieniami", pod nagłówkiem „System" (kategoria
     Aplikacja; autor 2026-09-24), pokazująca każdą kontrolkę
     w każdym stanie: zwykła, wyłączona, zaznaczona, długi tekst, pusta. Autor sprawdza w niej każdą
     porcję w minutę; zostaje na stałe.
   * **Zakres: pełny zestaw standardowy** (autor: fundament ma objąć „99% wszystkich potencjalnych
     elementów interaktywnych"): przyciski we wszystkich odmianach (główny, zwykły, cichy, ikonowy,
     niszczący, link); pola tekstowe, wyszukiwania, wielowierszowe, liczbowe; pole wyboru, przycisk
     opcji, przełącznik, suwak; lista rozwijana pojedyncza, wielokrotna, z wyszukiwaniem; zakładki,
     przełącznik segmentowy; wiersz listy; menu i menu kontekstowe; podpowiedź, okienko wysuwane, okno
     potwierdzenia, powiadomienie; wskaźnik postępu; tag, chip, odznaka; kafelek, sekcja rozwijana,
     separator, okruszki, pusta lista; **tabela** (nagłówek, kolumny z wyrównaniem, wcięcia komórek,
     obramowanie, wyróżnienie pojedynczej komórki kolorem podanym przez układającego); pasek przewijania; typografia; skala odstępów. **Poza zestawem:**
     kalendarz i wybór koloru — bez zastosowania przy stole, a kalendarz zaprasza do pól niosących czas
     (drugi zakaz). Układ (stosy, siatki, wyrównanie) nie wymaga szablonów — tylko odstępy ze skali.
   * **Wyjątek od „nic bez konsumenta"** — wpisany 2026-09-24 (`decisions.md` i `architecture.md`,
     *Pytania otwarte i reguła „nic bez konsumenta"*).
   * **Element spoza zestawu:** wykonawca buduje go jawnie jako własny i zgłasza w raporcie; **przy
     drugim użyciu przechodzi do motywu** osobnym zleceniem. Z założenia oryginalne zostają: karty
     treści (projektowane per typ), biurko z oknami, pasek boczny, ekrany jednorazowe.
   * **Porcje — przyjęte przez autora 2026-09-24.** Jedna porcja = jeden wykonawca w 20 minutach;
     autor sprawdza każdą w galerii **i w aplikacji**, bo porcja od razu zdejmuje stare poprawki
     nałożone na swoje kontrolki w całej aplikacji. Motyw domyślny biblioteki leży pod spodem do
     porcji 9. Kolumna „rundy" to poprawki od autora, „pomiar" — kroki, minuty i odczyt z
     `tools/subagent-usage.py`, suma przebiegów porcji — materiał do punktu kontrolnego.

     | # | Porcja | Rundy | Pomiar |
     |---|---|---|---|
     | 0 | galeria (zakładka), skala odstępów, wymiary z mockupu, typografia z krojem liczb | 1 (obcięte ogonki, grubości kroju nagłówków, siedem stopni pisma); przyjęta | 33 / 5,8 / 3,1 mln + runda 1: 22 / 4,0 / 1,5 mln |
     | 0b | powierzchnie i linie: tło, karta, panel, sekcja z obramowaniem; linia pozioma i pionowa, w liście, między sekcjami | 1 (architekt: kontrast przygaszonego tekstu i czerwieni, krój nagłówków od 20; autor: domyślny kolor tekstu); przyjęta — dwa kolory (drugorzędny, ostrzeżenie) przechodzą do porcji 1 | 33 / 6,3 / 2,7 mln + runda 1: 47 / 7,2 / 4,4 mln |
     | 1 | przyciski — sześć odmian; wysokość kontrolki według standardu okienkowego (dziś 38 z makiety) → 32, po rundzie 1 → 36 (autor: 32 zbyt ściśnięte); `frame-action` zostaje elementem ramy (pełna wysokość paska), nie przyciskiem (architekt) | 1 (autor: ikony niewidoczne na kolorowych przyciskach, wciśnięcie bez własnego koloru, wyłączony główny szary jak przed porcją, wysokość 36; architekt: przycięcie w galerii, odnośnik w zdaniu, grubość napisów); 2 (autor: ikony konturowe, odnośnik w zdaniu nad linią, najechanie akcentu odbarwia, bez wciśnięcia); 3 (autor: najechanie z wypełnieniem = kolor bez zmian + otoczka 3 px, odnośniki samodzielne szare, bez ręki nigdzie — `decisions.md`; poprawka: przycisk przycinał otoczkę); przyjęta | 52 / 8,4 / 6,1 mln + runda 1: 60 / 8,6 / 6,5 mln + runda 2: 50 / 7,9 / 5,3 mln + runda 3: 30 / 4,9 / 2,4 mln + poprawka otoczki (ten sam wykonawca, wznowiony): 14 / 2,2 / 1,7 mln |
     | 2 | pola tekstowe — zwykłe, wyszukiwania, wielowierszowe, liczbowe; kolor zaznaczenia; tekst do zaznaczenia; zakreślenie (styl tekstu na fragmencie, odmiany po znaczeniu: wyróżnione, trafienie wyszukiwania); pole w trakcie pisania ma wyraźną krawędź — to stan edycji, nie wskaźnik fokusu klawiatury (architekt; po rundzie 1 — krawędź z najechania, bez akcentu); znaczenia pędzli `Input*`; fokus zdejmowany w ramie raz dla całej aplikacji | scalona 2026-09-25; poprawka architekta: aplikacja padała po wejściu w system (selektor potomka w motywie pola), test budujący wszystkie motywy; 1 (autor: krawędź edycji i zaznaczenie w akcencie krzykliwe — zaznaczenie niebieskie; ikona, × i strzałki poza obszarem tekstu, bez tła pod myszą; architekt: liczby całkowite z przecinkiem, tekst pod paskiem przewijania, grubość tekstu w polu, wyrównanie galerii); przyjęta 2026-09-25 — drobne uwagi w *Poprawkach czekających na obszar* | 52 / 10,5 / 7,0 mln + runda 1: 39 / 9,1 / 4,1 mln |
     | 3 | lista, wiersz listy, pusta lista, pasek przewijania — **ustalenia architekta do briefu niżej**; przygaszony akcent naprawiony (16 %, z kanałem alfa) | scalona 2026-09-25, obejrzana — uwagi autora (przewijanie przechodzi wyżej, najechanie na wybranym) w *Poprawkach czekających na obszar*; wiersz ma wysokość najmniejszą 32, nie stałą, bo panel instancji kampanii ma wiersze z polem liczbowym — panel testowy, do usunięcia (autor), więc bez poprawek | 55 / 9,4 / 7,4 mln |
     | 4 | pole wyboru, przycisk opcji, przełącznik, suwak — to, co wypełnione akcentem, pod myszą się nie zmienia; wyłączone zaznaczone traci akcent; tor przełącznika 36×18; suwak tylko poziomy | scalona 2026-09-25; przyjęta 2026-09-25 bez rund — pytania autora w *Poprawkach czekających na obszar* | 40 / 9,8 / 4,8 mln |
     | 5 | okienko wysuwane; lista rozwijana pojedyncza, wielokrotna, z wyszukiwaniem | | |
     | 6 | zakładki, przełącznik segmentowy, kafelek, sekcja rozwijana, okruszki | | |
     | 7 | menu, menu kontekstowe, podpowiedź, okno potwierdzenia, powiadomienie, wskaźnik postępu (kolor wypełnienia z właściwości kontrolki, domyślnie akcent — podaje go układający widok, np. pasek PZ; tak jak w suwaku z porcji 4) | | |
     | 8 | tag, chip, odznaka (odmiany po znaczeniu: neutralna, wyróżniona akcentem, stany; kolor podany przez system dla jego skal; wnętrze krojem liczb, wymiary z motywu), tabela | | |
     | 8b | kompozycje przykładowe w galerii (pomysł autora): lista z filtrami i wyszukiwaniem, karta z tabelą i odznakami, okno potwierdzenia nad listą | | |
     | 9 | odcięcie motywu domyślnego biblioteki; usunięcie tokenów bez użycia (lista w raporcie 0b: m.in. `DungeonSuccessBrush`, `DungeonPaddingXl`, `DungeonNavigationRowHeight`) | | |
     | 10 | klocek: wiersz listy z kreską zaznaczenia | | |
     | 11 | klocek: chip z listą wyboru | | |
   * **Porcja 3 — ustalenia architekta do briefu (2026-09-25, przed zleceniem):**
     * *Pasek przewijania:* sam uchwyt — bez toru i bez przycisków krokowych; widoczny wyłącznie, gdy
       jest co przewijać (dziś `BuiltInControls.axaml` wymusza `Visible` i stały pas); cienki (mockup:
       uchwyt 6 px, kolor mocnej krawędzi), pod myszą jaśniejszy i szerszy w obrębie własnego pasa;
       pas zajmuje własne miejsce, nie przykrywa treści (tekst pola wielowierszowego nie może pod nim
       leżeć — dziś `AllowAutoHide=False` w motywie pola). Znika lokalny styl `ScrollBar`
       w `ContentTabView` i uchwyt w akcencie z `BuiltInControls`.
     * *Lista i wiersz:* `ControlTheme` dla `ListBox`/`ListBoxItem` — wysokość wiersza z tokenu, nie
       z treści; stany: spoczynek (przezroczyste), najechanie (`BackstageRowHover`), wybrane
       (przygaszony akcent), wybrane pod myszą, wyłączone. **Dług z `code-state.md`:**
       `DungeonAccentDimColor` ma wartość pełnego akcentu — dostaje wartość dzisiejszego lokalnego
       `ContentSelectedRowBrush` (akcent 16 %), a użytkownicy tokenu (godło kampanii w `Icons.axaml`,
       galeria odstępów) do sprawdzenia, czy nie zgasną — wypisać. Znikają lokalne style `ListBoxItem`
       w `CampaignInstancesToolView` i `FlyoutPresenter ListBoxItem` w `ContentTabView`.
     * *Pusta lista:* jedna kontrolka ramy — komunikat (tekst zwykły) i opcjonalna podpowiedź
       (przygaszona), u góry obszaru listy z wcięciem, nie na środku wysokiej listy; bez ilustracji.
       Wdrożyć tam, gdzie lista dziś bywa pusta (biblioteka kampanii ma `IsEmpty`, zakładka treści
       przy filtrach bez wyniku, świat kampanii bez okazów).
     * *Poza porcją 3:* wiersze z `Button` (`content-row-button`, lista kampanii, pasek boczny) — to
       klocek „wiersz listy z kreską zaznaczenia" (porcja 10) i zlecenia A; filtr z polami wyboru —
       porcja 5.
   * Szablon domyślny Avalonii (MIT, jawny) przejmuje się raz, świadomie — brief wskazuje, który i skąd;
     to nie jest grzebanie w bibliotekach, którego zabrania definicja wykonawcy.
   * **Fokus: dziś żadnego widocznego** — tylko najechanie i zaznaczenie, dla myszki. Autor: aplikacja
     będzie docelowo „keyboard first" ze skrótami; wtedy fokus pokazuje się **wyłącznie, gdy
     użytkownik faktycznie zaczął używać klawiatury** — nie przy kliknięciu myszką. Do zrobienia razem
     z obsługą klawiatury, nie teraz.
   * **Typografia w fundamencie** (autor: „bardzo ładna i spójna biblioteka typografii"): kroje, stopnie
     nagłówków i tekstu, odstępy — jako style tekstu ramy, pokazane w galerii. W tym krój o stałej
     szerokości znaków dla liczb, jak w mockupie (`--font-mono: IBM Plex Mono`).
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
   * ile rund poprawek od autora potrzebowała każda porcja interfejsu,
   * czy wracają błędy rzemiosła (stany, fokus, wyrównanie, układ zależny od zawartości),
   * kroki, czas i odczyt na zlecenie interfejsu — wobec 2026-09-24: typowe zlecenie z kodem od 22.09
     to 124 kroki i 17,5 minuty, a trzy najdroższe przebiegi w historii projektu to widoki
     (najdroższy: 362 kroki, 53 minuty, 142 mln tokenów odczytu).
   Wynik idzie do `decisions.md`, do wpisu o technologii interfejsu (punkt 2): **uznany** — Avalonia
   zostaje z uzasadnieniem z liczb; **odrzucony** — to jest wyzwalacz próby z interfejsem w HTML-u na
   jednej zakładce. Autor: „chciałbym to na końcu nazwać […] i rozwiązać, czy zbudowanie tych
   fundamentów rozwiązało problem".

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
* ~~Pole wyszukiwania: tekst zastępczy wyśrodkowany, fokus zdejmowany kliknięciem obok i Escape~~ —
  zrobione w porcji 2 fundamentu.

*B. Karta potwora*
* Wyzwanie — zakreślenie z motywu (porcja 2), samo „1/2"; PD obok, przygaszone. Tagi kategorii
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

* **Pola tekstowe (motyw ramy, `DungeonControls.axaml`, `FieldIconPointer.cs`)** — uwagi autora po
  rundzie 1 porcji 2, 2026-09-25:
  * Przycisk × w polu wyszukiwania bez podpowiedzi „Wyczyść" — sam krzyżyk jest czytelny.
  * Ikona na początku pola (ołówek, lupa) tylko przykrywa obszar tekstu, nie odcina go: nad ikoną,
    obok niej, nad nią i pod nią da się złapać kursor tekstowy. Obszar tekstu ma zaczynać się za
    ikoną — cały pas od krawędzi pola do tekstu, na pełnej wysokości, to strefa ikony ze strzałką.
  * Zaznaczanie jak w przeglądarce: przeciągnięcie **rozpoczęte obok tekstu** (np. w pustym miejscu
    karty) i przeprowadzone nad tekstem do zaznaczenia ma go zaznaczać — dziś trzeba trafić w sam
    tekst. Do rozstrzygnięcia przy zleceniu: zasięg (obszar wokół jednego bloku tekstu czy cała
    karta; zaznaczenie przez kilka bloków naraz to osobna, większa rzecz).
* **Przewijanie i lista (motyw ramy, `DungeonControls.axaml`)** — uwagi autora po porcji 3, 2026-09-25:
  * Przewijanie kółkiem nie przechodzi do panelu wyżej: lista dojechana do końca zatrzymuje kółko,
    zamiast przewijać obszar, w którym leży (dziś przewija np. cały obszar roboczy). Architekt: tak
    zachowują się aplikacje, przeglądarka łańcuchuje; w Avalonii `ScrollViewer.IsScrollChainingEnabled`
    = `False` w motywie przewijania.
  * Wiersz wybrany nie ma stanu najechania — pod myszą wygląda jak wybrany (gust autora; nie łamie
    konwencji: najechanie mówi „to da się kliknąć", a klik w wybrany wiersz nic nie zmienia). Znika
    token `DungeonAccentDimHoverColor` i stan „wybrane pod myszą" z komentarza motywu.
  * Kontrolka w wierszu listy (przycisk, pole) nie zmienia zaznaczenia wiersza: dziś kliknięcie
    przycisku w wierszu odznacza wiersz, w którym ten przycisk stoi (autor, 2026-09-25). Najpierw
    diagnoza przyczyny, poprawka w motywie albo w kontrolce ramy, nie w widoku. Widać to w panelu
    „Instancje w tej kampanii" (D&D 5e) — ten panel jest testowy i do usunięcia (autor), więc nie
    dostaje własnych poprawek; do odtworzenia błędu wystarczy przykład w galerii.
  * Podpis sekcji „Listy" w galerii mówi „wiersz ma stałą wysokość" — ma najmniejszą (32, patrz
    tabela porcji, porcja 3).
* **Pole wyboru, przełącznik, suwak (motyw ramy, `DungeonControls.axaml`)** — pytania autora po
  porcji 4, 2026-09-25, bez zmian na razie; odpowiedź architekta czeka na słowo autora:
  * Czy pole wyboru i przełącznik mają mieć najechanie? Architekt: tak — konwencja platform
    i bibliotek; mówi, że klikalna jest też etykieta (długa etykieta nie pokazuje sama, gdzie kończy
    się pole).
  * Czy suwak ma mieć najechanie albo podświetlenie przy przeciąganiu, np. otoczkę? Architekt:
    rozjaśnienie uchwytu wystarcza; otoczka na każdej kontrolce odrzucona przy przyciskach
    (`decisions.md`).
  * Czy trzy stany pola wyboru rozróżniać kolorem, nie tylko znakiem? Architekt: nie — zaznaczone
    i pośrednie mają ten sam kolor na wszystkich platformach; pośrednie to „częściowo zaznaczone”,
    ta sama rodzina, a inny kolor sugerowałby inne znaczenie.
  * Animacje (pytanie autora, 2026-09-25). Architekt: **przełącznik — tak**, gałka jedzie, tło
    przechodzi w akcent, ok. 120–150 ms z wyhamowaniem (konwencja Windows 11, iOS, Material; ruch
    mówi, w którą stronę zmienił się stan). **Pole wyboru, przycisk opcji — nie** (nic się nie
    przemieszcza). **Suwak zmieniający kolor wypełnienia z wartością — nie**: kolor niesie znaczenie,
    a zwykły suwak nie wie, czy wysoko to dobrze, czy groźnie; znaczenie, jeśli jest, podaje ten, kto
    układa widok. Proponowana reguła dla całej aplikacji (do `architecture.md` po słowie autora):
    animacja tylko tam, gdzie coś się przemieszcza albo pojawia; krótka (do 150 ms); nigdy nie
    opóźnia działania; najechanie i zaznaczenie zmieniają kolor od razu; wyłączone animacje
    w systemie (Windows „Pokaż animacje") wyłączają je w aplikacji.

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
