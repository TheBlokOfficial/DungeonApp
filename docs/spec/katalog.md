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
  i kliknięcie obok porzucają wpis (sekcja „Ceremonia zmiany”).
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
- Przy niepustym tekście na końcu listy zawsze stoją „Nowe stworzenie „…” bez wpisu” i „Nowy
  przedmiot „…” bez wpisu”.
- Ta sama paleta służy do przenoszenia i dodawania stanu i efektu, a później do „znajdź w świecie”
  i odnośników.

## Podgląd i karta

- Klik zaznacza. Dwuklik albo Enter otwiera **okno podglądu** — jedno na biurku — albo wyciąga je na
  wierzch. Otwarty podgląd idzie za zaznaczeniem: pokazuje ostatnio klikniętą entity, zaznaczenie
  katalogu go nie podmienia. Tryb edycji wyłącza się przy podmianie, żeby zmiana nie trafiła w następną
  entity.
- **Pinezka** w nagłówku podglądu odkłada bieżącą kartę do osobnego okna; podgląd dalej idzie za
  zaznaczeniem. Przypiętych okien może być kilka. Usunięcie entity zamyka jej przypięte okno, a podgląd
  zostaje pusty.
- Okno podglądu ma dwie warstwy: **pasek bieżący** u góry (wszystko, co należy do tej entity i zmienia
  się przy stole) i pod nim **kartę**. Linia między nimi to jedyny separator w oknie.
- **Karta jest statyczna i taka sama jak w bibliotece:** ścieżka, nazwa, tagi, bloki wartości, tabele
  cech i proza w tym samym układzie i z tymi samymi danymi co w zakładce treści. Podgląd nie dopisuje
  do karty ścieżki katalogów, № ani wpisu źródłowego i nie zmienia jej wyglądu; karta nie ma żywych
  kontrolek. Ścieżka katalogów i № stoją w pasku bieżącym.
- *Do zmiany w kodzie (wycinek 3a):* podgląd podmienia dziś ścieżkę karty na ścieżkę katalogów
  i dopisuje pod nazwą „№ 5 · Goblin” (`EntityCardBuilder`); karta wraca do postaci z biblioteki.

## Pasek bieżący

Nazwa robocza; przy budowie wchodzi do słownika w `docs/architecture.md`.

- Powierzchnia nad kartą, na tle innym niż okno, w podglądzie i w oknach przypiętych. Zostaje na górze
  okna, gdy karta pod nim się przewija (stoi poza przewijaną warstwą).
- Pasek to zarazem **zwarty widok istoty** z etapu 5: w oknie inicjatywy stoi ten sam pasek, a nie
  osobny widok.
- Zawartość:
  - **Tożsamość:** ścieżka katalogów entity jako druga ścieżka (breadcrumb), nazwa entity i №.
    W prawym górnym rogu „Zatwierdź” i „Odrzuć” (sekcja „Zatwierdzanie zmian”).
  - **PW:** podpis z ikoną serca „PW”, duża bieżąca liczba, obok mniejsze, przygaszone „/ 13” na
    wspólnej linii bazowej; pod nimi gruby, kwadratowy pasek od 0 do maksimum — minimalistyczny,
    od razu mówi, co się dzieje. Wypełnienie czerwone, z własnym tokenem (czerwień PW, nie błędu).
    PW ponad maksimum są widoczne (nadwyżka innym odcieniem, granica maksimum widoczna) i nie
    wyglądają jak błąd.
  - **KP i Szybkość** obok PW oraz **cechy w zwartej formie** (modyfikatory) — wartości bieżące, czyli
    baza z karty plus efekty (sekcja „Wartości z bazą”).
  - **Stany** — sekcja (sekcja „Stany”).
  - **Efekty** — sekcja (sekcja „Efekty”).
  - **Notatka MG** — wieloliniowa.
- Część przyklejona ma być zwarta, bo okno podglądu bywa małe. Makieta (sekcja „Projekt kontrolek”)
  stawiała notatkę pod częścią przyklejoną, przewijaną razem z kartą: notatkę czyta się przy otwarciu,
  nie w każdej turze. Które części są przyklejone — otwarte pytanie 5.
- Entity bez walki (przedmiot): pasek bez PW, KP, Szybkości i cech; zostają tożsamość, stany, efekty
  i notatka.
