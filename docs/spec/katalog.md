# Katalog świata — specyfikacja

Etap 2 roadmapy. Katalog przechowuje rzeczy świata kampanii, które mają funkcję mechaniczną: stworzenia
(potwory, NPC), przedmioty (także skrzynie), a od etapu 4 postacie graczy. Opis miejsca i wiedza bez
mechaniki to lore, nie entity. Katalog zastępuje testowe narzędzie „Świat kampanii”.

## Miejsce: biurko jest centrum gry

- Biurko zostaje jedyną zakładką gry. Podział na zakładki według filarów (eksploracja, interakcja,
  walka) odrzucony: scena przechodzi między filarami w trakcie rozmowy, a te same rzeczy świata należą
  do wszystkich trzech.
- **Biurko zajmuje miejsce strony kampanii.** Kategoria KAMPANIA ma zawsze jeden wiersz: bez otwartej
  kampanii półkę „Kampanie”, po otwarciu „Biurko”. Strona kampanii (nazwa, data, „Zamknij”) znika —
  nazwa stoi na pasku górnym, data na półce. Zamknięcie wraca do półki; inną kampanię otwiera się
  stamtąd. Zakładki kampanii spoza biurka (np. przyszła „Fabuła i lore”) stoją pod nim.
- **Biurko ma trzy poziomy głębi**, żeby elementy nie konkurowały hierarchią:
  - *leżące* (katalog) — płasko na blacie, w kolorze biurka;
  - *uniesione* (okna) — nad katalogiem, z cieniem; poruszają się po całym biurku;
  - *rama nad oknami* (listek, później hotbar) — najwyżej: tło ciemniejsze niż okno, mocniejszy cień,
    większe zaokrąglenie. Okna mogą wjechać pod nie.
- **Listek** — poziomy pasek w lewym górnym rogu biurka, z lekkim marginesem, w sekcjach
  oddzielonych liniami: „Zamknij kampanię” | Cofnij, Ponów | „Polecenia” z ikoną terminala. Cofnij,
  Ponów i Polecenia to wyłączone zaślepki z podpowiedzią, dopóki nie powstaną. Wskaźnika zapisu
  i przycisku „Zapisz” nie ma — kampania zapisuje się po każdej zmianie, nieudany zapis pokazuje
  powiadomienie. Z pierwszym ustawieniem kampanii (kopie zapasowe, waluta, warianty) dochodzi tu okno
  „Ustawienia kampanii”.
- **Katalog leży na biurku**, nie jest oknem, zakładką ani przyklejonym pasem. To katalog główny
  „Świat” położony na siatce, zawsze pod oknami. Domyślnie stoi w prawym górnym rogu; przesuwa się go,
  ciągnąc za wiersz główny, i przyciąga do siatki jak okna. Pozycja, szerokość i rozwinięte katalogi
  zapisują się z układem biurka (nie w zapisie kampanii).
- Pod drzewem linie siatki znikają: tło w kolorze biurka, bez ramki i cienia, żeby linie nie cięły
  liter. Do oceny na renderze — alternatywą jest pełna przezroczystość.
- Drzewo sięga od swojej górnej krawędzi do dna biurka i dalej się przewija. Szerokość stała,
  zmieniana przeciągnięciem prawej krawędzi; długa nazwa kończy się wielokropkiem, całość w podpowiedzi.
- Okna (także zmaksymalizowane) zakrywają katalog — świadomie. Zwinięty katalog główny to jedna linia
  „▸ Świat”.
- Inicjatywa (etap 5) to okno, które w razie potrzeby maksymalizuje się na całe biurko. *Wyzwalacz
  powrotu:* walka po punkcie kontrolnym „Pierwsza walka” wymaga własnej zakładki — wtedy katalog
  przenosi się poziom wyżej, wspólny dla wszystkich zakładek kampanii.

## Drzewo

- Katalog ma nazwę i zawartość, bez opisu. Entity leży w katalogu; entity w entity (plecak, skrzynia
  z zawartością) rozstrzyga etap 9.
- Kolejność automatyczna: najpierw katalogi, potem entity, alfabetycznie, liczby naturalnie
  („Goblin 2” przed „Goblin 10”). Kolejność fabularną daje prefiks w nazwie („1. Karczma”).
