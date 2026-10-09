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
- Zmiana nazwy w miejscu: pole w tym samym miejscu i rozmiarze co tekst.
- Przeciąganie: wiersz główny przesuwa katalog po biurku; każdy inny wiersz przenosi zaznaczenie do
  katalogu, na który się je upuści. Katalog przenosi się z zawartością i nie wejdzie do własnego
  potomka. „Przenieś do…” otwiera paletę z listą katalogów.

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
- Ta sama paleta służy do przenoszenia i dodawania stanu, a później do „znajdź w świecie” i odnośników.

## Podgląd i karta entity

- Klik zaznacza. Dwuklik albo Enter otwiera **okno podglądu** — jedno na biurku — albo wyciąga je na
  wierzch. Otwarty podgląd idzie za zaznaczeniem: pokazuje ostatnio klikniętą entity, zaznaczenie
  katalogu go nie podmienia. Tryb edycji wyłącza się przy podmianie, żeby zmiana nie trafiła w następną
  entity.
- **Pinezka** w nagłówku podglądu odkłada bieżącą kartę do osobnego okna; podgląd dalej idzie za
  zaznaczeniem. Przypiętych okien może być kilka. Usunięcie entity zamyka jej przypięte okno, a podgląd
  zostaje pusty.
- Karta jest ta sama co w bibliotece, różnią się dane. Ścieżka pokazuje katalogi entity; przy nazwie
  stoi № i wpis źródłowy („№ 5 · Goblin”, „№ 9 · bez wpisu”). Ścieżka, portret, nazwa i tabele stoją
  tam, gdzie na karcie wpisu.
- Pod tabelami cech, za separatorami z nagłówkami:
  - **Wartości bieżące:** gruby, kwadratowy pasek PW od 0 do maksimum i pole zmiany (`-7`, `+5`, `=30`,
    Enter zapisuje); przy wartości ostatnia zmiana („3, było 7, −4”). PW nie schodzą poniżej 0 —
    rozpisanie pokazuje nadmiar; PW ponad maksimum widać, nie są blokowane.
  - **Stany**, gdy są.
  - **Notatka MG** — zawsze pole, pusta z przygaszonym tekstem zastępczym. Wyjątek od „puste się nie
    pokazuje”: notatka pojawiająca się dopiero w trybie edycji przesuwałaby kartę.
- **Tryb edycji** (przełącznik w nagłówku okna): nazwa, KP, PW maks., Szybkość i cechy; u przedmiotu
  waga i wartość. Wartość różna od wpisu ma znacznik i „Przywróć z wpisu”. Proza i tagi zostają do
  odczytu — odstępstwa od nich MG zapisuje w notatce.
- **Entity bez wpisu:** typ wybrany w palecie, karta tego typu; puste wartości jako „—” (bloki, tabela
  cech), wypełniane w trybie edycji.
- **Stany:** dodawanie z palety (stan z paczki albo własny: nazwa, ikona domyślna), usuwanie. Stany
  domyślne wpisu stworzenia entity dziedziczy, dopóki MG ich nie zmieni.

## Model

- Drzewo należy do ramy (drzewo nie pyta o typ): katalogi to nowy model stanu kampanii, entity
  (dziś instancja) dostaje miejsce, notatkę, № i opcjonalny wpis — bez wpisu niesie typ. Licznik №
  leży w stanie kampanii. Entity bez miejsca leży w katalogu głównym.
- System podaje typy, które mają entity, ikonę typu, kartę i podpowiedź wiersza (PW). Katalog, paleta
  i okno podglądu są ramy.
- Biurko staje się pozycją kampanii w ramie; system nie deklaruje już zakładki biurka, tylko podaje
  swoje narzędzia.
- Format kampanii zmienia się w miejscu, bez podnoszenia wersji (rozstrzygnięcie o wersjach).
- Paleta wywołana z menu kontekstowego szuka nakładki przez okno główne, nie przez okienko menu.

## Wycinki

1. **Biurko w miejscu strony kampanii** — zbudowane (`docs/architecture.md`, „Biurko”, „Listek”,
   „Maksymalizacja okna”).
2. **Katalog na biurku i paleta:** katalogi, dodawanie z liczbą sztuk, №, przenoszenie, zmiana nazwy,
   usuwanie; podgląd z pinezką pokazuje kartę tylko do odczytu. „Świat kampanii” znika.
3. **Wartości bieżące:** pasek PW, pole zmiany z ostatnią zmianą, notatka.
4. **Tryb edycji i entity bez wpisu:** pola w miejscach wartości, znacznik różnicy, „Przywróć z
   wpisu”, „—” dla pustych.
5. **Stany na karcie:** sekcja stanów, dodawanie z palety, usuwanie, stany domyślne wpisu.

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
  *Wyzwalacz:* pomyłka przy stole (zły cel, „-70” zamiast „-7”) zaczyna boleć.