- *Ryzyko:* pasek rośnie z kampanią (postać w etapie 6: miejsca na zaklęcia z kilku kręgów, ładunki).
  Gdy zacznie zabierać pół okna, trzeba rozstrzygnąć, co stoi w nim zawsze (PW, stany), a co jest
  zwinięte albo zostaje na pełnej karcie postaci.

## Wartości z bazą

- KP, Szybkość, cechy i PW maks. mają **bazę** — wartość z karty entity (np. Szybkość goblina 9 m,
  modyfikator INT −2). Wartość w pasku = baza + suma modyfikatorów efektów. Model „baza +
  modyfikatory” powstaje od pierwszego wycinka, zanim efekty istnieją, żeby ich dodanie niczego nie
  przebudowywało.
- **Kolor względem bazy:** równa bazie — neutralna; powyżej — zielona; poniżej — czerwona, w tej samej
  konwencji co modyfikatory na karcie. Znaczenie jest inne niż na karcie (tam kolor mówi o znaku
  modyfikatora), więc to osobne tokeny o podobnych barwach. Przykład: INT −2 goblina jest neutralne,
  −3 czerwone; Szybkość 12 m zielona.
- Bieżące PW nie mają koloru względem maksimum — ranny goblin to stan zwykły, mówi o nim pasek PW.
- Pod wskaźnikiem rozpisanie: „15 = 13 + 2 Tarcza wiary” (reguła 6 granicy automatyzacji).
- System podaje listę wartości z bazą i skąd brać bazę; efekty, suma, kolory i rozpisanie należą do
  ramy, która nie wie, czym jest KP.

## Edycja w pasku

- **Wszystko w pasku edytuje się w miejscu, od razu, bez trybu edycji.** Karta zmienia się tylko
  w trybie edycji (wycinek 4). Rozstrzygnięcie „Tryb edycji karty istoty” odrzuciło „wszystko
  edytowalne od razu” z powodu przypadkowych zmian przy stole; w pasku ten powód rozwiązuje oznaczenie
  niezatwierdzonej zmiany i „Odrzuć”, więc odrzucenie zostaje tylko dla karty.
- **Liczba:** klik w wartość („8”) zamienia ją w pole w tym samym miejscu i rozmiarze. Wpisana liczba
  bez znaku ustawia wartość; Enter zatwierdza, Esc porzuca.
- **Narzędzie obrażeń i leczenia** — osobne od wyświetlania PW, ułatwia ich zmianę. Przyjmuje `-5`,
  `+3`, `=10`; liczba bez znaku nie przechodzi (rozstrzygnięcie „Pole zmiany liczby” dotyczy tego
  narzędzia, nie edycji w miejscu). Przed Enter zapowiada wynik („-5 → 3”). Rachunek należy do
  systemu:
  - `-` odejmuje (od etapu 5 najpierw PW tymczasowe) i schodzi najniżej do 0; rozpisanie pokazuje
    nadmiar („5 poniżej zera”);
  - `+` dodaje najwyżej do maksimum; rozpisanie pokazuje nadwyżkę („7 ponad maksimum”);
  - `=` ustawia dowolną wartość, także ponad maksimum (widać, nie blokuje).

  Wygląd i miejsce narzędzia — do projektu.
- **Notatka MG:** wieloliniowa, Enter dodaje linię. Zapis przy wyjściu z pola, przy zmianie entity
  w podglądzie i przy zamknięciu okna — nie przy każdym klawiszu ani z opóźnieniem. Musi wyglądać na
  edytowalną; pusta pokazuje przygaszony tekst zastępczy. Notatkę ma każda entity, także bez wpisu
  i z zepsutym wpisem.
- **Stany i efekty:** przycisk usunięcia przy wierszu, dodawanie na końcu sekcji przez paletę.

## Zatwierdzanie zmian

- Zmiana w pasku **działa i zapisuje się od razu**; przycisku „Zapisz” nie ma. Zapomniane
  zatwierdzenie niczego nie gubi, awaria laptopa nie zabiera zmian, a drzewo, inicjatywa i przełomy
  czytają jedną prawdę.
