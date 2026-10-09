# Co dalej

Kolejność od góry. Pozycja jest opisem potrzeby, nie zleceniem — przed pracą sprawdź w kodzie, czy
nadal jest aktualna. Pozycję zrobioną usuwa się przy scaleniu wycinka, który ją zamyka; to, co
zbudowane, opisuje `docs/architecture.md`.

## Kamień milowy: pierwsza sesja przy stole

Cel: MG prowadzi prawdziwą sesję bez podręcznika i bez własnych notatek na papierze (gracze grają
z kartek, aplikacja jest źródłem prawdy). Etapy w kolejności pracy; każdy jest użyteczny sam. W środku
punkt kontrolny „Pierwsza walka”. Rozstrzygnięcia, na których stoi plan: `docs/decisions.md`.

1. **Treść** — biblioteka wpisów (zbudowana: architecture.md). Projekt kart jest punktem wyjścia, nie
   specyfikacją: `docs/archive/zadania/zakladki-tresci.md`, sekcja *C*.
   - **Paczka `dnd5e-srd`: SRD 5.1 po polsku**, rośnie na żądanie: zestaw startowy stworzeń na
     pierwsze sesje, reszta, gdy przygoda jej potrzebuje. Słownictwo i spis stworzeń: `tools/srd/`.
     W prozie znaczniki `[[id|tekst]]` do zaklęć, stanów, przedmiotów i istot. Odległości w metrach
     (1,5 m za 5 stóp).
   - **Rzadkość z SRD:** niemagiczne i pospolite → Pospolity, niezwykłe → Niepospolity, rzadkie →
     Rzadki, bardzo rzadkie → Epicki, legendarne i artefakty → Legendarny.
   - **Biblioteka rośnie w długi rząd płaskich zakładek** (stany, zaklęcia, manewry, atuty). Kierunek:
     zakładki zostają, część grupuje się w rozwijaną listę (Stany nie na pierwszym poziomie).
     Rozstrzygnąć przed etapem 8; zmienia „Zakładki według typu” w `docs/decisions.md`.
   - **Szkielet zakładki:** każda zakładka ma ten sam układ (filtry, lista, szczegół) — za generyczny,
     do przemyślenia.
2. **Katalog świata** — specyfikacja i podział na wycinki: `docs/spec/katalog.md`. Zbudowane: drzewo
   katalogów z entity na biurku, paleta dodawania, podgląd z pinezką. Dalej karta entity: pole zmiany
   PW, notatka, tryb edycji, entity bez wpisu, stany.
3. **Kopie zapasowe kampanii**, rotujące — przed pierwszym prawdziwym użyciem, bo przy stole zapis
   biegnie na żywo po każdej zmianie.
4. **Postacie w minimum** — leżą obok kampanii, w katalogu systemu na dysku, i mogą grać w wielu
   kampaniach. W tym etapie nazwa i aspekt walki (KP, PW, cechy, inicjatywa) z formularza; w etapie 6
   postać rośnie o kolejne aspekty. Wejście postaci do kampanii tworzy jej entity. Identyfikator
   `paczka:id` staje się `źródło:id`: wpis w paczce albo postać.
5. **Inicjatywa i walka** — okno: MG wskazuje uczestników i układa kolejkę; rundę i turę przesuwa MG.
   - Zwarty widok istoty (PW z polem zmiany, lista „Stany i efekty”); karta zostaje do czytania.
   - Przełom „Następna tura” z okienkiem przełomu, znaczniki zmian w turze.
   - Kalkulator kości jako osobne okno biurka.
   - Rozstrzygnąć wyjątek od reguły 3 granicy (rzut istoty zostaje u MG): czy narzędzie rzuca
     stworzeniom inicjatywę (k20 + mod. ZRĘ z aspektu walki, z rozpisaniem, do poprawienia).

**Punkt kontrolny „Pierwsza walka”:** jedna prawdziwa walka poprowadzona w całości z aplikacji.
Uwagi autora poprawiają księgę, przełom i karty, zanim powstanie na nich pełna postać.

6. **Pełna postać** — pełna karta (aplikacja to jedyne źródło prawdy), wartości liczone
   z rozpisaniem, formularz do szybkiego przepisania istniejącej kartki gracza. Do paczki dochodzą
   pochodzenie i tło.
