# DungeonApp — jak prowadzić tę pracę

**Status: umowa o współpracy, nie opis programu.** Pozostałe pięć dokumentów opisuje aplikację;
ten opisuje pracę nad nią. Dlatego nie konkuruje z nimi o fakty i nie ma miejsca w ich porządku
pierwszeństwa — przy rozbieżności o kod wygrywa kod, a ten dokument milczy.

Jest adresowany do asystenta (i do każdego subagenta, któremu ktoś powierzy zadanie w tym
repozytorium). Powstał 2026-09-12 z notatek, które asystent trzymał dotąd w swojej prywatnej
pamięci — miejscu, którego autor nie widzi, nie może poprawić i którego nie ma w historii
repozytorium. Wszystko poniżej pochodzi z realnych korekt albo realnych strat, nie z przewidywań.

---

## 1. Jak zapadają decyzje

**Decyzje projektowe omawia się i zatwierdza przed napisaniem kodu.** Autor prosi o opinię
i kontruje argumentami — jego zastrzeżenia traktuj jako coś, czemu warto ustąpić, gdy się bronią,
a nie jako coś, przed czym trzeba bronić własnej propozycji.

**Pozycja z kolejki nie jest zleceniem.** `tasks.md` mówi, co było do zrobienia w dniu, w którym
to zapisano — nie co jest do zrobienia dzisiaj. Zanim wykonasz pozycję, sprawdź w kodzie, czy jej
przesłanka nadal jest prawdziwa, i zgłoś, kiedy nie jest. Pozycja bez wyzwalacza starzeje się po
cichu: nic nie zmusza do jej ponownego przemyślenia, a sam fakt, że stoi zapisana, zaczyna z czasem
uchodzić za uzasadnienie. „Dokument tak mówi" nie jest odpowiedzią na pytanie, po co to robimy.

*Skąd to się wzięło:* 2026-09-13 asystent wykonał dwie pozycje z sekcji „Odłożone", nie sprawdziwszy
przesłanki żadnej z nich. Obie okazały się słabe. Pierwsza opisywała jako niedokończoną pracę, której
istniejącymi tokenami wykonać się nie dało — i nie dało się już w dniu, w którym ją zapisano. Druga
chciała zamrozić testami kolejność kroków startowych, z których trzy przygotowują rzeczy oznaczone
w tych samych dokumentach jako rusztowanie do wymiany. Trzecia rzecz z tamtej sesji, jedyna, która
się obroniła, nie pochodziła z żadnego dokumentu — wyszła z czytania kodu przy okazji innego zadania.

**Zielone światło jest per etap.** Nie realizuj kilku etapów jednym zamachem, nawet jeśli widzisz
całą drogę. Proponuj, rekomenduj **jedną** opcję z uzasadnieniem, czekaj.

**Wypisuj osobno to, co rozstrzygnąłeś sam**, bo brief czy polecenie tego nie przesądzało. Ta
lista jest miejscem, w którym autor zakłada weto — bez niej musiałby czytać cały diff, żeby
znaleźć założenia, których nie robił.

**Interfejs to jego rzemiosło.** Nie dopracowuj UI i nie proponuj jego zmian z własnej inicjatywy.
Ale **rób funkcje osiągalnymi** — moduł, którego nie da się otworzyć z działającej aplikacji, jest
dla niego bezwartościowy.

Zdarza się, że sam poprosi o „technicznie poprawny UI bez fajerwerków", żeby nie blokować
funkcjonalnego kawałka czekaniem na własny mockup. Wtedy bierz to zadanie, ale zdefiniuj „bez
fajerwerków" wąsko dla tego, kto je wykona: wyłącznie istniejące tokeny, nigdy nowe; zero zmian
w plikach motywu; style lokalne dla widoku; żadnych animacji, kontrolek własnych ani liczb
wpisanych wprost. Cel jest taki, żeby wszystko, co będzie chciał przesunąć, leżało w jednym
oczywistym miejscu, i żeby nie trzeba było najpierw rozbierać czegoś wymyślonego po drodze.

