# Rozstrzygnięcia

Co przesądzone i czego nie robimy. Czytaj **przed** propozycją zmiany architektury. Pomysł z listy
odrzuconych wraca tylko wtedy, gdy jego wyzwalacz się spełnił albo powód przestał obowiązywać — wtedy
zmień wpis. Argumenty, których tu nie streszczono: `docs/archive/decisions.md` — tylko przy powrocie
do odrzuconego kierunku. Terminy: słownik w `docs/architecture.md`.

## Obowiązuje

- **Cztery projekty: `Core`, `Desktop`, `Content.<system>`, `App`.** Osobne projekty dla wpisów
  i biurka miałyby po jednym konsumencie, a każda funkcja przechodziłaby przez 3–4 projekty. Zasadę
  „rama nie zna D&D” pilnują referencje i skan słownictwa. *Wyzwalacz powrotu:* drugi system, który
  nie chce części wspólnego kodu.
- **Avalonia zostaje, motyw własny zostaje, bez Fluenta.** Koszt interfejsu leży w procesie pracy
  (drobne zlecenia, zatwierdzanie każdego kroku, poprawki co do piksela), nie w bibliotece; przejście
  na HTML nie zmieniłoby tego rachunku. Nowych kontrolek motywu nie buduje się na zapas.
- **Karta jest projektowana, nie składana z danych.** Układ karty należy do kodu systemu; dane niosą
  wyłącznie wartości.
- **Kształt jest kodem, wartości są danymi.** Literówka w nazwie pola to błąd kompilacji albo powód
  odrzucenia wpisu, nie cicho zignorowana wartość.
- **Nakładka entity to rzadka łatka rozwiązywana przy odczycie**, nie kopia wpisu: zmiana wpisu
  działa jak patch balansujący grę i dociera do zapisanych kampanii.
- **Architekt z wykonawcami.** Krok agenta wysyła cały jego kontekst, więc koszt to liczba kroków
  razy rozmiar kontekstu. Architekt trzyma rozmowę i czyta tylko kod potrzebny do zlecenia; wykonawca
  zaczyna od świeżego kontekstu i dostaje zlecenie z adresami (pliki i miejsca w nich). Opis procesu:
  `CLAUDE.md`; pomiar: `tools/usage.py`. *Wyzwalacz:* wykonawcy regularnie dochodzą do limitu kroków
  albo wycinek potrzebuje więcej niż dwóch wykonawców.
- **Model wykonawców: Sonnet do zleceń z adresami, Opus do porządków i zadań otwartych.** Odczyt
  kontekstu (połowa kosztu) kosztuje w obu tyle samo, więc model zmienia koszt kroku o ok. 30 %,
  a liczba kroków i poprawek zmienia go kilkukrotnie. Na porządkach Opus potrzebuje mniej kroków
  i poprawek niż Sonnet; zlecenia z adresami Sonnet wykonuje bez poprawek. Agent `porzadki` ma więc
  Opusa, `wykonawca` — Sonneta. *Wyzwalacz:* wykonawca na Sonnecie wymaga poprawek dwa razy z rzędu.
- **Zapis po każdej zmianie**, bez ręcznego „Zapisz”. *Wyzwalacz:* MG chce wrócić do wcześniejszego
  stanu → najpierw rotujące kopie zapasowe.
- **Błąd nie zamyka programu.** Przy stole zamknięcie kosztuje więcej niż błąd, a kampania jest
  zapisana po każdej zmianie. Błąd akcji MG kończy się powiadomieniem „Nie udało się wykonać tej
  czynności. Szczegóły w logu.” i program działa dalej; akcja go nie połyka po cichu. Mechanizm:
  `docs/architecture.md`, „Błędy”. *Wyzwalacz:* błąd, po którym dalsza praca psuje zapisaną kampanię.