- Wiersz katalogu: chevron (animowany przy zwijaniu), ikona katalogu w SVG, nazwa.
- Wiersz entity: ikona typu — ta sama co zakładka biblioteki (Stworzenie, Przedmiot, później Postać),
  nazwa, przygaszony № i, u istoty z walką, przygaszone bieżące PW / maksimum przy prawej krawędzi.
  Entity, której wpis zniknął z paczki, ma ikonę ostrzeżenia; powód widać w podglądzie.
- Tekst wierszy w rozmiarze pomocniczym (12), wiersz niższy niż wiersz listy. Minimum ikon: bez koszy
  i ołówków; działania idą przez zaznaczenie, menu i klawisze.
- **Tożsamość:** każda entity ma № unikalny w całej kampanii — nadawany po kolei przy tworzeniu, nigdy
  zmieniany ani używany ponownie. Nazwy mogą się powtarzać; nazwa domyślna to nazwa wpisu („Goblin”).
- Drzewo to porządek sceny, nie mechanika: rozgrywka toczy się w jednym katalogu naraz, a entity
  w złym katalogu niczego nie psuje. Dlatego operacje w drzewie nie mają oznaczeń niezatwierdzonych
  zmian (sekcja „Zatwierdzanie zmian”).

## Obsługa

- Zaznaczanie: klik, Ctrl+klik (dołącz), Shift+klik (zakres).
- Menu pod prawym przyciskiem:
  - katalog — Dodaj… (Ctrl+N), Nowy katalog (Ctrl+Shift+N), Zmień nazwę (F2), Usuń (Del);
  - entity — Otwórz (Enter), Zmień nazwę (F2), Przenieś do…, Usuń (Del).
- Usuwa się tylko pusty katalog; przy niepustym pozycja jest wyłączona i podaje powód. Usunięcie
  entity potwierdza okno z wymienionymi nazwami. Cofania nie ma: przeniesienie się odwraca, usunięcie
  potwierdza.
- „+” w wierszu głównym i w wierszu katalogu pod wskaźnikiem otwiera paletę dodawania do tego
  katalogu. Ctrl+N dodaje do zaznaczonego katalogu, do katalogu zaznaczonej entity, a bez zaznaczenia
  — do głównego.
- Zmiana nazwy w miejscu: pole w tym samym miejscu i rozmiarze co tekst. Zatwierdza tylko Enter; Esc
  i kliknięcie obok porzucają wpis (sekcja „Poprawki w drzewie”).
- Przeciąganie: wiersz główny przesuwa katalog po biurku; każdy inny wiersz przenosi zaznaczenie do
  katalogu, na który się je upuści. Katalog przenosi się z zawartością i nie wejdzie do własnego
  potomka. „Przenieś do…” otwiera paletę z listą katalogów. Po przeniesieniu katalog docelowy jest
  rozwinięty, przeniesione wiersze zaznaczone, a drzewo przewija się do nich.

## Paleta

Element ramy wielokrotnego użytku: okienko u góry pośrodku okna aplikacji, nad wszystkim.

- Wiersz celu („Dodaj do: Jaskinia”, „Przenieś 3 do…”, „Dodaj stan: Goblin № 5”), pole wyszukiwania,
  lista wyników jak wiersze biblioteki (ikona, nazwa, przygaszone tagi, odznaka). ↑↓, Enter, Esc;
  klik obok zamyka.
- Wywołujący podaje cel i to, co wolno wybrać — paleta pokazuje tylko poprawne wyniki.
- Dodawanie entity: wpisy typów, które mają entity. Liczba przed nazwą dodaje kilka sztuk („4 gob” →
  wiersz „Goblin ×4”); podpowiedź pod polem to mówi. Enter dodaje i zamyka, Ctrl+Enter dodaje i zostawia
  paletę otwartą (trzy gobliny i szef). Nowe entity są zaznaczone, ich katalog rozwinięty.
- Entity zawsze powstaje z wpisu; improwizowany NPC startuje z wpisu takiego jak „Pospolity człowiek”
  (rozstrzygnięcie „Entity zawsze powstaje z wpisu”).