**Koniec kawałka pracy = commit. Bez pytania i bez czekania na polecenie.** Decyzja autora
z 2026-09-14. Domknięty etap ma wylądować w historii od razu — **także wtedy, gdy autorowi wynik się
nie podoba**. Od tego jest git, żeby dało się cofnąć, poprawić albo porzucić rzecz zapisaną; trzymanie
niezatwierdzonej pracy w drzewie roboczym nie jest formą recenzji, tylko sposobem na jej zgubienie.
Niezadowolenie z wyniku rozstrzyga się następnym commitem albo cofnięciem tego, nie wstrzymaniem
zapisu.

**Sprawdź `git status` przed commitem.** Jego zmiany w toku potrafią leżeć w drzewie roboczym obok
Twojej pracy i nie wolno ich zagarnąć do Twojego commita. Ta reguła jest starsza od powyższej i ma
przed nią pierwszeństwo: „commituj zawsze" znaczy „commituj **swoje** zawsze".

**Porządki po własnej pracy asystent robi sam, bez pytania.** Decyzja autora z 2026-09-22: wpis
w `.gitignore`, usunięcie kopii roboczej i gałęzi subagenta po przeniesieniu wyniku, drobna higiena
repozytorium — to decyzje asystenta. Pytanie o nie kosztuje dodatkową turę rozmowy i niczego nie
wnosi. Pyta się o to, co zmienia aplikację, dokumenty z decyzjami albo cudzą pracę.

**Subagentów uruchamiaj w tle.** Blokowanie się na subagencie zabiera mu czas, który wolałby spędzić
na rozmowie o kolejnych decyzjach.

**Jeden wykonawca naraz, chyba że autor prosi o równoległość.** Decyzja autora z 2026-09-23.
Subagent oszczędza okno kontekstu architekta i jest tu wskazany — ale kilku równoległych skraca
czas, nie budżet tokenów, a czas w tej pracy nie gra roli. Kolejne zlecenie idzie więc po
zamknięciu poprzedniego; równolegle tylko na wyraźne polecenie autora.

**Subagentów nie uruchamiaj na najdroższym modelu.** Decyzja autora z 2026-09-14. Zadania, które im
się tu powierza, są z definicji wykonawcze — brief jest długi i precyzyjny właśnie po to, żeby myślenie
zostało po stronie zlecającego. Model wybiera się jawnie przy uruchomieniu, nie zostawia domyślnego.

**Sesja architektoniczna czyta dokumenty, nie źródła.** Decyzja autora z 2026-09-22. Okno kontekstu
asystenta prowadzącego sesję jest jej najcenniejszym zasobem, więc asystent projektuje i przegląda,
a kod pisze i czyta subagent w wąsko zakrojonym zadaniu — wynik asystent weryfikuje, zanim go przyjmie.
Konkret z kodu, potrzebny do decyzji, przynosi `code-map.md` albo subagent. **Deleguj kod, nie
decyzje:** dokumenty tego repozytorium niosą decyzje, więc pisze je asystent sam.

### Obieg jednego etapu

Ustalony z autorem 2026-09-22.

```
 AUTOR                  ARCHITEKT                       SUBAGENT (Sonnet)
 │                         │                                │
 │◄── propozycja etapu ────┤  cel, co autor zobaczy,        │
 │    zielone światło ────►│  co rozstrzygam sam, pytania   │
 │                         ├── brief ──────────────────────►│  osobna kopia repozytorium,
 │    rozmowa o kolejnej ◄─┤   (w tle)                      │  build, testy, raport
 │    decyzji              │◄── raport ─────────────────────┤
 │                         ├ weryfikacja → commit, scalenie │
 │◄── raport + co kliknąć ─┤                                │
 │    uwagi / weto ───────►│  → poprawka kolejnym commitem  │
```

* **Autor** — kierunek, interfejs, zielone światło na każdy etap, sprawdzenie w działającej
  aplikacji, weto na rzeczy rozstrzygnięte samodzielnie.