- **CommunityToolkit.Mvvm w modelach widoków**, nie ręczne `ObservableObject` i komendy. Wiązania są
  kompilowane, więc nazwę wygenerowaną sprawdza build, nie podpowiedzi edytora. Właściwości częściowe
  (C# 14), nie pola z atrybutem. Setter z logiką poza powiadomieniem zostaje ręczny, bo `OnXChanged`
  biegnie przed powiadomieniem o X. *Wyzwalacz:* generator psuje build albo edytor.
- **Księga, nie zasady.** Aplikacja śledzi i przelicza wartości, przenosi rzeczy i wykonuje przełomy;
  ataku, rzutu obronnego ani obrażeń nie rozstrzyga. MG liczy tak jak gracze, którzy zostają przy
  papierze. Rozstrzyganie na wskazanym celu wymagałoby struktury ataków, obrażeń i odporności w każdym
  wpisie, a każde pole ze strukturą to miejsce, gdzie MG utyka przy zmianie świata (drugi atak bossa,
  płonący miecz); zysk — połowy przy wielu celach, ST koncentracji — jest mały. Granica w `CLAUDE.md`.
- **Lista przełomów (zamknięta).** Nowy przełom tylko za zgodą autora.
  - Następna tura: koniec tury istoty (jej liczniki stanów −1, stany na zerze zdjęte, tyknięcia na
    koniec tury) i początek tury następnej (tyknięcia na początek).
  - Odpoczynek krótki i długi: odnowienie PW, miejsc na zaklęcia i ładunków według podręcznika.
  - Handel: cena z wartości (sprzedaż za połowę), którą MG poprawia, potem przeniesienie przedmiotu
    i złota.
  - Podróż: drużyna do innego katalogu świata i zegar o czas wpisany przez MG.
- **Przełom działa od razu, okienko pozwala go wyłączyć.** Okienko przełomu obok narzędzia wymienia
  każdą zmianę; odznaczenie pozycji ją przywraca, „Cofnij wszystko” — całość. Okienko znika przy
  następnej akcji w tym narzędziu albo po zamknięciu. Odroczenie zmian do pierwszej interakcji
  odrzucone: karta pokazywałaby stan, który już wygasa, a moment zapisu byłby niejasny.
- **Liczniki stanów tykają na końcu tury istoty, która nosi stan.** Ogłuszenie na jedną rundę
  zabiera wtedy jedną turę, a rzut „kończy stan na końcu tury” wypada w tej samej chwili. Podręcznik
  liczy czas od tury źródła; różnica to najwyżej ułamek rundy, a licznik MG poprawia w miejscu.
  Nazwy jak w polskim podręczniku: runda to obieg wszystkich, tura — działanie jednej istoty.
- **Stany i efekty — jedna lista na istocie.** Pozycja to stan z paczki (ikona, nazwa, skrót,
  odnośnik) albo stan własny MG (nazwa, ikona domyślna), z opcjonalnym licznikiem rund, notatką MG
  („KON ST 13 kończy”) i tyknięciem na początek lub koniec tury („Płonie: 1k6 ognia, 3 rundy”,
  „Regeneracja 10, chyba że ogień”). Stany domyślne wpisu entity dziedziczy, dopóki MG ich nie zmieni.
  Tyknięcie to jedyne miejsce z wyzwalaczem i czasem — dopuszczalne, bo pisze je MG, nie wpis,
  momenty są dwa, a każde przechodzi przez okienko przełomu.
- **Pole zmiany liczby.** Każda śledzona wartość przyjmuje `-12`, `+5` i `=30`; liczba bez znaku nie
  przechodzi, bo „12” wpisane jako obrażenia ustawiłoby PW na 12. Przy wartości zostaje ostatnia
  zmiana („30, było 42, −12”) do następnej zmiany tej wartości. Rachunek z podręcznika (PW
  tymczasowe schodzą pierwsze) należy do systemu, pole — do ramy.
- **Znaczniki zmian w turze.** „Następna tura” zapamiętuje stan sprzed przełomu; wszystko, co
  zmieniło się od tej chwili — ręcznie albo przełomem — ma tło znacznika, rzecz zdjęta jest
  wyszarzona do końca tury. Kolor ma własny token („zmieniło się w tej turze”), nie zielony (sukces)
  ani czerwony (obrażenia). Zapamiętany stan zapisuje się ze stanem walki. Poza walką tur nie ma.
- **Kalkulator kości zamiast rzutu z karty.** Okno ramy, niezależne od systemu: wyrażenie („k20+4”,
  „2k20 wyższy + 4”, „4× k20+2”), wynik z rozpisaniem, kilka ostatnich rzutów na czas sesji. Premię
  MG przepisuje z karty — przycisk rzutu na karcie wymagałby struktury ataków.
- **Tryb edycji karty istoty.** Jeden przełącznik zamienia wartości karty w pola (ramka, ciemniejsze
  tło) w tych samych miejscach i wymiarach. Bieżące PW mają pole zmiany zawsze. Wartość różna od
  wpisu ma znacznik i „Przywróć z wpisu”. Odrzucone: ołówek przy każdym polu (szum na karcie, dwa
  kliknięcia na zmianę) i wszystko edytowalne od razu (formularz, przypadkowe zmiany przy stole).
- **Entity bez wpisu.** Improwizowany karczmarz to imię, notatka i — jeśli trzeba — PW i KP wpisane
  ręcznie. To stan kampanii, nie treść, więc nie jest wpisem lokalnym kampanii.
- **Konsument nadaje polu strukturę.** Trzy poziomy: proza (czyta MG), pole nazwane z tekstem
  (miejsce na zaprojektowanej karcie, wyszukiwanie: „Szybkość 9 m”, „Odporności: ogień”) i pole
  typowane — liczba, kość, wybór z listy — które czyta konsument. Pole wchodzi poziom wyżej
  z konsumentem z bieżącego kamienia milowego, nie „na kiedyś” — późny konsument wymusza przepisanie
  paczek. Ataki, obrażenia i odporności zostają prozą. Kształt ustala rekord C#, więc struktura nie
  otwiera drogi do skryptów w danych.
- **Entity składa się z aspektów.** Entity to tożsamość (id, nazwa, miejsce, notatka, wpis źródłowy)
  i zestaw aspektów (walka: PW, KP, cechy, inicjatywa, stany; przedmiot: waga, wartość; sakiewka;
  ładunki; miejsca na zaklęcia). Narzędzie pyta o aspekt, nie o typ. Aspekty są kodem systemu
  i powstają z konsumentem. Czy entity trzyma inne entity (plecak, skrzynia), rozstrzyga etap 9.
- **Typ ma stały kształt; aspekt to dane bez wyglądu.** Typ niesie zaprojektowaną kartę, profil
  zakładki, wymagany rdzeń i pola własne, których nie czyta żadne narzędzie (rozmiar, Szybkość,
  zmysły, wyzwanie, rzadkość, proza). Aspekt to tylko nazwa grupy pól, o którą pyta narzędzie (walka
  czyta KP i PW stworzenia i postaci); MG go nie włącza ani nie zdejmuje. Gdzie stoi pole, decyduje
  karta. KP zbroi to pole przedmiotu.
- **Wpis stworzenia ma zawsze walkę.** NPC bez walki to statblok pospolitego człowieka. Puste KP i PW
  zna tylko entity bez wpisu, a jej blok wartości pokazuje „—” zamiast znikać, żeby nic nie skakało.
- **Entity nie zmienia typu.** Przemiana to pozycja „Stanów i efektów” (mimik „w przebraniu:
  skrzynia”) albo druga entity obok (figurka cudownej mocy ożywa w stworzenie, a sama zostaje
  przedmiotem). Podmiana statystyk (postać zwierzęcia druida, polimorfia) czeka na pełną postać.
- **Drzewo świata nie pyta o typ.** Każda entity może leżeć w każdym katalogu; jeśli etap 9 dopuści
  entity w entity, też bez reguły typu (chowaniec w kieszeni, chochlik w butelce). Sumy (waga plecaka,
  handel) czytają tylko pola przedmiotu, więc stworzenie w plecaku nic nie waży — ocenę zostawia się MG.
- **Zakładki według typu, nie aspektu.** Jeden typ to jedna zakładka, w kolejności podręcznika
  (Stworzenia, Przedmioty, Stany, Postacie, Pochodzenia, Klasy, Zaklęcia); MG szuka mimika wśród
  stworzeń, nie wśród „rzeczy, które walczą”. Aspekt bywa filtrem w zakładce („z walką”,
  „z ładunkami”). Postacie są w kategorii System, bo grają w wielu kampaniach. Świat kampanii nie jest
  zakładką treści: entity leżą w katalogu świata na biurku.
- **Typy z entity i wiedza.** Entity mają stworzenie, przedmiot i postać gracza. Wiedza ich nie ma
  i wskazuje się ją identyfikatorem: zaklęcia, zdolności, stany, hasła, lore. Zaklęcie nigdy nie leży
  w katalogu świata — postać je zna, zwój je wskazuje.
- **Wpis wskazuje inny wpis na dwa sposoby.** Wzmianka w prozie (zaklęcia w statbloku, „cel staje
  się powalony”, zwój w opisie) to własny tekst ze znacznikiem `[[id|tekst]]`; karta niczego z wpisu
  wskazanego nie pobiera, a klik otwiera jego kartę (odnośniki: roadmapa, „Odłożone”). Pole ze
  strukturą (lista „Stany i efekty”, zaklęcia znane postaci) składa wiersz z pól wpisu wskazanego, bo
  lista jest ich konsumentem. Treści wiedzy nie wkleja się w cudzą kartę: liczby czaru należą do
  rzucającego (ST, poziom komórki), a karta ma się ułożyć bez paczki wiedzy.
- **Jeden typ „Stworzenie”** dla potworów, przeciwników i NPC, zakładka „Stworzenia” — termin zasad
  z polskiego podręcznika. „Nieumarły” to pole do filtrowania, nie gałąź.
- **Katalog świata to porządek, nie warstwa scen.** Entity leżą w drzewie katalogów w stanie
  kampanii. Katalog ma tylko nazwę i zawartość — opis miejsca to strona lore. Katalog nigdy nie jest
  celem operacji na swojej zawartości („wszystkim w karczmie” — cele wskazuje MG). Usuwa się tylko
  pusty, żeby jedno kliknięcie nie zabrało po cichu części świata.
- **Postać gracza leży obok kampanii.** Trzeci rodzaj danych obok treści paczek i stanu kampanii:
  tworzy ją MG w aplikacji, może grać w wielu kampaniach. Postać mówi, kim jest (cechy, klasa
  i węzły, zaklęcia, poziom, PW maks.); jej entity w kampanii — co ma i gdzie jest (aktualne PW,
  stany, miejsce, ekwipunek, złoto). Awans dociera do każdej kampanii jak poprawka wpisu; przedmioty
  zostają w świecie, w którym je zdobyto. Identyfikator `źródło:id` wskazuje wpis albo postać.
- **Aplikacja jest jedynym źródłem prawdy o postaci, gracz gra z papieru.** Kartka gracza to gotowy
  szablon wypełniany ręcznie; aplikacja jej nie drukuje (zmienia się za często), drukuje tylko
  ściągawki opisów. Gracz przepisuje zmiany z tego, co mówi MG, a przy rozjeździe wygrywa aplikacja.
  Drobny rozjazd w trakcie sesji (zużyte miejsca, strzały) MG wpisuje, gdy się o nim dowie.
- **Postać ma pełną kartę jak potwór.** Wartości liczone (modyfikatory, biegłość z poziomu, rzuty
  obronne, umiejętności, pasywna Percepcja, ST zaklęć) mają rozpisanie, a MG je nadpisuje w trybie
  edycji ze znacznikiem.
- **Klasa i węzeł drzewka są wpisami w paczce.** Kształt i wygląd drzewka jest kodem systemu; węzeł
  zna klasę, poziom i węzły poprzedzające. Węzeł ma więc identyfikator (ściągawka drukuje go jak
  zaklęcie), wartości zostają płaskie, a własną klasę dopisuje się bez programisty. Odblokowanie
  zapisuje tylko siebie — nie dodaje PW, zaklęć ani następnych węzłów; niespełnione wymaganie widać,
  ale nie blokuje.
- **Wersje formatów nie rosną do pierwszej sesji przy stole.** Dopóki aplikacja nie ma prawdziwego
  użycia (koniec kamienia milowego „pierwsza sesja przy stole”), żaden typ treści ani zapis kampanii
  nie podnosi wersji formatu: niezgodną zmianę robi się w miejscu, a stare dane przepisuje albo usuwa
  ręcznie. Mechanizmu migracji nie ma, mechanizm porównania wersji jest. *Wyzwalacz:* koniec tego
  kamienia — od tej chwili niezgodna zmiana podnosi wersję i przychodzi razem z migracją.
- **Wartość przedmiotu jest we wpisie, cena na slocie.** Wartość to liczba bez jednostki, z ułamkami,
  która mówi tylko, ile razy coś jest cenniejsze od czegoś innego; skala liczb to umowa twórcy paczki.
  Cenę nakłada sprzedający — należy do slotu (sklep, plecak) razem z ilością, więc ten sam przedmiot
  kosztuje różnie w różnych miejscach bez reguły wybierającej cenę według miejsca.
- **Jednostki to listy mnożników od jednej bazy** (kilogramy i funty, monety kampanii). Waga i wartość
  są obowiązkowe w każdym przedmiocie; zero jest wartością i karta je pokazuje („0 kg”) — informacji
  nie chowa się dlatego, że wynosi zero.
- **Rzadkość według skali gier, nie D&D.** Pięć stopni z kolorami jak w grach komputerowych:
  Pospolity (jasnoszary), Niepospolity (zielony), Rzadki (niebieski), Epicki (fioletowy), Legendarny
  (pomarańczowy). Przy kilkuset przedmiotach jedna skala czytelna od razu jest ważniejsza niż wierność
  podręcznikowi. To, czy przedmiot jest magiczny (ważne mechanicznie), niesie osobny znacznik
  i pigułka „magiczny”, nie rzadkość.
- **Słownictwo z polskiego podręcznika.** Paczka SRD i etykiety kart mówią językiem polskiego
  wydania (punkty wytrzymałości — PW, niepodatność, pasywna Percepcja, test ataku, krąg zaklęcia),
  bo gracze czytają te same słowa na swoich kartkach. Wyjątek: skróty cech SIŁ/ZRĘ/KON/INT/MDR/CHA,
  czytelniejsze w tabeli. Ściągawka tłumacza: `tools/srd/slownik.md`.
- **Paczka SRD rośnie na żądanie.** Tłumaczy się stworzenia i przedmioty potrzebne w przygodzie,
  którą MG przygotowuje, nie cały SRD naraz. Spis do wyboru: `tools/srd/stworzenia.md`.

## Odrzucone

- **Procedury systemu i rzut z karty na wskazany cel** (atak przeciw KP, rzut obronny z połową,
  obrażenia z odpornością, koncentracja) — patrz „Księga, nie zasady”.
- **Zakaz wszelkiego upływu czasu w aplikacji** („nic nie wygasa, bo minął czas”) — licznik stanu to
  księgowanie przy stole; przełom zdejmuje stan na kliknięcie MG i pokazuje to w okienku.
- **Hierarchia dziedziczenia istot** (Entity → Żywa istota → Nieumarły → Zombie) — D&D przecina
  gałęzie (inteligentny miecz, przemiana druida, NPC wskrzeszony jako zombie), a zmiana gałęzi to nowy
  obiekt bez tożsamości; miesza też etykietę z budową. Patrz aspekty wyżej.
- **Dziennik świata** (wszystkie zmiany w jednym długim logu) — jego potrzeby pokrywają okienko
  przełomu, znaczniki w turze, ostatnia zmiana przy wartości i historia kalkulatora.
- **Blok wartości bez dopisku dosuwany do dna obrazka** — rząd dopisku zostaje zarezerwowany zawsze,
  choć samotny blok bez dopisku (Kolczuga: KP) zdaje się wisieć; dosunięcie przesuwałoby bloki przy
  przełączaniu wpisów, co razi bardziej niż pusty rząd.
- **Skrypty w paczkach (np. Lua)** — formuła deklaratywna bez gałęzi wystarcza i nie wymaga
  piaskownicy.
- **Logika per wpis, kaskady zmian, cofanie jako wymóg silnika** — działanie pierścienia czy cechy
  potwora to słownictwo i zostaje tekstem; skutek należy do akcji MG, nie jest reakcją na zmianę.
- **Zakładki, nawigacja albo kategorie z danych paczki** — pasek wypełnia skompilowany system.
- **Karta z listy elementów podanej w danych, generyczne prymitywy UI dla danych, jedna uniwersalna
  forma pośrednia** — to decyzje o układzie przebrane za dane.
- **Dziedziczenie i osadzanie szablonów, typy treści jako plik danych** — typ treści jest kodem.
- **Klasy postaci wkompilowane w kod systemu, całe drzewo klasy w jednym wpisie** — wpis nie jest
  kodem, a wartości są płaskie; patrz klasa i węzeł wyżej.
- **Wpisy lokalne dla kampanii, rejestr wewnątrz kampanii** — treść mieszka w paczkach; autorstwo
  idzie przez własną paczkę MG.
- **Materializacja wpisu w entity, nakładka jako miejsce na warianty rzeczy** — patrz nakładka wyżej.
- **Odrzucanie całej paczki za jeden wadliwy wpis** — wadliwy wpis oznacza się, reszta się wczytuje.
- **Rozgałęzianie po rodzaju wpisu w Core/Desktop i introspekcja typu treści przez narzędzie** —
  jedynym miejscem, gdzie wolno wiedzieć, czym jest wpis, jest system.
- **Ładowanie systemów w czasie wykonania, systemy zależne od innych systemów** — statycznie,
  równolegle, bez zależności.
- **Wariant zasad jako przełącznik sprawdzany w logice** — wariant, który potrzebuje przełącznika
  w środku logiki, zwykle wykonuje regułę, którą powinien wykonać MG.
- **Warstwa scen** (sceny z własnym stanem i operacjami na zawartości) — kampania jest sesją
  i światem naraz.
- **Migracja formatu i pola zarezerwowane budowane z wyprzedzeniem** — manifest jest pobłażliwy, więc
  dołożenie pola nie wymaga migracji; pierwsza migracja powstanie z pierwszą niezgodną zmianą po
  pierwszej sesji przy stole (patrz wersje formatów wyżej).
- **Śledzenie tur jako element karty** — tury, rundy i liczniki efektów trzyma narzędzie walki;
  karta pokazuje wpis albo entity.
- **Architekt bez czytania kodu, każda runda poprawek u nowego wykonawcy** — zlecenie bez adresów
  zmusza wykonawcę do szukania, a każdy nowy wykonawca to ok. 150 tys. tokenów na sam start.
- **Cała praca w głównej sesji** — kontekst rośnie bez końca, a autor traci rozmowę w trakcie pracy.
- **Raporty z każdej fazy pracy, lista zadań w repozytorium, dokumenty prowadzone jak dziennik** —
  tokeny bez czytelnika; stan niesie git.
