# Co dalej

Kolejność od góry. Pozycja jest opisem potrzeby, nie zleceniem — przed pracą sprawdź w kodzie, czy
nadal jest aktualna. Pozycję zrobioną usuwa się w commicie, który ją zamyka.

## Kamień milowy: pierwsza sesja przy stole

Cel: MG prowadzi prawdziwą sesję bez podręcznika i bez własnych notatek na papierze (gracze grają
z kartek, aplikacja jest źródłem prawdy). Etapy w kolejności pracy; każdy jest użyteczny sam. W środku
punkt kontrolny „Pierwsza walka”. Rozstrzygnięcia, na których stoi plan: `docs/decisions.md`.

1. **Treść.** Gotowe: lista z wyszukiwaniem, filtrami i sortowaniem, szczegół wpisu, pole obrazka
   z ramką. Projekt kart jest punktem wyjścia, nie specyfikacją:
   `docs/archive/zadania/zakladki-tresci.md`, sekcja *C. Projekt szkieletu i kart*.
   - **Stany** (Powalony, Ogłuszony…) jako typ wpisu — przy stole i na ściągawce. Wpis stanu ma
     skrót (jedno–dwa zdania) i ikonę z paczki, bo karta pokazuje stan wierszem: ikona, nazwa,
     skrót. Wpis stworzenia może mieć stany domyślne (np. stała niewidzialność). Na istocie stan jest
     pozycją listy „Stany i efekty”.
   - **Paczka `dnd5e-srd`: SRD 5.1 po polsku do 5. poziomu postaci** (CC-BY 4.0). Tłumaczy agent
     w tle, typ po typie, gdy rekord i karta typu są gotowe; tu stworzenia do wyzwania 5 i stany,
     resztę etapy 6–9. W prozie znaczniki `[[id|tekst]]` do zaklęć, stanów, przedmiotów i istot (karta
     pokazuje sam tekst), żeby odnośniki nie wymagały drugiego przejścia. Odległości w metrach (1,5 m
     za 5 stóp). Rzadkość: niemagiczne i pospolite → Pospolity, niezwykłe → Niepospolity, rzadkie →
     Rzadki, bardzo rzadkie → Epicki, legendarne i artefakty → Legendarny. Dziś paczka ma pięć
     stworzeń i dziesięć przedmiotów przykładowych (Pierścień regeneracji i Miecz worpalny ponad
     zakresem).
2. **Katalog świata** — okno biurka zamiast testowego „Świata kampanii”. Drzewo w zapisie kampanii:
   jeden katalog główny, podkatalogi, entity (dziś instancja) w katalogach. Katalog to nazwa
   i zawartość, bez opisu. Dodawanie z wyszukiwaniem, kilka sztuk naraz z numeracją („Goblin 1–4”).
   Nazwa entity (dziś operacja zmiany nazwy jest, pola w interfejsie nie ma) i notatka MG.
   Entity bez wpisu (imię, notatka, opcjonalnie PZ i KP). Przenoszenie entity i katalogów; usuwa
   się tylko pusty katalog. Klik w entity otwiera okno jego karty — kilka naraz — z polem zmiany PZ
   (`-7`, `+5`, `=30`, Enter zapisuje) i trybem edycji karty.
   Karta jest jedna dla wpisu i entity; różnią się tylko danymi. Sekcje wartości bieżących (gruby,
   kwadratowy pasek PZ od 0 do maksimum z polem szybkiej zmiany) i stanów (pod tabelą cech, za
   separatorem z nagłówkiem) pokazują się, gdy są wypełnione. Breadcrumb, portret, nazwa i tabele
   nie zmieniają miejsca. Entity dziedziczy stany wpisu, dopóki MG ich nie zmieni.
3. **Kopie zapasowe kampanii**, rotujące — przed pierwszym prawdziwym użyciem. Przy stole zapis
   biegnie na żywo po każdej zmianie.
4. **Postacie w minimum** — leżą obok kampanii, w katalogu systemu, i mogą grać w wielu kampaniach.
   Na razie nazwa i aspekt walki (KP, PZ, cechy, inicjatywa) z formularza; w etapie 6 postać rośnie
   o kolejne aspekty. Wejście postaci do kampanii tworzy jej entity. Identyfikator `źródło:id`
   wskazuje odtąd wpis w paczce albo postać.