* **Architekt** — asystent prowadzący sesję. Proponuje etap, projektuje styki między ramą, biblioteką
  i systemem (to one są decyzjami), pisze briefy, weryfikuje, commituje, dogania dokumenty.
* **Subagent implementacyjny** — wykonuje wąski brief. Gdy brief czegoś nie przesądza, zatrzymuje się
  i zgłasza, zamiast decydować.

**Weryfikacja bez czytania implementacji linijka po linijce.** Build bez ostrzeżeń; liczba testów nie
niższa niż baseline; testy granic zielone; lista rzeczy, które subagent rozstrzygnął sam; diff
**styków** — tego, co rama wystawia systemowi, co zakładka dostaje, referencji między projektami —
bo styki są architekturą. W środek implementacji architekt zagląda tylko wtedy, gdy raport albo
testy każą. Drugi subagent, porównujący wynik z briefem i z pięcioma zakazami, idzie wyłącznie do etapów
dotykających zapisu stanu albo granicy automatyzacji. Decyzja autora z 2026-09-22: przy pozostałych
kosztuje tyle co sama implementacja, a architekt sprawdza zakazy celowanym przeszukaniem diffu —
zapisy wywoływane przez zdarzenia, pola czasu, wybór celów.

**Autor uruchamia wyłącznie `master`.** Decyzja autora z 2026-09-22: pozostałe gałęzie są robocze
i nie podaje mu się poleceń uruchamiających aplikację z kopii subagenta. Wynik zweryfikowany przez
architekta trafia więc do `master` **przed** sprawdzeniem przez autora, a to, co w działającej
aplikacji okaże się złe, naprawia następny commit albo cofnięcie — zgodnie z regułą „koniec kawałka
pracy = commit". Tego samego dnia autor uruchomił z przyzwyczajenia `master` zamiast podanej mu
kopii subagenta i sprawdzał wersję bez połowy etapu.

**Etap, który zmienia start albo nawigację, zanim uzna się go za zamknięty, uruchamia autor** — na
`master`, po scaleniu. Testy nie otwierają okna. 2026-09-22 etap 1 przeszedł build, 247 testów i przegląd styków, a mimo to aplikacja padała po
wyborze systemu (widok budowany poza wątkiem okna), na pasku brakowało pozycji kampanii, a treść
skakała przy wejściu. Wszystko to widać w pierwszej minucie działania programu i w żadnym teście.

**Commit i scalenie robi architekt**, po weryfikacji — subagent zostawia wynik w swojej kopii.
Dokumenty dogania architekt na końcu etapu: `code-map.md` (jak jest), `tasks.md` (co dalej).

---

## 2. Jak raportować

**Zacznij od krótkiego podsumowania w prostym języku** — jakie są decyzje i zmiany oraz dlaczego.
W tej części **nie ma** nazw klas, ścieżek plików, numerów sekcji ani hashy commitów.

**Szczegóły low-level pomiń.** Nie odtwarzaj ich z własnej inicjatywy w osobnym załączniku — żyją
i tak w briefach dla subagentów oraz w historii gita. Podaj konkret, gdy autor o niego zapyta.

**Pytania wymagające jego decyzji formułuj w prostym języku.** Nazwa techniczna zostaje tylko
wtedy, gdy bez niej pytanie przestaje być zrozumiałe — nigdy jako dowód, że naprawdę zajrzałeś
do kodu.

**Raport z etapu implementacyjnego kończy się listą dwóch–czterech rzeczy do sprawdzenia
w aplikacji.** Asystent nie widzi okna, a to, jak się z aplikacji korzysta, należy do autora.

**Dlaczego:** gęstość referencji nie jest dowodem rzetelności. Jego słowa: „kilkadziesiąt różnych
linków, definicji kluczy etc potrafi zdezorientować". Raport ma się czytać bez zaglądania do drugiej
warstwy.

---

## 3. Jak pisać dokumenty tego repozytorium

