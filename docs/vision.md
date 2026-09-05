# Wizja

## Czym to jest

Desktopowy panel Mistrza Gry. Narzędzie do prowadzenia sesji przy stole —
nie stół wirtualny, nie gra dla graczy, nie arbiter narracji.

Czym świadomie **nie** jest:

- **Nie VTT.** Brak mapy taktycznej, brak figurek, brak trybu dla graczy.
- **Nie sieciowe.** Jedna maszyna, jeden użytkownik, żadnej synchronizacji.
- **Nie arbiter.** Silnik liczy i przedstawia wynik, decyzję podejmuje MG.

Kryterium sukcesu jest jedno: przy stole, w trakcie sesji, odczytanie stanu
i wprowadzenie zmiany ma być szybsze niż sięgnięcie po kartkę.

## Zasada nadrzędna

**Aplikacja pamięta i liczy. MG rozstrzyga.**

W cRPG komputer musi rozstrzygać, bo nie ma sędziego. Przy stole sędzia
siedzi obok, więc aplikacja nigdy nie musi zdecydować — musi pamiętać i
policzyć na żądanie. To zdejmuje z nas prawie całą specyfikę systemów gry,
bo systemowe jest głównie rozstrzyganie, a księgowość jest wspólna.

Granica: **automatyzujemy wszystko, co jest deterministyczne po decyzji MG,
i nigdy samą decyzję.** „MG mówi: długi odpoczynek" → aplikacja stosuje
odnowienia dla całej drużyny. „MG mówi: trafił za 7" → aplikacja odejmuje 7
od zdrowia. Aplikacja nigdy nie pyta, czy trafił.

To jest zarazem sprawdzian dla każdej przyszłej funkcji: *czy ona
rozstrzyga, czy pamięta?* Jeśli rozstrzyga — wypada z zakresu albo wymaga
jawnego zatwierdzenia przez MG.

## Kampania

**Kampanię definiuje jej stan, nie zestaw możliwości.**

Gdyby tożsamością kampanii była lista włączonych funkcji, dwie kampanie o
tym samym zestawie byłyby tym samym bytem, a kampania byłaby paczką
ustawień. Tożsamością jest treść: świat, drużyna, data, historia, to gdzie
sprawy stoją.

Stąd trzy decyzje:

- **Stan jako migawka, nie event sourcing.** Kampania trzyma bieżący stan,
  nie dziennik zdarzeń, z którego dałoby się go maszynowo odtworzyć.
- **Kampania to katalog, nie plik.** Rozerwany zapis daje się wtedy wykryć,
  zamiast po cichu uszkodzić całość. Kopia zapasowa to skopiowany katalog
  kampanii z zewnątrz — model save game, nie mechanizm wewnątrz formatu
  zapisu. To wyklucza wersjonowanie per plik, regenerację i historię
  pokoleń wewnątrz kampanii: manifest i pliki bloków danych noszą wspólny numer
  pokolenia, a rozjazd między nimi znaczy zapis przerwany w połowie, nic
  więcej — to wykrywanie uszkodzenia, nie historia. Zapis jest atomowy.
- **Zakładanie kampanii pyta o tożsamość** — nazwa, system, data startowa —
  nie o konfigurację. Zaczynasz od treści, nie od ustawień.

## Narzędzia

**Wszystkie narzędzia są wbudowane i zawsze obecne.** Nie ma wtyczek, nie ma
ładowania z dysku, nie ma wyboru zestawu per kampania. Zestaw narzędzi jest
własnością wersji aplikacji, nie kampanii.

**Narzędziem jest logika produkująca następstwa.** Test: czy z jednego
wejścia wynikają konsekwencje, których nikt nie wpisał? Przesunięcie czasu
sypie kaskadą — dojrzewa wydarzenie, wygasa efekt, kurczą się racje. To jest
narzędzie. Rejestr, w którym wpisana wartość po prostu leży, nie jest
narzędziem — to notatki.

Dodatkowo logika musi zdejmować z MG robotę **powtarzalną i mechaniczną**.
Automatyzacja, która nie oszczędza powtarzalnego wysiłku, jest ozdobą na
notatkach.

Konsekwencja: zestaw zostaje mały sam z siebie, bo logika jest droga do
napisania i utrzymywana na zawsze — także wtedy, gdy nikt jej nie otwiera od
dwóch lat. Prawdziwym ogranicznikiem jest koszt utrzymania, nie pojemność
zasobnika; czytelność zasobnika jest sprawą interfejsu i rozwiązuje się
grupowaniem i wyszukiwaniem, nie odmawianiem narzędzi.

Nisze — ekonomia świata, frakcje, pogoda — nie są narzędziami, dopóki nic
się w nich nie dzieje samo; są treścią w notatkach.

## Biurko