- Kampania pamięta dla każdej entity **punkt odniesienia** — stan z ostatniego zatwierdzenia. Wartość
  różna od niego jest niezatwierdzona i ma **niebieskawe tło**; kolor cyfr dalej mówi o bazie, więc oba
  sygnały się nie gryzą. Dodany stan albo efekt ma niebieskie tło, usunięty zostaje przygaszony do
  zatwierdzenia.
- Przy niezatwierdzonej liczbie widać wartość z punktu odniesienia: „było 13 · −5” (kilka zmian
  sumuje się względem punktu odniesienia). To jest ślad zmiany z reguły 6: trwa do zatwierdzenia
  i przetrwa zamknięcie kampanii. Zastępuje „ostatnią zmianę do następnej zmiany tej wartości”
  z rozstrzygnięcia „Pole zmiany liczby”.
- „Zatwierdź” robi z obecnego stanu entity nowy punkt odniesienia. „Odrzuć” przywraca punkt
  odniesienia i pyta o potwierdzenie, bo cofa wiele naraz. Przyciski stoją w prawym górnym rogu paska,
  **osobno dla każdej entity** (przy stole myśli się „goblin”, nie „wszystkie zmiany”); widoczne, gdy
  entity ma niezatwierdzone zmiany, z zarezerwowanym miejscem.
- Zasięg: tylko wartości paska (PW, wartości z bazą, stany, efekty, notatka). Operacje w drzewie
  (przeniesienie, nazwa, dodanie, usunięcie) nie mają oznaczeń.
- W walce „Następna tura” zatwierdza tak jak przycisk — to ten sam mechanizm co „Znaczniki zmian
  w turze” w `docs/decisions.md`. Które entity zatwierdza (tylko kończącą turę czy wszystkie) —
  otwarte pytanie 8, etap 5.
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
- Zmienia rozstrzygnięcie „Stany i efekty — jedna lista”: „efekt” znaczył tam własny stan MG
  („Płonie”), teraz znaczy modyfikator liczb. Są dwie listy i dwa wpisy w słowniku.

## Ceremonia zmiany

Każda zmiana świata ma zamiar przed i ślad po.

- **Zamiar:** żadna zmiana stanu entity nie wynika z jednego kliknięcia. Zmiana przychodzi z wpisania
  i Enter, z wyboru w palecie albo w oknie potwierdzenia, z przeciągnięcia z wyraźnym celem. Nie ma
  przycisków −/+, przełączników zmieniających stan, suwaków ani przełącznika obrażenia/leczenie. Klik
  w wartość w pasku otwiera pole i sam nic nie zmienia; przełącznik trybu edycji to tylko tryb.
- **Ślad:** w pasku niebieskie oznaczenie niezatwierdzonej zmiany; przy liczbach dodatkowo wartość
  sprzed zmiany („było”). Notatka i stany pokazują zmianę samym wyglądem — wartość sprzed zmiany
  dotyczy tylko liczb. W drzewie akcja zaznacza i odsłania to, czego dotknęła; powiadomienie
  („Przeniesiono 3 do: Jaskinia goblinów”) tylko wtedy, gdy skutku nie widać.
- Ceremonia zmniejsza ryzyko pomyłki, ale jej nie cofa: od tego „Odrzuć” i odłożone Cofnij.
- **Do poprawy w drzewie** (wycinek 3b):
  - zmiana nazwy w miejscu zatwierdza się dziś po kliknięciu obok — ma zatwierdzać tylko Enter;
    notatka jest wyjątkiem (Enter dodaje w niej linię, a utrata dłuższej notatki boli bardziej);
  - próg przeciągnięcia 4 px (`WorldCatalogView`, `DragThreshold`) podnieść do ok. 8 px, żeby drgnięcie
    przy kliknięciu nie przenosiło wiersza;
  - po przeniesieniu przewinąć drzewo do przeniesionych wierszy (katalog już się rozwija, wiersze
    zostają zaznaczone).

  Usunięcie ma okno potwierdzenia — spełnia regułę.
- Przy scaleniu reguła wchodzi do `docs/decisions.md`.

## Projekt kontrolek

- Pasek składa się z **rodziny kontrolek w dwóch formach** (tekst do czytania i pole), projektowanej
  raz: liczba edytowalna w miejscu, tekst wieloliniowy, wiersz stanu, wiersz efektu, narzędzie obrażeń
  i leczenia. Te same kontrolki zamieniają wartości karty w pola w trybie edycji (wycinek 4).