- Ta sama paleta służy do przenoszenia i dodawania stanu i efektu, a później do „znajdź w świecie”
  i odnośników.

## Podgląd i karta

- Klik zaznacza. Dwuklik albo Enter otwiera **okno podglądu** — jedno na biurku — albo wyciąga je na
  wierzch. Otwarty podgląd idzie za zaznaczeniem: pokazuje ostatnio klikniętą entity, zaznaczenie
  katalogu go nie podmienia.
- **Pinezka** w nagłówku podglądu odkłada bieżącą kartę do osobnego okna; podgląd dalej idzie za
  zaznaczeniem. Przypiętych okien może być kilka. Usunięcie entity zamyka jej przypięte okno, a podgląd
  zostaje pusty.
- **Tytuł okna** niesie nazwę i №: „Podgląd · Goblin 2 · № 2”, okno przypięte „Goblin 2 · № 2”.
  Ścieżki katalogów w oknie nie ma — miejsce entity pokazuje drzewo, a № wskazuje ją jednoznacznie.
- **Karta entity zmienia się w miejscu** (rozstrzygnięcie „Karta entity zmienia się w miejscu”). To
  ta sama karta co w bibliotece, przedzielona istniejącą kreską przed parami pól:
  - **część żywa** (nad kreską) — ścieżka kategorii, obraz, nazwa entity, tagi, PW, KP, Szybkość,
    tabele cech oraz sekcje Stany, Efekty i Notatka; wartości entity w miejscach wartości wpisu;
  - **część stała** (pod kreską) — pary pól, akcje, proza i opis wpisu, bez zmian względem biblioteki;
    nic, co zmienia się przy stole.
- **Ścieżka kategorii jak w bibliotece** („Stworzenia › Nieumarli › Szkielet”). Jej ostatni człon to
  nazwa wpisu, więc widać, czym jest „Goblin 2”. Linii „№ 5 · Goblin” pod nazwą nie ma: wypychała
  kolumnę tytułu ponad dolną krawędź obrazu.
- Nic nie jest przyklejone: karta przewija się w całości, a część żywa stoi na jej górze.
- *Do zmiany w kodzie (wycinek 2.3b):* `EntityCardBuilder` podmienia ścieżkę kategorii na ścieżkę
  katalogów i dopisuje linię „№ 5 · Goblin”; tytuł podglądu to samo „Podgląd”.

## Część żywa

Nazwy „część żywa” i „część stała” wchodzą przy budowie do słownika w `docs/architecture.md`.

- **Układ stworzenia:** w kolumnie obok obrazu nazwa, tagi i PW na całą szerokość kolumny. KP
  i Szybkość stoją w rzędzie tabel cech, na prawo od INT/MDR/CHA: podpis na wysokości skrótów cech,
  wartość na wysokości wartości cech, dopisek („strzępy zbroi”) na wysokości modyfikatorów. Pod
  tabelami sekcje Stany, Efekty i Notatka, potem kreska.
- **PW:** podpis z ikoną serca „PW”, duża bieżąca liczba, obok mniejsze, przygaszone „/ 13” na
  wspólnej linii bazowej; pod nimi gruby, kwadratowy pasek od 0 do maksimum. Pasek tylko pokazuje
  stosunek bieżących do maksimum — nie da się go przeciągać. Wypełnienie czerwone, z własnym tokenem
  (czerwień PW, nie błędu). PW ponad maksimum są widoczne (nadwyżka innym odcieniem, granica maksimum
  widoczna) i nie wyglądają jak błąd. Pod paskiem dopisek wpisu („2k8+4”).
- Kolumna obok obrazu mieści nazwę, tagi i blok PW w wysokości portretu (200 px); dłuższa nazwa
  podnosi nagłówek jak w bibliotece.
- „Zatwierdź” i „Odrzuć” stoją po prawej, na wysokości ścieżki kategorii (sekcja „Zatwierdzanie
  zmian”).
- Entity bez walki (przedmiot): nagłówek jak w bibliotece, pod nim Stany, Efekty i Notatka.
- Część żywa to zarazem **zwarty widok istoty** z etapu 5: okno inicjatywy pokazuje ją bez części
  stałej. Jak ją tam przyciąć — etap 5.
