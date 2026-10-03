# Rozstrzygnięcia

Co przesądzone i czego nie robimy. Czytaj **przed** propozycją zmiany architektury. Pomysł z listy
odrzuconych wraca tylko wtedy, gdy jego wyzwalacz się spełnił albo powód przestał obowiązywać — wtedy
zmień wpis. Pełne argumenty sprzed przebudowy obiegu pracy: `docs/archive/decisions.md`.

## Obowiązuje

- **Cztery projekty, nie siedem.** Warstwa „bibliotek” (wpisy, biurko) miała po jednym konsumencie,
  a każda funkcja przechodziła przez 3–4 projekty. Zasady „rama nie zna D&D” pilnują referencje i skan
  słownictwa. *Wyzwalacz powrotu:* drugi system, który nie chce części wspólnego kodu.
- **Avalonia zostaje, motyw własny zostaje, nie wracamy do Fluenta.** Koszt interfejsu siedział
  w procesie (małe porcje, zatwierdzanie kroków, pikselowe rundy), nie w bibliotece; zmiana na HTML
  odtworzyłaby ten sam rachunek. Nowych kontrolek motywu nie buduje się na zapas.
- **Karta jest projektowana, nie składana z danych.** Układ karty należy do kodu systemu; dane niosą
  wyłącznie wartości.
- **Kształt jest kodem, wartości są danymi.** Literówka w nazwie pola to błąd kompilacji albo powód
  odrzucenia wpisu, nie cicho zignorowana wartość.
- **Nakładka instancji to rzadka łatka rozwiązywana przy odczycie**, nie kopia wpisu: zmiana wpisu
  działa jak patch balansujący grę i dociera do zapisanych kampanii.
- **Model subagentów: Opus 5.5 także do pracy mechanicznej.** Pomiar na porządkach komentarzy (trzy
  rozłączne paczki, worktree, ten sam opis zadania): Sonnet 5.5 — 80 kroków, 12,3 min, 14,4 mln
  odczytu, przerwany przed buildem, 6 poprawek po przeglądzie; Opus 5.5 — 66 i 64 kroki, ok. 9,5 min,
  12,7 i 11,0 mln odczytu, build i testy zielone, 3 i 0 poprawek. Opus wyszedł taniej w odczycie
  i bez dokańczania. Subagent biegnie zawsze w tle, żeby autor mógł w tym czasie pytać.
  Liczby: `tools/subagent-usage.py`. *Wyzwalacz:* nowy model albo pomiar z Sonnetem bez poprawek.
- **Zapis po każdej zmianie**, bez ręcznego „Zapisz”. *Wyzwalacz:* MG chce wrócić do wcześniejszego
  stanu → najpierw rotujące kopie zapasowe.
- **Błąd nie zamyka programu.** Przy stole zamknięcie kosztuje więcej niż błąd, a kampania jest
  zapisana po każdej zmianie. Wyjątek na wątku interfejsu (komenda, kliknięcie, timer, układ) łapie
  jeden handler: wpis w logu, jedno powiadomienie „Nie udało się wykonać tej czynności. Szczegóły
  w logu.”, program działa dalej. Błąd w tle poza interfejsem — tylko log. Akcja MG nie połyka błędu
  po cichu, tylko go przepuszcza. *Wyzwalacz:* błąd, po którym dalsza praca psuje zapisaną kampanię.