7. **Klasy i drzewka** — klasa i każdy węzeł to wpisy w paczce, drzewko to widok. MG zaznacza węzeł
   i odblokowuje go postaci; niespełnione wymaganie widać, ale nie blokuje. SRD: cechy klas
   i podklas do 5. poziomu.
8. **Zaklęcia** bez obrazka; filtry i sortowanie po poziomie i szkole, koncentracja i rytuał jako
   tagi. SRD: zaklęcia 0–3.
9. **Ekwipunek, sakiewka, handel.**
   - Złoto na entity. Cena i ilość należą do slotu (sklep, plecak), nie do wpisu.
   - Okno handlu jak w cRPG: towar kupca z cenami (z wartości wpisu przez kurs waluty kampanii,
     sprzedaż za połowę), MG może zmienić cenę, „Kup” i „Sprzedaj” przenoszą przedmiot i złoto. Brak
     złota widać, nie blokuje.
   - Waluta kampanii to lista nominałów z mnożnikami (uniwersalna moneta albo własne monety); funty
     to samo dla wagi.
   - SRD: ekwipunek podstawowy, przedmioty magiczne pospolite do rzadkich; przed wyceną ustalić skalę
     wartości (małe, czytelne liczby z miejscem w dół i w górę).
   - Rozstrzygnąć: czy ekwipunek to entity w entity w drzewie świata, czy slot — lista pozycji
     wewnątrz istoty.
   - Rozstrzygnąć: stworzenie na sprzedaż (koń u handlarza) — skąd wartość, z której liczy się cena.
10. **Odpoczynek** — przełom krótkiego i długiego odpoczynku z okienkiem: odnowienie PW, miejsc na
    zaklęcia i ładunków. Rozstrzygnąć odnowienie „o świcie”; propozycja: przełącznik w okienku
    długiego odpoczynku, bo zegar niczego nie uruchamia.
11. **Czas świata i podróże** — okno zegara przesuwanego przez MG (szybkie przyciski i dowolna
    wartość). Podróż to jedna akcja: drużyna do innego katalogu, zegar o czas wpisany przez MG.

## Po kamieniu milowym

- **Fabuła i lore** — zakładka dokumentów. Markdown z paczki, pisany poza aplikacją (np.
  w Obsidianie). Linki między stronami i do identyfikatorów; klik w identyfikator pokazuje kartę.
  Odhaczenia decyzji w fabule zapisują się w kampanii, więc jedna przygoda może iść dla kilku grup.
- **Ściągawki** — wybór wpisów (cechy klas, zaklęcia, przedmioty, stany) i wydruk A4 przez PDF.
  Każdy typ ma własny układ na białą kartkę. Zaznaczenia może podpowiedzieć postać (jej węzły,
  zaklęcia, ekwipunek); MG poprawia je przed drukiem.
- **Galeria wpisów** — zakładka treści na całą szerokość.
  - Filtry i sortowanie paskiem u góry; po lewej wąski spis treści (wyszukiwarka, grupy paczek,
    odrzucone wpisy); obok karty w 1–3 kolumnach stałej szerokości zależnie od okna, ułożone
    wierszami (karta do najkrótszej kolumny).
  - Długa karta przycięta: dół gaśnie gradientem przezroczystości, przycisk „Rozwiń”.
  - Klik w spisie przewija do karty i oznacza ją ramką akcentu (krótkie rozbłyśnięcie, potem stała
    ramka); klik w kartę zaznacza wiersz.
  - Wymaga wirtualizacji kart o różnej wysokości (200+ stworzeń).
- **Formuły w danych paczki** — pola liczone, które dopisuje twórca paczki.

## Porządki w kodzie i dokumentach

- **Komentarze nieaktualne w treści:** `CampaignRowViewModel` (powód niedostępności, którego wiersz nie
  pokazuje), `AppShellView.axaml` (host rozgrzewki „także po wyborze systemu”), `PanelCatalog`
  (kolejność „panele powłoki, potem narzędzia”).
- Punkt 4 granicy (zdarzenia nie zapisują) i zakaz logiki per wpis nie mają strażnika w kodzie —
  pilnuje ich przegląd.
- **Powody odrzucenia wpisu paczki** są surowym angielskim tekstem parsera („The JSON value could not
  be converted to …”) — potrzebne polskie zdanie, które nazywa pole.

## Szlif — po kamieniu milowym

Wygląd, który działa, ale mógłby być lepszy. Nie blokuje kamienia milowego.