Obszar roboczy to **same okna**: równorzędne, do zmiany rozmiaru, do
schowania w zasobniku i przywrócenia.

- **Zasobnik trzyma panele, nie narzędzia.** Co jest pod spodem — logika czy
  treść — jest dla zasobnika niewidoczne. MG nigdy nie widzi słowa
  „narzędzie"; widzi okna: Drużyna, Czas świata, Notatki.
- **Panele i narzędzia nie stoją w relacji jeden do jednego.** Jedno
  narzędzie może dać dwa okna; jedno okno może czerpać z dwóch źródeł.
  Panel może też nie mieć narzędzia pod spodem — notatki są takim
  przypadkiem: wpisana treść po prostu leży, nic z niej nie wynika samo.
- **Logika nigdy nie mieszka w panelu.** Panel przedstawia i przyjmuje
  polecenia, nie wylicza następstw. Narzędziem jest to, co zostaje po
  zabraniu okna — i co da się przetestować bez okna.

Reguła nie ma wyjątków: **wszystko, co leży na blacie, korzysta z modelu
okna** — lista postaci tak samo jak notatki czy zegar. Poza biurkiem żyją
tylko widoki powłoki, których blat nie dotyczy: biblioteka kampanii i
ustawienia. Jak prezentować pełną kartę pojedynczej postaci, rozstrzygniemy,
gdy będzie powstawać — ale wejściem do niej jest okno listy, nie osobny
byt obok biurka.

### Rama i zasobnik

Podział jest po zachowaniu, nie po wyglądzie: **co da się zminimalizować,
przesunąć i przeskalować, jest oknem; co stoi nieruchomo, jest ramą.**

Ramie należy się to, czego okno nie może albo nie powinno obsłużyć:
zarządzanie samymi oknami (zasobnik nie może być oknem, bo musiałby
zarządzać sobą), przechodzenie między kontekstami aplikacji, stan aplikacji,
wejście do ustawień. Wszystko, co dotyczy treści kampanii, jest oknem —
dlatego zegar nie ma odpowiednika w ramie, choć bywa najczęściej sprawdzany.

Jedno dopowiedzenie, żeby regułę dało się czytać dosłownie: **rama niesie
tożsamość, nie stan.** Nazwa kampanii w nagłówku odpowiada na pytanie „gdzie
jestem", nie „jak stoją sprawy w świecie". Rama mówi, gdzie jesteś i co robi
aplikacja; okna mówią, co dzieje się w świecie.

Zasobnik jest wspólny dla wszystkich kampanii jako pojemnik — zmienne jest
tylko jego wypełnienie. Leżą w nim **dwie różne rzeczy**:

- **kwadraciki okien** — zminimalizowanych i dostępnych; kliknięcie kładzie
  okno na blacie;
- **miniature panele** — osobny byt o podobnej zasadzie działania, siedzący
  w zarezerwowanym slocie.

