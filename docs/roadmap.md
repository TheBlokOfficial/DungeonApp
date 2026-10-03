# Co dalej

Kolejność od góry. Pozycja jest opisem potrzeby, nie zleceniem — przed pracą sprawdź w kodzie, czy
nadal jest aktualna. Pozycję zrobioną usuwa się w commicie, który ją zamyka.

## Kamień milowy: pierwsza sesja przy stole

Cel: poprowadzić prawdziwą sesję bez podręcznika i bez papieru. Każdy brak z tej listy oznacza grę
hybrydową, więc kamień obejmuje całość. Etapy w kolejności pracy; każdy jest użyteczny sam. Formuły
nie są potrzebne — KP czy ST czarów MG wpisuje. Rozstrzygnięcia, na których stoi plan, są
w `docs/decisions.md` (katalog świata, postać gracza, klasy, zakaz 2).

1. **Treść.** Gotowe: lista z wyszukiwaniem, filtrami i sortowaniem, szczegół wpisu, pole obrazka
   z ramką. Projekt kart jest punktem wyjścia, nie specyfikacją:
   `docs/archive/zadania/zakladki-tresci.md`, sekcja *C. Projekt szkieletu i kart*.
   - **Karta potwora czeka na przegląd autora w aplikacji:** portret obok nazwy, KP, PZ i Szybkość
     pod tagami, dwie tabele cech, stałe sekcje prozy z nazwanymi wpisami.
   - **Wczytanie paczek od nowa** bez restartu; wybór zostaje, jeśli wpis o tym id nadal istnieje.
   - **Zaklęcia.** Czas rzucania, zasięg i czas trwania wyłącznie jako napisy; filtry i sortowanie
     po poziomie i szkole; koncentracja i rytuał jako tagi.
   - **Stany** (Powalony, Ogłuszony…) jako typ wpisu — przy stole i na ściągawce. Wpis stanu ma
     skrót (jedno–dwa zdania) i ikonę z paczki, bo karta pokazuje stan wierszem: ikona, nazwa,
     skrót. Wpis potwora może mieć stany domyślne (np. stała niewidzialność).
   - **Paczka `dnd5e-srd`: SRD 5.1 po polsku do 5. poziomu postaci** (CC-BY 4.0): zaklęcia 0–3,
     cechy klas i podklas do 5. poziomu, potwory do wyzwania 5, ekwipunek podstawowy, przedmioty
     magiczne pospolite do rzadkich, stany. Tłumaczenie danych to praca dla agenta w tle, typ po
     typie, gdy rekord i karta typu są gotowe. Odległości w metrach (1,5 m za 5 stóp). Dziś paczka
     ma pięć potworów i osiem przedmiotów przykładowych. Rzadkość D&D na skalę aplikacji:
     niemagiczne i pospolite → Pospolity, niezwykłe → Niepospolity, rzadkie → Rzadki, bardzo
     rzadkie → Epicki, legendarne i artefakty → Legendarny. Przed wyceną przedmiotów ustalić skalę
     wartości: małe, czytelne liczby z miejscem w dół i w górę (dziś przykłady mają cenę SRD
     w sztukach złota).
2. **Katalog świata** — okno biurka zamiast testowego „Świata kampanii”. Drzewo w zapisie kampanii:
   jeden katalog główny, podkatalogi, entity (dziś instancja) w katalogach. Katalog to nazwa
   i zawartość, bez opisu. Dodawanie z wyszukiwaniem, kilka sztuk naraz z numeracją („Goblin 1–4”).
   Nazwa entity (dziś operacja zmiany nazwy jest, pola w interfejsie nie ma) i notatka MG.
   Przenoszenie entity i katalogów; usuwa się tylko pusty katalog. Klik w entity otwiera okno jego
   karty — kilka naraz — z szybkimi PZ: „-7” albo „+5” i Enter zapisuje.
   Karta jest jedna dla wpisu i entity; różnią się tylko danymi. Sekcje wartości bieżących (gruby,
   kwadratowy pasek PZ od 0 do maksimum z polem szybkiej zmiany) i stanów (pod tabelą cech, za
   separatorem z nagłówkiem) pokazują się, gdy są wypełnione. Breadcrumb, portret, nazwa i tabele
   nie zmieniają miejsca. Entity dziedziczy stany wpisu, dopóki MG ich nie zmieni; 0 PZ niczego
   samo nie dodaje (zakaz 5).