- Forma do czytania wygląda jak tekst karty; pole zajmuje dokładnie to samo miejsce. Edytowalność
  komunikuje cała strefa: inne tło paska znaczy „tu się edytuje”, na karcie nic się nie edytuje.
- Warianty do porównania na jednym arkuszu:
  - **A** — w spoczynku czysty tekst; pod wskaźnikiem delikatne tło, ramka i kursor tekstowy.
    Najczyściej, ale trudniej odkryć za pierwszym razem. Propozycja.
  - **B** — stały, cichy ślad (przerywane podkreślenie albo słabe tło), pod wskaźnikiem pełna ramka.
  - **C** — stała ramka; wygląda jak formularz, tylko dla porównania.

  Pusta notatka zawsze pokazuje tekst zastępczy, w każdym wariancie.
- Stany, które każda kontrolka ma zaprojektowane: spoczynek, pod wskaźnikiem, edycja; błędny wpis
  („abc” w liczbie — Enter nic nie robi, pole to pokazuje); zapowiedź wyniku przed Enter;
  niezatwierdzona zmiana; powyżej i poniżej bazy, także razem z niezatwierdzoną; brak wartości („—”);
  notatka pusta, w jednej i w kilku liniach (rośnie w dół); fokus klawiatury (przy stole przechodzi
  się Tabem, więc tu fokus dostaje wygląd mimo „Szlifu” w roadmapie).
- **Kolejność pracy:** arkusz rodziny w Galerii — projektant (Opus), iteracje z oglądaniem renderu,
  render 2× — potem wybór wariantu, dopiero potem kompozycja paska z tych kontrolek. Zlecenie projektu
  podaje model interakcji i kontrolki, nie listę widżetów do ustawienia.
- Render paska: kadr samego okna podglądu w skali 2×, bez katalogu i innych okien.
- **Makieta paska** (gałąź `projekt-pasek-biezacy`) to przykład, nie wzór kompozycji. Z niej zostaje:
  duża liczba PW z mniejszym „/ 13”, podpis z ikoną serca, minimalistyczny pasek PW, pasek na liniach
  karty, render 2× (`SaveScaled` w `tools/render/Program.cs`, do przeniesienia). Słabe w niej: notatka
  wygląda jak zwykły tekst i nie widać, że da się ją edytować; stany jako pigułki; separatory wokół
  notatki; brak kontrolek dwóch form.

## Model

- Drzewo należy do ramy (drzewo nie pyta o typ): katalogi to model stanu kampanii, entity ma
  miejsce, notatkę, № i opcjonalny wpis — bez wpisu niesie typ. Licznik № leży w stanie kampanii.
  Entity bez miejsca leży w katalogu głównym.
- System podaje typy, które mają entity, ikonę typu, kartę i podpowiedź wiersza (PW). Katalog, paleta,
  okno podglądu i pasek bieżący są ramy.
- Do paska system podaje: wartości z bazą (klucz, podpis, baza z wartości entity), śledzone wartości
  z rachunkiem (PW: bieżąca, maksimum, reguła `-`/`+`/`=`). Rama trzyma notatkę, stany, efekty, punkt
  odniesienia, sumowanie, kolory i rozpisanie.
- Biurko jest pozycją kampanii w ramie; system nie deklaruje zakładki biurka, tylko podaje swoje
  narzędzia.
- Format kampanii zmienia się w miejscu, bez podnoszenia wersji (rozstrzygnięcie o wersjach). Zmiany
  formatu (notatka, punkt odniesienia, stany, efekty) pilnuje test zapisu bajt w bajt.
- Paleta wywołana z menu kontekstowego szuka nakładki przez okno główne, nie przez okienko menu.

## Wycinki

1. **Biurko w miejscu strony kampanii** — zbudowane (`docs/architecture.md`, „Biurko”, „Listek”,
   „Maksymalizacja okna”).
2. **Katalog na biurku i paleta** — zbudowane (`docs/architecture.md`, „Paleta”, „Katalog świata”,
   „Podgląd i przypięte karty”).