- *Ryzyko:* część żywa rośnie z kampanią (postać w etapie 6: miejsca na zaklęcia z kilku kręgów,
  ładunki) i spycha część stałą w dół. Gdy zacznie zabierać pół okna, trzeba rozstrzygnąć, co stoi
  w niej zawsze (PW, stany), a co jest zwinięte.

## Wartości z bazą

- KP, Szybkość, cechy i PW maks. mają **bazę** — wartość z wpisu (np. Szybkość goblina 9 m, INT 6).
  Pokazana wartość = baza + suma modyfikatorów efektów. Model „baza + modyfikatory” powstaje od
  pierwszego wycinka, zanim efekty istnieją, żeby ich dodanie niczego nie przebudowywało.
- **Wartości z bazą nie edytuje się wprost**, tylko efektem. Zmiana trwała też jest efektem: boss ze
  120 PW to „PW maks. +68”, który wisi, dopóki MG go nie zdejmie, a zdjęcie przywraca wartość wpisu.
- **Kolor cyfr względem bazy:** równa bazie — zwykły (biały); powyżej — zielony; poniżej — czerwony.
  Dotyczy KP, Szybkości, PW maks. i wartości cech. Tło komórki modyfikatora mówi dalej o znaku, jak
  w bibliotece — dwa znaczenia w dwóch miejscach. Kolory względem bazy mają własne tokeny, choćby
  o barwach modyfikatorów karty. Przykład: Szybkość 12 m zielona; ZRĘ 12 zamiast 14 czerwona, a jej
  modyfikator +1 dalej na zielonym tle.
- Efekt na cechę zmienia jej wartość, a modyfikator wynika z wartości (kod systemu).
- Rozpisania pod wartością nie ma: dopisek zostaje tekstem wpisu, a skąd odchylenie, mówią kolor
  i sekcja Efekty tuż pod tabelami.
- Bieżące PW nie mają koloru względem maksimum — ranny goblin to stan zwykły, mówi o nim pasek PW.
- System podaje listę wartości z bazą i skąd brać bazę; efekty, suma i kolory należą do ramy, która
  nie wie, czym jest KP.

## Edycja w części żywej

- **MG zmienia przy stole tylko:** bieżące PW, stany (dodaj, usuń), efekty (dodaj, usuń) i notatkę.
  Wartości z bazą — efektem; część stała się nie zmienia. Zmianę, która nie jest liczbą (dodatkowa
  odporność, drugi atak bossa), MG zapisuje w notatce — kierunek na razie, nie ostateczny.
- **Bieżące PW:** klik w liczbę zamienia ją w pole zmiany w tym samym miejscu i rozmiarze. Pole
  przyjmuje `-5`, `+3`, `=10`; liczba bez znaku nie przechodzi (rozstrzygnięcie „Pole zmiany liczby”).
  Przed Enter zapowiada wynik („−5 → 3”); Enter zatwierdza, Esc porzuca. Klik w liczbę odróżnia
  bieżące PW od wartości z bazą, które na klik nie reagują. Rachunek należy do systemu:
  - `-` odejmuje (od etapu 5 najpierw PW tymczasowe) i schodzi najniżej do 0; zapowiedź pokazuje
    nadmiar („5 poniżej zera”);
  - `+` dodaje najwyżej do maksimum; zapowiedź pokazuje nadwyżkę („7 ponad maksimum”);
  - `=` ustawia dowolną wartość, także ponad maksimum (widać, nie blokuje).
- **Notatka MG:** wieloliniowa, Enter dodaje linię. Zapis przy wyjściu z pola, przy zmianie entity
  w podglądzie i przy zamknięciu okna — nie przy każdym klawiszu ani z opóźnieniem. Musi wyglądać na
  edytowalną; pusta pokazuje przygaszony tekst zastępczy. Notatkę ma każda entity, także z zepsutym
  wpisem.
- **Stany i efekty:** przycisk usunięcia przy wierszu, dodawanie na końcu sekcji przez paletę.

## Zatwierdzanie zmian