- **CommunityToolkit.Mvvm w modelach widoków**, nie ręczne `ObservableObject` i komendy. Próba na
  pasku górnym: build, testy i aplikacja bez zmian; wiązania są kompilowane, więc nazwę wygenerowaną
  sprawdza build, nie podpowiedzi edytora. Dawnego powodu odejścia od Toolkitu nie odtworzono.
  Właściwości częściowe (C# 14), nie pola z atrybutem. Setter z logiką poza powiadomieniem zostaje
  ręczny, bo `OnXChanged` biegnie przed powiadomieniem o X. *Wyzwalacz:* generator psuje build albo
  edytor.
- **Zakaz 2 według intencji: czas nie jest niczyim wejściem.** Litera „żaden typ nie niesie czasu”
  zabraniała zegara świata i rundy w inicjatywie, choć nic ich nie wykonuje. Narzędzie trzyma zegar,
  rundę i wskaźnik tury, przesuwa je akcja MG, czyta tylko jego widok. Podróż przesuwa zegar
  wyłącznie jako część jednej akcji MG z czasem, który MG widzi; samo przeniesienie drużyny zegara
  nie rusza (zakaz 5).
- **Katalog świata to porządek, nie warstwa scen.** Entity leżą w drzewie katalogów w stanie
  kampanii. Katalog ma tylko nazwę i zawartość — opis miejsca to strona lore. Nie ma cyklu życia
  i nigdy nie jest celem operacji na swojej zawartości („wszystkim w karczmie” — zakaz 4). Usuwa się
  tylko pusty, żeby jedno kliknięcie nie zabrało po cichu części świata.
- **Postać gracza leży obok kampanii.** Trzeci rodzaj danych obok treści paczek i stanu kampanii:
  tworzy ją MG w aplikacji, może grać w wielu kampaniach. Postać mówi, kim jest (cechy, klasa
  i węzły, zaklęcia, poziom, PZ maks.); jej entity w kampanii — co ma i gdzie jest (aktualne PZ,
  stany, miejsce, ekwipunek, złoto). Awans dociera do każdej kampanii jak poprawka wpisu; przedmioty
  zostają w świecie, w którym je zdobyto. Identyfikator `źródło:id` wskazuje wpis albo postać.
- **Klasa i węzeł drzewka są wpisami w paczce.** Kształt i wygląd drzewka jest kodem systemu; węzeł
  zna klasę, poziom i węzły poprzedzające. Węzeł ma więc identyfikator (ściągawka drukuje go jak
  zaklęcie), wartości zostają płaskie, a własną klasę dopisuje się bez programisty. Odblokowanie
  zapisuje tylko siebie — nie dodaje PZ, zaklęć ani następnych węzłów; niespełnione wymaganie widać,
  ale nie blokuje.
- **Wersje formatów nie rosną do pierwszej sesji przy stole.** Dopóki aplikacja nie ma prawdziwego
  użycia (koniec kamienia milowego „pierwsza sesja przy stole”), żaden typ treści ani zapis kampanii
  nie podnosi wersji formatu: niezgodną zmianę robi się w miejscu, a stare dane przepisuje albo usuwa
  ręcznie. Migracji nie ma; mechanizm porównania wersji zostaje. *Wyzwalacz:* koniec tego kamienia —
  od tej chwili niezgodna zmiana podnosi wersję i przychodzi razem z migracją.
- **Wartość jest we wpisie, cena na slocie.** Wpis przedmiotu niesie wartość: liczbę bez jednostki,
  z ułamkami, która mówi tylko, ile razy coś jest cenniejsze od czegoś innego. Cenę nakłada
  sprzedający — należy do slotu (sklep, plecak) razem z ilością, więc ten sam przedmiot kosztuje
  różnie w różnych miejscach bez reguły wybierającej cenę według miejsca (zakaz 1). Skala liczb to
  umowa autora paczki, nie aplikacji. Jednostki (kilogramy i funty, monety kampanii) to listy
  mnożników od jednej bazy.

## Odrzucone

- **Skrypty w paczkach (Lua, „poziom 3”)** — formuła deklaratywna bez gałęzi wystarcza i nie wymaga
  piaskownicy.
- **Osobny system efektów, kaskady zmian, cofanie jako wymóg silnika** — łamią zakazy 3–5; efekt jest
  wkładem do sumy, nie mechanizmem.
- **Zakładki, nawigacja albo kategorie z danych paczki** — pasek wypełnia skompilowany system.
- **Karta z listy elementów podanej w danych, generyczne prymitywy UI dla danych, jedna uniwersalna
  forma pośrednia** — to decyzje o układzie przebrane za dane.
- **Dziedziczenie i osadzanie szablonów, typy treści jako plik danych** — typ treści jest kodem.
- **Klasy postaci wkompilowane w kod systemu, całe drzewo klasy w jednym wpisie** — wpis nie jest
  kodem, a wartości są płaskie; patrz klasa i węzeł wyżej.
- **Zagnieżdżone wartości w polu wpisu** — wartości są płaskie; strukturę da slot. Wyjątkiem jest
  sekcja prozy karty (wstęp i nazwane wpisy): kształt ustala rekord typu, a służy tylko do czytania.
- **Wpisy lokalne dla kampanii, rejestr wewnątrz kampanii** — treść mieszka w paczkach; autorstwo
  idzie przez własną paczkę MG.
- **Materializacja wpisu w instancji, nakładka jako miejsce na warianty rzeczy** — patrz nakładka wyżej.
- **Odrzucanie całej paczki za jeden wadliwy wpis** — wadliwy wpis oznacza się, reszta się wczytuje.
- **Rozgałęzianie po rodzaju wpisu w Core/Desktop i introspekcja typu treści przez narzędzie** —
  jedynym miejscem, gdzie wolno wiedzieć, czym jest wpis, jest system.
- **Ładowanie systemów w czasie wykonania, systemy zależne od innych systemów** — statycznie,
  równolegle, bez zależności.
- **Dodatek jako przełącznik sprawdzany w logice** — wariant, który potrzebuje przełącznika w środku
  logiki, zwykle wykonuje regułę, którą powinien wykonać MG.
- **Warstwa scen** — kampania jest sesją i światem naraz.
- **Migracja formatu i pola zarezerwowane budowane z wyprzedzeniem** — manifest jest pobłażliwy, więc
  dołożenie pola nie wymaga migracji; pierwsza migracja powstanie z pierwszą niezgodną zmianą po
  pierwszej sesji przy stole (patrz wersje formatów wyżej).
- **Śledzenie tur jako element karty albo licznik rund w danych** — zakaz 2.