5. **Inicjatywa i walka** — okno: MG wskazuje uczestników i układa kolejkę; rundę i turę przesuwa
   MG. Zwarty widok istoty (PZ z polem zmiany, lista „Stany i efekty”), karta zostaje do czytania.
   Przełom „Następna tura” z okienkiem bocznym, znaczniki zmian w turze. Kalkulator kości jako
   osobne okno biurka. Rozstrzygnąć: czy narzędzie rzuca stworzeniom inicjatywę (k20 + mod. ZRĘ
   z aspektu walki, z rozpisaniem, do poprawienia).

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
9. **Ekwipunek, sakiewka, handel** — entity w entity (plecak postaci jako gałąź drzewa świata), złoto
   na entity. Cena i ilość leżą na slocie (sklep, plecak), nie we wpisie. Okno handlu jak w cRPG:
   towar kupca z cenami (z wartości wpisu przez kurs waluty kampanii, sprzedaż za połowę), MG może
   zmienić cenę, „Kup” i „Sprzedaj” przenoszą przedmiot i złoto. Waluta kampanii to lista nominałów
   z mnożnikami (uniwersalna moneta albo własne monety); funty to samo dla wagi. Brak złota widać,
   nie blokuje. SRD: ekwipunek podstawowy, przedmioty magiczne pospolite do rzadkich; przed wyceną
   ustalić skalę wartości (małe, czytelne liczby z miejscem w dół i w górę).
10. **Odpoczynek** — przełom krótkiego i długiego odpoczynku z okienkiem: odnowienie PZ, miejsc na
    zaklęcia i ładunków. Rozstrzygnąć odnowienie „o świcie” (*w długim odpoczynku z wyłącznikiem*;
    zegar niczego nie uruchamia).
11. **Czas świata i podróże** — okno zegara przesuwanego przez MG (szybkie przyciski i dowolna
    wartość). Podróż to jedna akcja: drużyna do innego katalogu, zegar o czas wpisany przez MG.

## Po kamieniu milowym

- **Fabuła i lore** — zakładka dokumentów. Markdown z paczki, pisany poza aplikacją (np.
  w Obsidianie). Linki między stronami i do identyfikatorów; klik w identyfikator pokazuje kartę.
  Odhaczenia decyzji w fabule zapisują się w kampanii, więc jedna przygoda może iść dla kilku grup.
- **Ściągawki** — wybór wpisów (cechy klas, zaklęcia, przedmioty, stany) i wydruk A4 przez PDF.
  Każdy typ ma własny układ na białą kartkę. Zaznaczenia może podpowiedzieć postać (jej węzły,
  zaklęcia, ekwipunek); MG poprawia je przed drukiem.

## Porządki w kodzie i dokumentach

- **Komentarze po polsku** w ok. 40 plikach — przetłumaczyć na angielski (konwencja: kod i komentarze
  po angielsku). `tools/comment-hits.py` wskazuje też komentarze z historią.
- **Komentarze nieaktualne w treści:** `CampaignRowViewModel` (powód niedostępności, którego wiersz nie
  pokazuje), `AppShellView.axaml` (host rozgrzewki „także po wyborze systemu”), `PanelCatalog`
  (kolejność „panele powłoki, potem narzędzia”).
- **Dokumenty:** dodatki wobec wariantów.
- Punkt 4 granicy (zdarzenia nie zapisują) i zakaz logiki per wpis nie mają strażnika w kodzie —
  pilnuje ich przegląd.
- **Powody odrzucenia wpisu paczki** są surowym angielskim tekstem parsera („The JSON value could not
  be converted to …”) — potrzebne polskie zdanie, które nazywa pole.

## Szlif — po kamieniu milowym

Wygląd, który działa, ale mógłby być lepszy. Nie blokuje kamienia milowego; wracamy, gdy będzie czas.

- Pełna nazwa przedmiotu po najechaniu znika, gdy wskaźnik zjedzie na jej dopisaną część, i po
  kliknięciu w tytuł — nie da się zaznaczyć całej nazwy. Okienko musiałoby przyjmować wskaźnik
  i samo być tekstem do zaznaczenia.