- Zmiana w części żywej **działa i zapisuje się od razu**; przycisku „Zapisz” nie ma. Zapomniane
  zatwierdzenie niczego nie gubi, awaria laptopa nie zabiera zmian, a drzewo, inicjatywa i przełomy
  czytają jedną prawdę.
- Kampania pamięta dla każdej entity **punkt odniesienia** — stan z ostatniego zatwierdzenia. Wartość
  różna od niego jest niezatwierdzona i ma **kleks**: niebieski, dekoracyjny rozprysk (SVG) za
  wartością. Kleks nie zmienia tła ani koloru cyfr, więc nie gryzie się z kolorem względem bazy ani
  z tłem modyfikatora. Dodany stan albo efekt ma kleks przy wierszu, usunięty zostaje przygaszony do
  zatwierdzenia; zmieniona notatka ma kleks przy podpisie. Kształt (farba czy atrament) i kontrast
  cyfr na kleksie — do projektu.
- Kleks mówi, że wartość się zmieniła, nie — z czego. Wartości sprzed zmiany („było 13”) ani
  rozpisania przy liczbie nie ma: pomyłkę („−70” zamiast „−7”) widać po pasku PW, a cofa ją „Odrzuć”.
- „Zatwierdź” robi z obecnego stanu entity nowy punkt odniesienia. „Odrzuć” przywraca punkt
  odniesienia i pyta o potwierdzenie, bo cofa wiele naraz. Przyciski stoją po prawej, na wysokości
  ścieżki kategorii, **osobno dla każdej entity** (przy stole myśli się „goblin”, nie „wszystkie
  zmiany”); widoczne, gdy entity ma niezatwierdzone zmiany, z zarezerwowanym miejscem.
- Kleks i „Odrzuć” zastępują ceremonię przy zmianie: zmiana z jednego kliknięcia (usunięcie stanu)
  jest dopuszczalna, bo widać ją i da się ją cofnąć.
- Zasięg: tylko część żywa (PW, wartości z bazą, stany, efekty, notatka). Operacje w drzewie
  (przeniesienie, nazwa, dodanie, usunięcie) nie mają oznaczeń.
- W walce „Następna tura” zatwierdza tak jak przycisk — to ten sam mechanizm co „Znaczniki zmian
  w turze” w `docs/decisions.md`. Które entity zatwierdza (tylko kończącą turę czy wszystkie) —
  otwarte pytanie 4, etap 5.
- „Odrzuć” to tańsza, od razu użyteczna forma odłożonego Cofnij.
- Mechanizm ramy, ogólny: różnica rekordów entity względem zapamiętanego punktu odniesienia, bez kodu
  w funkcjach i bez wiedzy o systemie.

## Stany

- Sekcja z nagłówkiem „Stany”. Każdy stan to wiersz: kwadratowa ikona stanu na wysokość 2–3 linii
  tekstu, obok nazwa, pod nazwą miejsce na licznik rund i notatkę MG („3 rundy · KON ST 13 kończy”,
  etap 5). Bez pigułek — nie pomieszczą tych pól.
- Dodawanie z palety (stan z paczki albo własny: nazwa, ikona domyślna), usuwanie przyciskiem przy
  wierszu. Stany domyślne wpisu stworzenia entity dziedziczy, dopóki MG ich nie zmieni.
- Licznik stanu tyka sam (przełom tury, etap 5). Stan nie zmienia żadnej liczby — od tego są efekty.

## Efekty

- **Efekt to modyfikator liczb nadany ręcznie przez MG:** nazwa wpisana przez MG („Tarcza wiary”)
  i jeden lub kilka modyfikatorów — wartość docelowa i liczba ze znakiem („KP +2”, „Szybkość −3 m”,
  „ZRĘ +2”; „Przyspieszenie: KP +2, Szybkość +9”). Aplikacja dodaje modyfikatory do bazy; nic więcej.
- Efekt nie pochodzi z wpisu: zaklęcie ani stan w paczce nie niosą modyfikatorów (reguła 1 granicy).
  Rzucone zaklęcie samo nic nie robi — MG dodaje efekt i MG go zdejmuje.
