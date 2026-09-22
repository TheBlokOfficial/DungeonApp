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

**Subagentów uruchamiaj w tle.** Blokowanie się na subagencie zabiera mu czas, który wolałby spędzić
na rozmowie o kolejnych decyzjach.

**Subagentów nie uruchamiaj na najdroższym modelu.** Decyzja autora z 2026-09-14. Zadania, które im
się tu powierza, są z definicji wykonawcze — brief jest długi i precyzyjny właśnie po to, żeby myślenie
zostało po stronie zlecającego. Model wybiera się jawnie przy uruchomieniu, nie zostawia domyślnego.

**Sesja architektoniczna czyta dokumenty, nie źródła.** Decyzja autora z 2026-09-22. Okno kontekstu
asystenta prowadzącego sesję jest jej najcenniejszym zasobem, więc asystent projektuje i przegląda,
a kod pisze i czyta subagent w wąsko zakrojonym zadaniu — wynik asystent weryfikuje, zanim go przyjmie.
Konkret z kodu, potrzebny do decyzji, przynosi `code-map.md` albo subagent. **Deleguj kod, nie
decyzje:** dokumenty tego repozytorium niosą decyzje, więc pisze je asystent sam.

---

## 2. Jak raportować

**Zacznij od krótkiego podsumowania w prostym języku** — jakie są decyzje i zmiany oraz dlaczego.
W tej części **nie ma** nazw klas, ścieżek plików, numerów sekcji ani hashy commitów.

**Szczegóły low-level pomiń.** Nie odtwarzaj ich z własnej inicjatywy w osobnym załączniku — żyją
i tak w briefach dla subagentów oraz w historii gita. Podaj konkret, gdy autor o niego zapyta.

**Pytania wymagające jego decyzji formułuj w prostym języku.** Nazwa techniczna zostaje tylko
wtedy, gdy bez niej pytanie przestaje być zrozumiałe — nigdy jako dowód, że naprawdę zajrzałeś
do kodu.

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
   nie stoi już tam, gdzie należy: „dlaczego tak" w `architecture.md`, „dlaczego nie tamto"
   w `decisions.md`, „jak jest dziś" w `code-map.md`, „co dalej" w `tasks.md`. Odeślij po nazwie
   sekcji, zamiast powtórzyć. Tabela własności jest w `README.md`.

   *Skąd to się wzięło:* przegląd 2026-09-14 znalazł ten sam argument w pięciu dokumentach naraz
   (dwie flagi jako dowód, że stary format przeciekał układem) i w czterech (zmiana wpisu
   traktowana jak patchnote). Powtórzenia brały się z dobrej intencji — każdy dokument miał się
   czytać samodzielnie. Cena była taka, że **żadnego nie dało się bezpiecznie pominąć**, więc
   koszt wejścia w sesję był sumą wszystkich sześciu.

---

## 4. Briefy dla subagentów

Każdy brief w tym repozytorium musi nieść te trzy zakazy. Wszystkie pochodzą z incydentów.

1. **Nie zabijaj procesów** (`Stop-Process`, `taskkill`). Subagent ubił działającą instancję
   aplikacji autora, żeby odblokować `dotnet clean`. Poprawne zachowanie to zgłosić blokadę, nie
   sprzątnąć cudzy proces — patrz „Środowisko".
2. **Nie przeszukuj `bin/` ani `obj/`.** Subagent zaczął grepować pliki `.dll` w poszukiwaniu
   referencji do typów. Skanuj tylko źródła; katalogi wyjściowe zawierają kopie i pochodne, więc
   odpowiedź jest i zaszumiona, i kosztowna.
3. **Nie tłum ostrzeżeń** (`#pragma`, `<NoWarn>`, `SuppressMessage`). `TreatWarningsAsErrors` jest
   włączone celowo — kompilator jest tu walidatorem treści. Ostrzeżenie się naprawia u źródła albo
   zgłasza, nigdy nie wycisza.

Co jeszcze się sprawdziło:

* **Podawaj baseline liczby testów.** Bez niej subagent nie wie, czy spadek jest regresją.
* **Każ osobno wypisać rzeczy rozstrzygnięte samodzielnie.** Tak wyszły dwa realne błędy
  w briefach.
* **Podawaj warunek zatrzymania jako informację, nie jako przeszkodę.** Gdy brief mówi „jeśli
  referencja okaże się żywa, zatrzymaj się i zgłoś", dopisz, że to właśnie jest wynik, po który
  wysyłasz zadanie. Inaczej subagent traktuje zatrzymanie jako porażkę i próbuje obejść.
* **Ogranicz długość raportu**, gdy budżet jest niski.

**Dlaczego to tu stoi:** briefy w tym repo są długie i precyzyjne, i właśnie dlatego łatwo w nich
pominąć zakaz, który wydaje się oczywisty.

---

## 5. Rozstrzygnięcia, które wracają

Rzeczy raz rozstrzygnięte, które mimo to wracają jako „nowe pomysły". Pełne argumenty są
w `decisions.md` i `architecture.md`; tu jest tylko tyle, żeby rozpoznać temat i nie zaczynać go
od zera.

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