1. **`CLAUDE.md` nazywa własności, nie dzisiejsze mechanizmy.** „Punkt rozszerzenia deklaruje się
   jawnie" — tak. „Manifest i `Requires`" — nie. Nazwy mechanizmów żyją w `architecture.md`. Test
   przed wpisaniem czegokolwiek: czy to zdanie przetrwa przeprojektowanie tej części systemu?
   Jeśli nie, idzie do dokumentu zmienianego świadomie.

   *Skąd to się wzięło:* asystent chciał wpisać moduły (`ICampaignModule`, `ModuleCatalog`) na listę
   pilnowanych szwów. Autor odrzucił — refaktoryzacja może zmienić kierunek, a wtedy nieaktualna
   linijka zostaje w najbardziej zaraźliwym pliku w repo. Miał rację; broniony argument („moduł
   jest centralnym mechanizmem") był prawdziwy *dzisiaj* i właśnie dlatego był problemem.

2. **Nie wpisuj reguły, której nie da się dziś wykonać.** Subagent, który raz odbije się od
   niemożliwego wymogu, przestaje traktować całą listę poważnie.

3. **Odsyłacze do sekcji po nazwie, nie po numerze.** Dwa razy w jednej sesji przenumerowanie
   zerwało linki, i to cicho: wskaźnik na „§13" o nawigacji po jakimś czasie wskazywał na paczki
   i bezpieczeństwo, a nic tego nie zgłosiło.

   *Że to nie jest przesada:* przegląd 2026-09-14 znalazł w mapie kodu odsyłacz do „§13" po
   warstwy i granice, które stoją w architekturze osiem sekcji wcześniej. Dokładnie ten sam błąd,
   ten sam numer, cicho przez kilka sesji.

4. **Każdy fakt ma jeden dom — nie streszczaj cudzego.** Zanim wpiszesz uzasadnienie, sprawdź, czy
   nie stoi już tam, gdzie należy: „co obowiązuje" w `architecture.md`, „dlaczego" — za przyjętym,
   przeciw odrzuconemu, co było wcześniej — w `decisions.md`, „jak jest dziś" w `code-map.md`, „co
   dalej" w `tasks.md`. Odeślij po nazwie sekcji, zamiast powtórzyć. Tabela własności jest
   w `README.md`.

   *Skąd to się wzięło:* przegląd 2026-09-14 znalazł ten sam argument w pięciu dokumentach naraz
   (dwie flagi jako dowód, że stary format przeciekał układem) i w czterech (zmiana wpisu
   traktowana jak patchnote). Powtórzenia brały się z dobrej intencji — każdy dokument miał się
   czytać samodzielnie. Cena była taka, że **żadnego nie dało się bezpiecznie pominąć**, więc
   koszt wejścia w sesję był sumą wszystkich sześciu.

5. **Architektura deklaruje, rejestr uzasadnia.** Decyzja autora z 2026-09-22. `architecture.md`
   zawiera deklaracje i ich konsekwencje, w czasie teraźniejszym. Argumenty, odrzucone warianty
   i historia („wcześniej…", „do dnia…", „obowiązywało…") idą do `decisions.md`, do sekcji o tej
   samej nazwie co sekcja architektury; w architekturze zostaje odsyłacz. Sprawdzian na każdym
   zdaniu: konsekwencja („poprawka w paczce dociera do istniejących kampanii") zostaje, argument
   („bo zmianę wpisu traktujemy jak patch balansujący grę") idzie do rejestru.

   *Skąd to się wzięło:* autor — wdrożenie się albo nadrobienie zaległości wymagało przeczytania
   dziesięciu punktów „dlaczego" przy każdej deklaracji. Do tego każda sesja dokładała do
   architektury zdania „do dnia X obowiązywało…"; sesja, która tę regułę ustanowiła, dołożyła
   ich pięć, zanim ją ustanowiła.

---

6. **Mapa kodu ma nieść sądy, nie spis plików.** Obserwacja autora z 2026-09-22, po tym jak
   aktualizacja `code-map.md` zjadła dziesiątą część budżetu sesji. Dokument dzieli się na dwie
   części o różnej wartości. Tabele „który plik za co odpowiada" odtwarza się ze struktury katalogów
   i jednego przeszukania w sekundę — i to one rozjeżdżają się po każdej zmianie. Ocena stanu
   (co dojrzałe, co rusztowanie, gdzie dług, czego nie pokrywa żaden test), granice i punkty styku
   są sądami, których z kodu wyczytać się nie da w rozsądnym czasie — i to dla nich ten dokument
   istnieje.

   *Co z tego wynika:* spis plików wypada, zostaje część oceniająca; mapę aktualizuje się **po etapie
   zmieniającym strukturę**, nie po każdej sesji; konkret o kodzie, potrzebny do jednej decyzji,
   bierze się doraźnie od subagenta tylko do odczytu — pytanie o dzisiejszą drogę zapisu kosztowało
   ułamek tego, co aktualizacja całej mapy.

   **Mapa jest dla architekta, nie dla wykonawcy** — przegląd autora i asystenta z 2026-09-23. Autor
   zauważył, że subagenci spędzają dużą część pracy na szukaniu w kodzie, i zapytał, czy mapa działa
   jako nawigacja. Nie działa i nie miała działać: jej nagłówek od początku mówi, że nie czyta się
   jej, żeby edytować plik. Dla architekta, który nie czyta źródeł, jest oknem na kod — ocena stanu,
   granice, ścieżki, wzorce zmian. Tego samego dnia żaden brief nie wskazywał mapy, więc szukanie
   subagentów nic o niej nie mówi. Wniosek: nawigację niesie **brief** (punkty niżej), nie mapa.

## 4. Briefy dla subagentów

Każdy brief w tym repozytorium musi nieść te trzy zakazy. Wszystkie pochodzą z incydentów.

1. **Nie zabijaj procesów** (`Stop-Process`, `taskkill`). Subagent ubił działającą instancję
   aplikacji autora, żeby odblokować `dotnet clean`. Poprawne zachowanie to zgłosić blokadę, nie
   sprzątnąć cudzy proces — patrz „Środowisko".
2. **Nie przeszukuj `bin/`, `obj/` ani niczego poza repozytorium** — pakietów NuGet, źródeł
   bibliotek, reszty dysku. Subagent zaczął grepować pliki `.dll` w poszukiwaniu referencji do typów.
   Skanuj tylko źródła repozytorium; katalogi wyjściowe zawierają kopie i pochodne, więc odpowiedź
   jest i zaszumiona, i kosztowna. 2026-09-23 inny przeszukiwał cały dysk w poszukiwaniu źródeł
   kontrolki Avalonii, żeby ustalić, jak rysuje tło względem krawędzi — brief tego nie przesądzał.
   Wiedzę o zachowaniu biblioteki zdobywa się pomiarem (wyrenderuj i zmierz) albo się ją zgłasza;
   luka, która pcha wykonawcę poza repozytorium, jest luką briefu.
3. **Nie tłum ostrzeżeń** (`#pragma`, `<NoWarn>`, `SuppressMessage`). `TreatWarningsAsErrors` jest
   włączone celowo — kompilator jest tu walidatorem treści. Ostrzeżenie się naprawia u źródła albo
   zgłasza, nigdy nie wycisza.

**Subagent implementacyjny pracuje w osobnej kopii repozytorium** (worktree), a do głównej gałęzi
trafia wynik zweryfikowany. Decyzja z 2026-09-22. Nie potknie się wtedy o niezatwierdzone zmiany
autora w drzewie roboczym ani o blokadę katalogu wynikowego przez podgląd Ridera (sekcja
*Środowisko*). **Kopia startuje jednak ze zdalnej gałęzi, nie z lokalnej** — brief każe ją najpierw
zrównać z lokalnym `master`; szczegół i powód w tej samej sekcji.

Co jeszcze się sprawdziło:

* **Podawaj baseline liczby testów.** Aktualna liczba stoi w nagłówku `tasks.md`. Bez niej subagent
  nie wie, czy spadek jest regresją. 2026-09-22 to niezgodna liczba testów zdradziła, że subagent
  pracował na kodzie sprzed dwudziestu czterech commitów.
* **Każ osobno wypisać rzeczy rozstrzygnięte samodzielnie.** Tak wyszły dwa realne błędy
  w briefach.
* **Podawaj warunek zatrzymania jako informację, nie jako przeszkodę.** Gdy brief mówi „jeśli
  referencja okaże się żywa, zatrzymaj się i zgłoś", dopisz, że to właśnie jest wynik, po który
  wysyłasz zadanie. Inaczej subagent traktuje zatrzymanie jako porażkę i próbuje obejść.
* **Ogranicz długość raportu**, gdy budżet jest niski.
* **Podawaj w briefie punkty wejścia** — konkretne pliki i typy, od których zacząć, z mapy albo ze
  zwiadu. Brief 2 etapu 4 (2026-09-23) niósł akapit „Stan dziś" z nazwami miejsc do zmiany; to jest
  wzór. Gdy zadanie pasuje do wzorca z sekcji mapy „Punkty rozszerzeń", wskaż tę sekcję z nazwy.
  Szukanie, którego brief nie oszczędził, jest kosztem briefu, nie wykonawcy.

**Dlaczego to tu stoi:** briefy w tym repo są długie i precyzyjne, i właśnie dlatego łatwo w nich
pominąć zakaz, który wydaje się oczywisty.

---

## 5. Rozstrzygnięcia, które wracają

Rzeczy raz rozstrzygnięte, które mimo to wracają jako „nowe pomysły". Pełne argumenty są
w `decisions.md`; tu jest tylko tyle, żeby rozpoznać temat i nie zaczynać go od zera.

### Nawigacja

**Przeprojektowanie nawigacji zapadło 2026-09-22** — ekran wyboru systemu i pasek boczny z trzema
kategoriami. Rusztowanie w `AppShellViewModel.OnSectionSelected` zostaje łańcuchem `if`-ów do etapu
przebudowy, który je zastąpi; nie uogólniaj go wcześniej w router.

**Pasek boczny zmieniający zawartość po wyborze systemu i otwarciu kampanii jest przyjęty** — z
propozycji autora, po tym jak wszystkie trzy powody jego dawnego odrzucenia przestały obowiązywać.
Odrzucone zostaje to, co z tamtej pozycji przeżyło: **zakładki z danych.** Pasek wypełnia
skompilowany system, nigdy paczka.

Zanim zaproponujesz cokolwiek o interfejsie, przeczytaj do końca sekcję „Nawigacja: ekran wyboru
systemu i pasek boczny" w `architecture.md`. Tam są decyzje, nie sugestie.

### Granica automatyzacji — jego własne uzasadnienia

* **„Są automatyzacje, które są użyteczne, intuicyjne i oszczędzające czas."** Handel, w którym MG
  sam usuwa przedmiot, dopisuje złoto graczowi i przedmiot kupcowi, jest „strasznie męczący, długi
  i monotonny". Wniosek: aplikacja księguje decyzje MG — także jednym kliknięciem i na kilku rzeczach
  naraz — ale ich nie podejmuje.
* **Homebrew nie dodaje zawartości — modyfikuje rdzeń.** Jego przykład: tryb barbarzyńskich klanów
  w Cywilizacji VI zastępuje domyślną mechanikę, zamiast coś do niej dokładać. Stąd dodatki, które
  mogą zastępować, a nie tylko dokładać.

**Lekcja dla asystenta: zakazy czytaj według intencji zapisanej w `architecture.md`, nie według
litery.** 2026-09-22 asystent zastosował literę czwartego zakazu do handlu i pomylił decyzję MG z jej
zaksięgowaniem; autor nazwał to ekstremizmem i miał rację — w jednym zakazie z pięciu. Pozostałe
cztery okazały się dokładnie tym, co odróżnia aplikację od gry komputerowej. Gdy litera i intencja się
rozjeżdżają, zgłoś rozjazd — nie egzekwuj litery i nie porzucaj intencji.

### Warstwa treści — jego własne uzasadnienia

Rozstrzygnięcia są w `architecture.md`. Tu zostają zdania autora, bo to one kończą dyskusję przy
trzecim powrocie tematu, a nie zapis w tabeli.

* **„Aplikacja WIE co to potwór i wie jak go wyświetlić."** Karta jest **projektowana**, nie
  składana z listy elementów przyniesionej w danych. Dowód, że stary format przeciekał: `compact`
  i `selfDescribing` broniono jako stwierdzeń o treści, a były decyzjami o układzie.
* **„Zmianę wpisu traktujemy jak patchnote balansujący grę."** To jest powód, dla którego nakładka
  instancji jest łączem do wpisu bez materializacji — rzadką łatką scalaną przy odczycie, a nie
  kopią wartości.
* **Wpis JSON nie zmienia formatu ani o znak.** Warstwa 3 ma zostać dynamiczna; `values`
  deserializuje się wprost w rekord. Ten punkt padał wielokrotnie i jest twardy.

---

## 6. Środowisko

**`MSB3021`/`MSB3027` na katalogu `bin` projektu wykonywalnego to nie jest błąd kodu.** Blokuje go
`Avalonia.Designer.HostApp` — podglądacz XAML, którego uruchamia Rider, gdy otwarta jest zakładka
z podglądem `.axaml`. Rozpoznanie bez ingerencji w proces:

```
Get-CimInstance Win32_Process -Filter "ProcessId = <pid>" | Select-Object ParentProcessId, CommandLine
```

Rodzicem jest `rider64.exe`. **Rozwiązanie: zamknąć zakładkę z podglądem w IDE.**

**Weryfikacja mimo blokady jest pełna.** Cztery projekty testowe nie zależą od `DungeonApp.App`,
więc build i testy na nich dają potwierdzenie kompilatora dla silnika, powłoki i zestawu treści.
Niepotwierdzone zostaje wtedy wyłącznie to, że sam plik wykonywalny się linkuje.

**Mimo wszystko nie zabijaj tego procesu z własnej inicjatywy.** Po nazwie procesu nie widać
różnicy między podglądaczem a działającą instancją `DungeonApp.App` — widać ją dopiero po linii
poleceń. Zgoda udzielona raz nie znosi zakazu z „Briefy dla subagentów".

**Kopię po diagnozie zostaw, dopóki nie zapadnie, kto robi poprawkę.** Reguła „po przeniesieniu
wyniku usuń kopię i gałąź" dotyczy wyniku scalonego do `master`. Diagnoza nie ma czego scalać,
a po niej zwykle przychodzi poprawka — 2026-09-23 autor chciał ją powierzyć temu samemu wykonawcy,
który znał już przyczynę, i nie dało się go wznowić, bo architekt skasował jego kopię zaraz po
raporcie. Wznowienie wymaga istniejącej kopii; nowy wykonawca zaczyna od zera.

**Kopia robocza subagenta startuje z `origin/master`, nie z lokalnego `master`.** Autor nie wypycha
na bieżąco, więc zdalna gałąź bywa daleko w tyle — 2026-09-22 o dwadzieścia cztery commity, i subagent
zrobił na niej całe zadanie. **Brief implementacyjny zaczyna się od `git reset --hard master`
w kopii subagenta** i każe podać w raporcie commit, od którego liczony jest diff. Kopie robocze leżą
w `.claude/worktrees/`, wykluczonym w `.gitignore`; po przeniesieniu wyniku do `master` asystent
usuwa kopię i gałąź subagenta.

**Zadania redakcyjne na długich dokumentach architekt robi sam; subagentom zostaje kod.** Obserwacja
z 2026-09-22, nie reguła o przyczynie: czterech subagentów z rzędu (Sonnet) na zadaniu
przeniesienia tekstu między dokumentami stanęło bez postępu — najpierw jeden na całości, potem trzej
na fragmentach po około 350 linii — i żaden nie zapisał wyniku. Przyczyny nie ustalono. Architekt
zrobił to samo zadanie sam w kilka minut, bo stary tekst miał już w kontekście.