- Efekt nie ma licznika i nie wygasa. Stan z licznikiem i efekt to osobne pozycje: licznik
  Spowolnienia tyka sam, a jego efekt na Szybkość zostaje, dopóki MG go nie zdejmie. Wiszący efekt
  widać w sekcji i w kolorze wartości.
- Cele: wartości z bazą, które podaje system (KP, Szybkość, cechy, PW maks.). Sumowanie zwykłe;
  wyjątki podręcznika (efekty o tej samej nazwie się nie kumulują, Zbroja maga ustawia KP) rozstrzyga
  MG liczbą albo zdjęciem efektu.
- Sekcja z nagłówkiem „Efekty”, wiersz na efekt; wygląd — do projektu.

## Poprawki w drzewie

Drzewo nie ma kleksów ani „Odrzuć”, więc pomyłkę ma tam powstrzymać sama obsługa (wycinek 2.3c):

- zmiana nazwy w miejscu zatwierdza się po kliknięciu obok — ma zatwierdzać tylko Enter;
- próg przeciągnięcia 4 px (`WorldCatalogView`, `DragThreshold`) podnieść do ok. 8 px, żeby drgnięcie
  przy kliknięciu nie przenosiło wiersza;
- po przeniesieniu przewinąć drzewo do przeniesionych wierszy (katalog już się rozwija, wiersze
  zostają zaznaczone).

Akcja w drzewie zaznacza i odsłania to, czego dotknęła; powiadomienie („Przeniesiono 3 do: Jaskinia
goblinów”) tylko wtedy, gdy skutku nie widać. Usunięcie ma okno potwierdzenia.

## Projekt kontrolek

- Część żywa składa się z **rodziny kontrolek** projektowanej raz: pasek PW, bieżące PW z polem
  zmiany, notatka, wiersz stanu, wiersz efektu, kleks, kolory względem bazy. Dwie formy (tekst do
  czytania i pole w tym samym miejscu) mają bieżące PW i notatka.
- Forma do czytania wygląda jak tekst karty; pole zajmuje dokładnie to samo miejsce. Tła strefy
  edycji nie ma, więc to kontrolka musi powiedzieć, że da się ją kliknąć.
- Warianty do porównania na jednym arkuszu:
  - **A** — w spoczynku czysty tekst; pod wskaźnikiem delikatne tło, ramka i kursor tekstowy.
    Najczyściej, ale trudniej odkryć za pierwszym razem. Propozycja.
  - **B** — stały, cichy ślad (przerywane podkreślenie albo słabe tło), pod wskaźnikiem pełna ramka.
  - **C** — stała ramka; wygląda jak formularz, tylko dla porównania.

  Pusta notatka zawsze pokazuje tekst zastępczy, w każdym wariancie.
- Stany, które każda kontrolka ma zaprojektowane: spoczynek, pod wskaźnikiem, edycja; błędny wpis
  („abc” w polu zmiany — Enter nic nie robi, pole to pokazuje); zapowiedź wyniku przed Enter; kleks;
  powyżej i poniżej bazy, także z kleksem i na zielonym albo czerwonym tle modyfikatora; brak wartości
  („—”); PW ponad maksimum; notatka pusta, w jednej i w kilku liniach (rośnie w dół); fokus klawiatury
  (przy stole przechodzi się Tabem, więc tu fokus dostaje wygląd mimo „Szlifu” w roadmapie).
- **Kolejność pracy:** arkusz rodziny w Galerii — projektant (Opus), iteracje z oglądaniem renderu,
  render 2× — potem wybór wariantu, dopiero potem kompozycja części żywej z tych kontrolek. Zlecenie
  projektu podaje model interakcji i kontrolki, nie listę widżetów do ustawienia.
- Render części żywej: kadr samego okna podglądu w skali 2×, bez katalogu i innych okien.
- **Makieta paska** (gałąź `projekt-pasek-biezacy`) to przykład z odrzuconego układu z paskiem nad
  kartą. Z niej zostaje: duża liczba PW z mniejszym „/ 13”, podpis z ikoną serca, minimalistyczny
  pasek PW, render 2× (`SaveScaled` w `tools/render/Program.cs`, do przeniesienia). Słabe w niej:
  notatka wygląda jak zwykły tekst i nie widać, że da się ją edytować; stany jako pigułki.