- Tagi w nagłówku karty przy wielu wartościach zawijają się w drugą linię i wypychają nagłówek ponad
  obrazek (dlatego Miecz worpalny nie ma podtypu „żołnierska, do walki wręcz”).
- Tabela cech: modyfikator zero zostaje „+0” — tak pisze podręcznik, a każdy modyfikator ma znak.
  Do ewentualnej zmiany na „0”, gdyby zaczęło razić.
- Sekcja opisu przedmiotu bez kreski i nagłówka „Opis” (opis ma prawie każdy przedmiot). Ikona
  pergaminu w lewym górnym rogu, wysoka na dwie linie tekstu, a tekst ją opływa jak inicjał
  w manuskrypcie: obok ikony z odstępem, pod nią od lewej krawędzi. Nadal jest sekcją.
- Suwak: uchwyt w spoczynku trochę za ciemny. Pomysł: pod myszą obwódka zamiast rozjaśnienia.
- Menu: skrót nie stoi w jednej linii ze strzałką podmenu, podmenu nachodzi na menu.
- Ikony kategorii na liście treści (przedmioty: broń, zbroja, różdżka…; stworzenia: typ) — małe,
  przygaszone; ta sama ikona zamiast zastępczej w pustym polu obrazka. Kategoria spoza listy dostaje
  ikonę domyślną.
- Przełączanie zakładek bez efektu przenikania (fade) — zmiana ma być natychmiastowa. Usunąć też
  gradient po prawej stronie biblioteki kampanii.

## Po stronie autora

- Przenieść stare kampanie do `Dokumenty\DungeonApp\dnd5e\campaigns\` (stare `Packs`/`Campaigns` nie są
  czytane) i dopisać stworzeniom pole `group`.
- Przepisać albo usunąć stworzenia we własnych paczkach w starym formacie (proza jako zwykły napis) —
  pokazują się jako odrzucone wpisy.

## Odłożone, z wyzwalaczem

- **„Zapisz jako nowy wpis”** z karty egzemplarza do własnej paczki MG; egzemplarz wskazuje potem
  nowy wpis — MG chce użyć podkręconego egzemplarza albo improwizowanej istoty w innej przygodzie.
- **„Zmień wpis źródłowy”** egzemplarza (goblin okazuje się hobgoblinem; nazwa, notatka, bieżące PZ
  i stany zostają) — pierwsza taka podmiana obchodzona przy stole usunięciem i dodaniem.
- **Edycja kilku zaznaczonych egzemplarzy naraz** — MG zmienia to samo pole po kolei na kilku.
- **Typ „Zdolność”** z kategorią (atut, manewr, inwokacja, metamagia, styl walki) — pierwsza
  zdolność spoza drzewek klas; do tego czasu wybory w klasie są węzłami.
- **Odnośniki do wiedzy** (klik w zaklęcie albo stan w prozie otwiera kartę; na biurku jako pływające
  okno), słownik „Hasło” i zakładka „Zasady” — MG zbyt często szuka opisu wspomnianego w prozie.
- **Formuły w danych paczki** (pola liczone, które dopisuje autor paczki) — po kamieniu milowym.
- **Edytor treści w aplikacji** (wpisy, przygoda, lore) — pisanie plików poza aplikacją zaczyna
  przeszkadzać w przygotowaniu sesji.
- **Odległości między lokacjami** (propozycja czasu podróży) — wpisywanie czasu podróży zaczyna męczyć.
- **Dodatki (warianty zasad)** — pierwszy wariant, który autor chce mieć w konkretnej kampanii.
- **Dodanie paczki przeciągnięciem do okna** — kolizja nazwy daje odmowę z komunikatem, nigdy
  nadpisanie.
- **Wczytanie paczek od nowa** — przycisk w zakładce treści i F5; wybór zostaje, jeśli wpis o tym id
  nadal istnieje. Restart po każdej poprawce paczki zaczyna przeszkadzać przy przygotowaniu sesji.
- **Pomiar startu** — pierwsze zacięcie zauważone przez autora; wtedy najpierw liczby, potem poprawka.
