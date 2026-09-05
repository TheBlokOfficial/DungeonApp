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
i zapisuje w kronice. Aplikacja nigdy nie pyta, czy trafił.

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

- **Stan jako migawka, nie event sourcing.** Kampania trzyma bieżący stan.
  Kronika jest zapisem dla człowieka, nie źródłem prawdy do odtworzenia.
- **Kampania to katalog, nie plik.** Rozerwany zapis daje się wtedy wykryć,
  zamiast po cichu uszkodzić całość.
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
  „narzędzie"; widzi okna: Drużyna, Czas świata, Kronika.
- **Panele i narzędzia nie stoją w relacji jeden do jednego.** Jedno
  narzędzie może dać dwa okna; jedno okno może czerpać z dwóch źródeł.
  Panel może też nie mieć narzędzia pod spodem — kronika taki jest.
- **Logika nigdy nie mieszka w panelu.** Panel przedstawia i przyjmuje
  polecenia, nie wylicza następstw. Narzędziem jest to, co zostaje po
  zabraniu okna — i co da się przetestować bez okna.

Reguła nie ma wyjątków: **wszystko, co leży na blacie, korzysta z modelu
okna** — lista postaci tak samo jak kronika czy zegar. Poza biurkiem żyją
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
3. **Narzędzie → dane.** Narzędzie wylicza następstwa i zapisuje je jako
   nazwane zmiany. To jedyne miejsce, w którym powstaje nowy stan.
4. **Dane → okno.** Sekcja się zmieniła, okno odczytuje ją ponownie. Bez
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
wersjonowane sekcje treści, których rdzeń nie interpretuje. Nie `heroes`,
`npc`, `locations` jako pola formatu, bo wtedy rdzeń poznaje tożsamość rzeczy
w świecie i przy trzecim systemie okaże się, że zna ją źle. Sekcja powstaje
przy pierwszym zapisie, więc nieużywane narzędzie kosztuje zero. Deklaracja
kształtu nie jest własnością danych: sekcję może czytać i zmieniać każde
narzędzie, które ją zadeklarowało. Kształty treści systemowej należą do
definicji systemu, nie do narzędzia.

**Zmiany są nazwane, nie przypisywane.** Narzędzie nie ustawia wartości po
cichu, tylko zgłasza „zwiększ zmęczenie o jeden, powód: dzień marszu" — a
warstwa treści stosuje to, podbija generację i zapisuje wpis w kronice. To
jedyny mechanizm dający obiecaną wyjaśnialną historię.

**Brak sekcji znaczy „jeszcze nic tu nie ma", nie „zapis rozerwany".**
Narzędzie dodane w czerwcu musi działać na kampanii założonej w marcu. Dziś
kod robi odwrotnie i traktuje brak wpisu jako rozerwany zapis — to do
naprawienia razem z przebudową, nie wcześniej.

**Kiedy to budujemy:** nie teraz. Dziś boli w jednym miejscu — przy drużynie
— a przebudowa persystencji na jednym przykładzie to projektowanie pod
wyobrażoną zmienność. **Wyzwalacz: drugie narzędzie, które musi czytać albo
zmieniać bohaterów** (zasoby, odpoczynek, inicjatywa z listą uczestników).
Do tego czasu nie utrwalamy głębiej własności treści przez narzędzie.

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
- **Konsola sesji.** Wolny wpis MG-a do kroniki — najtańsze domknięcie
  pierwszego wycinka.
- **Przedmioty i paczki treści.** Przedmiot jako referencja do definicji z
  paczki plus nadpisania instancji.
- **Pełna obsługa klawiatury.** Wymóg wynikający z użycia przy stole.

## Czego nie wskrzeszamy

Poprzednie podejście dzieliło kod na `domain` / `application` /
`infrastructure`. Przy jednym użytkowniku i jednym procesie te warstwy
generowały ceremonię bez zysku. Obecny podział jest odpowiedzią na tamten
koszt. Nie wracamy do tego, nawet gdy pojawi się pokusa „porządku".