## Model

- Drzewo należy do ramy (drzewo nie pyta o typ): katalogi to model stanu kampanii, entity ma
  miejsce, notatkę, № i wpis, który może zniknąć z paczki. Licznik № leży w stanie kampanii. Entity
  bez miejsca leży w katalogu głównym.
- System podaje typy, które mają entity, ikonę typu, kartę i podpowiedź wiersza (PW). Katalog, paleta,
  okno podglądu i kontrolki części żywej są ramy; układ części żywej należy do karty systemu (karta
  jest projektowana).
- Do części żywej system podaje: wartości z bazą (klucz, podpis, baza z wpisu), śledzone wartości
  z rachunkiem (PW: bieżąca, maksimum, reguła `-`/`+`/`=`). Rama trzyma notatkę, stany, efekty, punkt
  odniesienia, sumowanie i kolory.
- Biurko jest pozycją kampanii w ramie; system nie deklaruje zakładki biurka, tylko podaje swoje
  narzędzia.
- Format kampanii zmienia się w miejscu, bez podnoszenia wersji (rozstrzygnięcie o wersjach). Zmiany
  formatu (notatka, punkt odniesienia, stany, efekty) pilnuje test zapisu bajt w bajt.
- Paleta wywołana z menu kontekstowego szuka nakładki przez okno główne, nie przez okienko menu.

## Wycinki

Podział, numery i stan wycinków: `docs/roadmap.md`, etap E2. Numery w tej specyfikacji (2.3b, 2.3c)
to numery stamtąd.

## Otwarte pytania

Rozstrzygnąć przed budową wycinka, którego dotyczą.

1. **Efekt na PW** (2.3e): zmienia tylko maksimum, a bieżące MG poprawia sam (propozycja; po zdjęciu
   efektu bieżące ponad nowym maksimum widać, nic nie blokuje), czy jak Pomoc z podręcznika — także
   bieżące?
2. **Rodzaje modyfikatorów** (2.3e): tylko dodawanie i odejmowanie (propozycja; podwojenie szybkości to
   „+9”), „ustaw na” (Zbroja maga) później?
3. **Dodawanie efektu** (2.3e): jedną linią w palecie — „KP +2 Tarcza wiary” i Enter (propozycja, przy
   stole szybciej) — czy formularzem?
4. **„Następna tura”** (etap E5): zatwierdza tylko istotę kończącą turę czy wszystkie?

## Poza etapem 2

- PW tymczasowe, liczniki i tyknięcia stanów, kolejka i „Do walki” z menu albo przeciągnięciem
  (etap 5; okno zmaksymalizowane zakrywa katalog, więc przeciąganie do kolejki rozstrzyga ten etap),
  postacie (4), entity w entity (9), podróż jako przeniesienie katalogu drużyny (11).
- **Hotbar** (pomysł na później) — element ramy nad oknami pośrodku dolnej krawędzi biurka, jak listek. Trzyma
  zminimalizowane okna (zamiast surowych kwadratów w lewym dolnym rogu) i sloty na
  miniatury: widżety, które wystawia okno, żeby jego najważniejsza wartość była widoczna bez całego
  okna (data i godzina świata z okna zegara). Naturalny moment: zegar świata (etap 11), pierwsze okno
  z widżetem. Licznik czasu sesji z mockupu wymaga zgody na wyjątek od „bez timerów”.
- **Cofnij / Ponów na listku** — odłożone. Ostatnie akcje MG w sesji, w pamięci do zamknięcia
  kampanii, jedna akcja to jeden krok; odwrotność zmiany liczy się ogólnie z poprzednich wersji
  rekordów, bez kodu w funkcjach. Zmienia „cofanie jako wymóg silnika” na liście odrzuconych.
  Częściowo zastępuje je „Odrzuć” na karcie. *Wyzwalacz:* pomyłka przy stole (zły cel, „-70” zamiast
  „-7”) zaczyna boleć.
- **Skrót z drzewa do edycji PW** — `-`, `+` albo `=` przy zaznaczonej entity otwiera pole zmiany
  bieżących PW w podglądzie. Odłożone: roadmapa, „Odłożone, z wyzwalaczem”.