3. **Kopie zapasowe kampanii**, rotujące. Przy stole zapis biegnie na żywo po każdej zmianie.
4. **Inicjatywa** — okno: entity i postacie z liczbą wpisaną przez MG, ułożone według niej; runda
   i wskaźnik tury przesuwane przez MG.
5. **Postacie graczy** — leżą obok kampanii, w katalogu systemu, i mogą grać w wielu kampaniach. MG
   tworzy je i zmienia formularzem w aplikacji. Wejście postaci do kampanii tworzy jej entity.
   Identyfikator `źródło:id` wskazuje odtąd wpis w paczce albo postać.
6. **Klasy i drzewka** — klasa i każdy węzeł to wpisy w paczce, drzewko to widok. MG zaznacza węzeł
   i odblokowuje go postaci; niespełnione wymaganie widać, ale nie blokuje.
7. **Ekwipunek, sakiewka, handel** — entity w entity (plecak postaci jako gałąź drzewa świata), złoto
   na entity. Cena i ilość leżą na slocie (sklep, plecak), nie we wpisie. Okno handlu: MG wskazuje
   sprzedającego, kupującego i sakiewkę, aplikacja proponuje cenę z wartości wpisu przez kurs waluty
   kampanii, MG ją poprawia, jedno kliknięcie przenosi przedmiot i złoto. Waluta kampanii to lista
   nominałów z mnożnikami (uniwersalna moneta albo własne monety); funty to samo dla wagi. Brak złota
   widać, nie blokuje.
8. **Czas świata i podróże** — okno zegara przesuwanego przez MG (szybkie przyciski i dowolna
   wartość). Podróż to jedna akcja: drużyna do innego katalogu, zegar o czas wpisany przez MG.
9. **Fabuła i lore** — zakładka dokumentów. Markdown z paczki, pisany poza aplikacją (np.
   w Obsidianie). Linki między stronami i do identyfikatorów; klik w identyfikator pokazuje kartę.
   Odhaczenia decyzji w fabule zapisują się w kampanii, więc jedna przygoda może iść dla kilku grup.
10. **Ściągawki** — wybór wpisów (cechy klas, zaklęcia, przedmioty, stany) i wydruk A4 przez PDF.
    Każdy typ ma własny układ na białą kartkę. Zaznaczenia może podpowiedzieć postać (jej węzły,
    zaklęcia, ekwipunek); MG poprawia je przed drukiem.

## Porządki w kodzie

- **Komentarze po polsku** w ok. 40 plikach — przetłumaczyć na angielski (konwencja: kod i komentarze
  po angielsku). `tools/comment-hits.py` wskazuje też komentarze z historią.
- **Komentarze nieaktualne w treści:** `CampaignRowViewModel` (powód niedostępności, którego wiersz nie
  pokazuje), `AppShellView.axaml` (host rozgrzewki „także po wyborze systemu”), `PanelCatalog`
  (kolejność „panele powłoki, potem narzędzia”).
- Piąty zakaz nie ma strażnika w kodzie — pilnuje go przegląd.
- **Powody odrzucenia wpisu paczki** są surowym angielskim tekstem parsera („The JSON value could not
  be converted to …”) — potrzebne polskie zdanie, które nazywa pole.

## Drobne uwagi autora

- Suwak: uchwyt w spoczynku trochę za ciemny. Pomysł: pod myszą obwódka zamiast rozjaśnienia.
- Menu: skrót nie stoi w jednej linii ze strzałką podmenu, podmenu nachodzi na menu.

## Po stronie autora

- Przenieść stare kampanie do `Dokumenty\DungeonApp\dnd5e\campaigns\` (stare `Packs`/`Campaigns` nie są
  czytane) i dopisać potworom pole `group`.
- Przepisać albo usunąć potwory we własnych paczkach w starym formacie (proza jako zwykły napis) —
  pokazują się jako odrzucone wpisy.

## Odłożone, z wyzwalaczem

- **Formuły** — po kamieniu milowym „przy stole”.
- **Edytor treści w aplikacji** (wpisy, przygoda, lore) — pisanie plików poza aplikacją zaczyna
  przeszkadzać w przygotowaniu sesji.
- **Odległości między lokacjami** (propozycja czasu podróży) — wpisywanie czasu podróży zaczyna męczyć.
- **Dodatki (warianty zasad)** — pierwszy wariant, który autor chce mieć w konkretnej kampanii.
- **Dodanie paczki przeciągnięciem do okna** — kolizja nazwy daje odmowę z komunikatem, nigdy
  nadpisanie.
- **Pomiar startu** — pierwsze zacięcie zauważone przez autora; wtedy najpierw liczby, potem poprawka.