- Pełna nazwa przedmiotu po najechaniu znika przy zjechaniu na dopisaną część i po kliknięciu
  w tytuł — okienko musiałoby przyjmować wskaźnik i samo być tekstem do zaznaczenia.
- Tagi w nagłówku karty przy wielu wartościach zawijają się w drugą linię i wypychają nagłówek ponad
  obrazek (dlatego Miecz worpalny nie ma podtypu „żołnierska, do walki wręcz”).
- Tabela cech: modyfikator zero „+0” jak w podręczniku — do zmiany na „0”, gdyby zaczęło razić.
- Opis przedmiotu bez kreski i nagłówka „Opis”, z inicjałem pergaminu jak zasady na karcie stanu.
- Akapity w prozie: po twardym Enter nowa linia trochę niżej, jak w edytorze tekstu (także obok
  inicjału pergaminu).
- Suwak: uchwyt w spoczynku trochę za ciemny. Pomysł: pod myszą obwódka zamiast rozjaśnienia.
- Menu: skrót nie stoi w jednej linii ze strzałką podmenu, podmenu nachodzi na menu.
- Ikony kategorii na liście treści (przedmioty: broń, zbroja…; stworzenia: typ), małe i przygaszone
  jak ikony stanów; ta sama zamiast zastępczej w pustym polu obrazka, spoza listy — domyślna.
- Przełączanie zakładek bez efektu przenikania (fade) — zmiana ma być natychmiastowa. Usunąć też
  gradient po prawej stronie biblioteki kampanii.
- Widoczny wygląd fokusu klawiatury.

## Po stronie autora

- Kampanie przenieść do `Dokumenty\DungeonApp\dnd5e\campaigns\` (katalogów `Packs` i `Campaigns`
  aplikacja nie czyta) i dopisać stworzeniom pole `group`.
- Wpisy we własnych paczkach bez obiektów `combat` i `item` pokazują się jako odrzucone — przepisać
  albo usunąć. Stworzenie: `"template": "dnd5e:creature"`, KP, PW i cechy w obiekcie `combat`, proza
  w sekcjach. Przedmiot: waga i wartość w obiekcie `item`, ładunki w `charges` (`max`, `recharge`).
  Wzór: wpisy paczki `dnd5e-srd`.

## Odłożone, z wyzwalaczem

- **„Zapisz jako nowy wpis”** z karty entity do własnej paczki MG; entity wskazuje potem nowy wpis —
  MG chce użyć podkręconej entity albo entity bez wpisu w innej przygodzie.
- **„Zmień wpis źródłowy”** entity (goblin okazuje się hobgoblinem; nazwa, notatka, bieżące PW i stany
  zostają) — pierwsza taka podmiana obchodzona przy stole usunięciem i dodaniem.
- **Edycja kilku zaznaczonych entity naraz** — MG zmienia to samo pole po kolei na kilku.
- **Typ „Zdolność”** z kategorią (atut, manewr, inwokacja, metamagia, styl walki) — pierwsza
  zdolność spoza drzewek klas; do tego czasu wybory w klasie są węzłami.
- **Odnośniki do wiedzy** (klik w zaklęcie albo stan w prozie otwiera kartę; na biurku jako pływające
  okno), słownik „Hasło” i zakładka „Zasady” — MG zbyt często szuka opisu wspomnianego w prozie.
- **Edytor treści w aplikacji** (wpisy, przygoda, lore) — pisanie plików poza aplikacją zaczyna
  przeszkadzać w przygotowaniu sesji.
- **Odległości między lokacjami** (propozycja czasu podróży) — wpisywanie czasu podróży zaczyna męczyć.
- **Warianty zasad** — pierwszy wariant, który autor chce mieć w konkretnej kampanii.
- **Dodanie paczki przeciągnięciem do okna** (kolizja nazwy daje odmowę z komunikatem, nigdy
  nadpisanie) — kopiowanie katalogów paczek w Eksploratorze zaczyna przeszkadzać.
- **Wczytanie paczek od nowa** — przycisk w zakładce treści i F5; wybór zostaje, jeśli wpis o tym id
  nadal istnieje. Restart po każdej poprawce paczki zaczyna przeszkadzać przy przygotowaniu sesji.
- **Pomiar startu** — pierwsze zauważalne zacięcie przy starcie; wtedy najpierw liczby, potem
  poprawka.