Miniatura **nie jest trzecim stanem okna** („otwarte / zminimalizowane / w
slocie"), tylko **obecnością równoległą**: mini-zegar może siedzieć w
zasobniku, podczas gdy pełne okno zegara leży otwarte na blacie. Dlatego
kliknięcie w miniaturę nie przywraca okna — miniatura robi swoje, okno
otwiera się kwadracikiem.

Miniatura jest opcjonalną własnością panelu, nie nowym rodzajem bytu: panel
albo deklaruje postać zwartą, albo nie da się go wstawić do slotu. Postać
zwarta i pełna to **dwa widoki nad tym samym odczytem, nie dwie
implementacje**, a przyciski miniatury wysyłają **te same nazwane
polecenia** co przyciski okna. Dwie ścieżki do jednej akcji zawsze się w
końcu rozjeżdżają.

### Przepływ: okno, narzędzie, dane

To nie jest trójkąt, tylko pętla o jednokierunkowych krawędziach:

1. **Dane → okno.** Okno czyta dane bezpośrednio, nie przez narzędzie. Gdyby
   pytało narzędzie o wartości, narzędzie stałoby się warstwą dostępu do
   danych i wróciłaby własność treści przez narzędzia.
2. **Okno → narzędzie.** Kliknięcie niczego nie zmienia samo; okno zgłasza
   nazwany zamiar („przesuń o trzy dni").
3. **Narzędzie → dane.** Narzędzie oddaje przekształcenie, a rdzeń stosuje
   je do bieżącej wartości bloku danych. To jedyne miejsce, w którym powstaje
   nowy stan.
4. **Dane → okno.** Blok danych się zmienił, okno odczytuje go ponownie. Bez
   odpytywania w pętli i bez przesyłania wartości w zdarzeniu.

Narzędzie nigdy nie woła okna.

Wyjątek od „okno czyta dane wprost": **odczyt wyliczony** — sformatowana data
w kalendarzu świata, „ile dni do najbliższego wydarzenia", suma obciążenia
drużyny — jest obliczeniem, więc należy do narzędzia. Narzędzie wystawia go
jako funkcję nad danymi, nie jako własny stan.

## Treść kampanii a stan narzędzia

To są **dwie różne rzeczy**, dziś zlepione w kodzie.

Rozstrzyga długość życia: **czy ta treść ma sens, gdy narzędzia nie ma?**
Data w świecie, skład drużyny, zaplanowane wydarzenia, notatki — mają.
Kolejność inicjatywy — nie ma, umiera z walką. Rzut kością nie ma nic
własnego.

Z tego wynika, że narzędzia posiadają **prawie nic** i zbiegają do
bezstanowych usług nad wspólną treścią. Czas świata należy do świata, nie do
zegara; zegar wie tylko, co znaczy „przesuń o trzy dni" i co się wtedy sypie.

**Kierunek zapisu:** kampania na dysku to tożsamość plus `data` — nazwane,
wersjonowane bloki danych, których rdzeń nie interpretuje. Nie `heroes`,
`npc`, `locations` jako pola formatu, bo wtedy rdzeń poznaje tożsamość rzeczy
w świecie i przy trzecim systemie okaże się, że zna ją źle. Blok danych
powstaje przy pierwszym zapisie, więc nieużywane narzędzie kosztuje zero.
Deklaracja kształtu nie jest własnością danych: blok danych może czytać i
zmieniać każde narzędzie, które go zadeklarowało. Kształty treści systemowej
należą do definicji systemu, nie do narzędzia.

Definicja i treść kampanii to dwa różne światy danych. Definicja — czym
jest dana rzecz, jakie ma pola — leży poza treścią kampanii, jest wspólna
dla systemu i kampania jej nie zmienia. Treść kampanii to bloki danych:
zmienne, własne dla kampanii, w jej katalogu. Kampania trzyma referencję do
definicji plus dane instancji — zapis w rodzaju „core:iron_sword, sztuk: 1"
— nigdy kopię definicji. Gdy referencja się nie rozwiązuje (brak definicji
przy odczycie), dane zostają nienaruszone: fakt jest zgłaszany jako
nieodnaleziony, nic nie jest usuwane ani „naprawiane" — kampania otwiera
się normalnie, a po przywróceniu definicji wszystko wraca samo. Zapis nie
sprawdza, czy cel referencji istnieje — sprawdza tylko, że pole ma postać
referencji; inaczej kampania zapisana przy komplecie definicji stałaby się
niezapisywalna po ich zmianie.

Bloki danych są **rejestrowane, nie posiadane**. Rejestr żyje w rdzeniu i
trzyma nazwę bloku danych, jego bieżącą wersję, jego **kształt** i ścieżki
migracji — nic więcej. Kształt opisuje budowę: z jakich nazwanych pól i
jakich typów blok danych się składa. Język kształtu startuje na minimum —
zestaw nazwanych pól o typach
prostych — a zagnieżdżanie, warianty i referencje dochodzą,
gdy pojawi się treść, która ich wymaga. Rdzeń przy zapisie sprawdza
zgodność wyniku z zadeklarowanym kształtem, ale kształt nigdy nie niesie
reguły między wartościami: „to pole jest referencją" — tak, „ta wartość
musi być większa od tamtej" — nie; reguła między wartościami to robota
narzędzia albo robota MG. Bez tej granicy język kształtu stałby się
interpreterem, przed którym ten dokument już ostrzega w części o systemie
gry. Znajomość budowy nie jest znajomością znaczenia: rdzeń wie, z jakich pól
i typów blok danych się składa, nie wie, czym te rzeczy są w świecie gry — ta granica zostaje w mocy. Narzędzie deklaruje wyłącznie,
których bloków danych używa — które czyta, które zmienia. Granica jest
postawiona świadomie: nazwa bloku danych w rejestrze to wpis w tablicy, nie
pole w schemacie formatu zapisu. Rejestr jest następcą dzisiejszego katalogu modułów, nie
drugim rejestrem obok niego, i dziedziczy jego własność — wpis wprost w
pliku, zero magii ładowania, zmiana widoczna w diffie.

**Zapis to przekształcenie, nie gotowa treść.** Narzędzie nie oddaje nowej
wartości bloku danych — oddaje przekształcenie, które rdzeń stosuje do
wartości bieżącej. Powód: narzędzia budzone zdarzeniami pracują kaskadowo,
więc treść wyliczona na kopii sprzed cudzego zapisu po cichu skasowałaby
tamten zapis. Przekształcenie daje ten sam wynik niezależnie od tego, co
zaszło między odczytem a zapisem. Rdzeń przekształcenia nie interpretuje —
wykonuje je, sprawdza zgodność wyniku z zadeklarowanym kształtem bloku
danych i ogłasza na magistrali, że blok danych się zmienił. Zapis nie niesie
nazwy ani powodu: istnieje po to, żeby wiadomo było, że blok danych się
zmienił, nie dlaczego. Ogłoszenie niesie samą nazwę bloku danych, nigdy
nowej wartości — okno po zmianie odczytuje blok danych ponownie i to domyka
pętlę okno→narzędzie→dane→okno. Jedno wejście zapisu na blok danych, brak
drugiej drogi — i to jest jedyne egzekwowanie tej reguły, trzyma się na
kształcie
API, nie na teście. Z tego wynika wprost, że „cofnij" jest poza
zakresem produktu — nie jako odłożone na później, tylko jako wykluczone
przez format zapisu: stan jest migawką, a zapis nie niesie historii, z
której dałoby się maszynowo odtworzyć poprzedni stan.

**Brak bloku danych znaczy „jeszcze nic tu nie ma", nie „zapis rozerwany".**
Narzędzie dodane w czerwcu musi działać na kampanii założonej w marcu. Dziś
kod robi odwrotnie i traktuje brak wpisu jako rozerwany zapis — to do
naprawienia razem z przebudową, nie wcześniej.

**Kiedy to budujemy:** najpierw. Fundament — rejestr bloków danych plus jedyne
wejście zapisu — powstaje przed pierwszym prawdziwym narzędziem, nie jako
reakcja na ból przy drugim. Nie ma bowiem na czym tego bólu poczekać:
prototypowe narzędzia, które miały go pokazać, zostały usunięte.

## Zdarzenia

Dane odpowiadają na pytanie **jak jest**. Zdarzenia — **co się właśnie
stało**. To nie jest ta sama informacja i wspólna treść nie zastępuje
magistrali: z odczytu daty nie wynika, że data się przesunęła ani że
przekroczyła próg, na który ktoś czekał.

- **Zdarzenie niesie fakt dokonany, nie stan.** Wartość jest w danych;
  odbiorca po szczegóły idzie do danych.
- **Nie publikujemy zdarzeń bez słuchacza.**
- Publikujący nie wie, kto słucha — i to jest różnica między magistralą a
  zależnością między konkretnymi narzędziami.
- **Narzędzie nie zależy od innego narzędzia.** Nie ma deklaracji zależności
  między narzędziami, nie ma porządku aktywacji, nie ma typowanego sięgania
  po inne narzędzie. Zostają dwa kanały pośrednie: wspólne dane odpowiadają
  na pytanie „jak jest", magistrala na pytanie „co się stało". Brak
  krawędzi narzędzie→narzędzie znosi problem cykli i kolejności aktywacji —
  nie ma czego sortować topologicznie — kosztem dociążenia magistrali: to
  ona staje się jedyną drogą, którą jedno narzędzie reaguje na działanie
  drugiego.

## System gry

**Systemowa jest treść, nie kod.** Żadnego silnika reguł i żadnego osobnego
projektu na ruleset. Struktura jest wspólna dla systemów TTRPG („zasób
nazwany, z maksimum i regułą odnawiania" opisuje punkty życia, poczytalność
i pęd równie dobrze), a różni się to, co odnawia i kiedy — czyli dane.

Dwa zabezpieczenia:

- **Definicja systemu jest deklaratywna i głupia — żadnych wyrażeń.** Jeśli
  reguła wymaga wyrażenia, to reguła dla MG. Inaczej za rok mamy interpreter
  zamiast narzędzia.
- **Pierwszy system wpisujemy konkretnie, ale w jednym miejscu.** Nie
  budujemy warstwy konfiguracji, zanim działa jeden system. Uogólnienie ma
  być refaktoryzacją, nie przepisaniem. Drugi system jest wyzwalaczem, nie
  założeniem.

To samo dotyczy notatek: mogą mieć nazwy i kategorie, nigdy zależności
między wartościami. Wartość reagująca na inną wartość awansuje do narzędzia
albo zostaje ręczną robotą MG.

## Odłożone świadomie

- **Tryb aktywnej sesji.** Sesja jako byt trwały, z początkiem i końcem.
- **System paczek treści.** Instalacja, źródło i dystrybucja paczek z
  definicjami — samą referencję do definicji treść kampanii już zakłada.
- **Pełna obsługa klawiatury.** Wymóg wynikający z użycia przy stole.

## Czego nie wskrzeszamy

Poprzednie podejście dzieliło kod na `domain` / `application` /
`infrastructure`. Przy jednym użytkowniku i jednym procesie te warstwy
generowały ceremonię bez zysku. Obecny podział jest odpowiedzią na tamten
koszt. Nie wracamy do tego, nawet gdy pojawi się pokusa „porządku".