3. **Pasek bieżący**, poprzedzony arkuszem rodziny kontrolek (sekcja „Projekt kontrolek”):
   - **3a. Wartości:** karta wraca do postaci z biblioteki; pasek z tożsamością (ścieżka katalogów,
     nazwa, №), PW (edycja w miejscu, narzędzie obrażeń i leczenia), KP, Szybkość, zwarte cechy jako
     baza + modyfikatory; notatka.
   - **3b. Zatwierdzanie:** punkt odniesienia, niebieskie oznaczenie, „było…”, Zatwierdź i Odrzuć;
     poprawki ceremonii w drzewie.
   - **3c. Stany:** sekcja, dodawanie z palety, usuwanie, stany domyślne wpisu.
   - **3d. Efekty:** sekcja, dodawanie, usuwanie, sumowanie, kolory względem bazy, rozpisanie.
4. **Tryb edycji karty i entity bez wpisu:** pola w miejscach wartości karty (te same kontrolki co
   w pasku), znacznik różnicy, „Przywróć z wpisu”, „—” dla pustych.

## Otwarte pytania

Rozstrzygnąć przed budową wycinka, którego dotyczą.

1. **Efekt na PW** (3d): zmienia tylko maksimum, a bieżące MG poprawia sam (propozycja; po zdjęciu
   efektu bieżące ponad nowym maksimum widać, nic nie blokuje), czy jak Pomoc z podręcznika — także
   bieżące?
2. **Rodzaje modyfikatorów** (3d): tylko dodawanie i odejmowanie (propozycja; podwojenie szybkości to
   „+9”), „ustaw na” (Zbroja maga) później?
3. **Dodawanie efektu** (3d): jedną linią w palecie — „KP +2 Tarcza wiary” i Enter (propozycja, przy
   stole szybciej) — czy formularzem?
4. **Podział i kolejność:** arkusz kontrolek, potem 3a–3d jak wyżej?
5. **Co jest przyklejone** (3a): propozycja — tożsamość, PW, wartości z bazą i stany; notatka pod
   spodem, przewijana z kartą. Gdzie efekty?
6. **Karta: wpis czy entity** (3a, 4): karta ma być statyczna jak w bibliotece. Czy pokazuje czysty
   wpis, czy wartości entity po nałożeniu łatki (odchylenia z trybu edycji: podkręcone KP, PW maks.)?
   Jeśli czysty wpis — odchylenia widać tylko w pasku jako bazę, a tryb edycji trzeba umieścić od
   nowa. Co pokazuje podgląd entity bez wpisu: pustą kartę typu czy sam pasek?
7. **Nazwa elementu:** „pasek bieżący” jest robocza.
8. **„Następna tura”** (etap 5): zatwierdza tylko istotę kończącą turę czy wszystkie?

## Poza etapem 2

- PW tymczasowe, liczniki i tyknięcia stanów, kolejka i „Do walki” z menu albo przeciągnięciem
  (etap 5; okno zmaksymalizowane zakrywa katalog, więc przeciąganie do kolejki rozstrzyga ten etap),
  postacie (4), entity w entity (9), podróż jako przeniesienie katalogu drużyny (11).
- **Hotbar** (pomysł na później) — element ramy nad oknami pośrodku dolnej krawędzi biurka, jak listek. Trzyma
  zminimalizowane okna (zamiast dzisiejszych surowych kwadratów w lewym dolnym rogu) i sloty na
  miniatury: widżety, które wystawia okno, żeby jego najważniejsza wartość była widoczna bez całego
  okna (data i godzina świata z okna zegara). Naturalny moment: zegar świata (etap 11), pierwsze okno
  z widżetem. Licznik czasu sesji z mockupu wymaga zgody na wyjątek od „bez timerów”.
- **Cofnij / Ponów na listku** — odłożone. Ostatnie akcje MG w sesji, w pamięci do zamknięcia
  kampanii, jedna akcja to jeden krok; odwrotność zmiany liczy się ogólnie z poprzednich wersji
  rekordów, bez kodu w funkcjach. Zmienia „cofanie jako wymóg silnika” na liście odrzuconych.
  Częściowo zastępuje je „Odrzuć” w pasku. *Wyzwalacz:* pomyłka przy stole (zły cel, „-70” zamiast
  „-7”) zaczyna boleć.
- **Skrót z drzewa do edycji PW** — `-`, `+` albo `=` przy zaznaczonej entity przenosi kursor do
  narzędzia obrażeń i leczenia w podglądzie. Odłożone: roadmapa, „Odłożone, z wyzwalaczem”.
