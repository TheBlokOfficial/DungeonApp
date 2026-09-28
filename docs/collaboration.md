# DungeonApp — jak prowadzić tę pracę

**Status: umowa o współpracy, nie opis programu.** Pozostałe pięć dokumentów opisuje aplikację;
ten opisuje pracę nad nią. Dlatego nie konkuruje z nimi o fakty i nie ma miejsca w ich porządku
pierwszeństwa — przy rozbieżności o kod wygrywa kod, a ten dokument milczy.

Jest adresowany do asystenta (i do każdego subagenta, któremu ktoś powierzy zadanie w tym
repozytorium). Każda reguła pochodzi z realnej korekty albo realnej straty, nie z przewidywań.
**Skąd** — incydent, pomiar, wcześniejsze brzmienie — stoi w [decisions.md](decisions.md),
*Część C — obieg pracy*, pod nazwą reguły. Tamtą część czyta się, gdy reguła jest kwestionowana albo
ma się zmienić, nie na starcie sesji.

**Na starcie sesji czyta się ten dokument bez dwóch sekcji czytanych przed czynnością:** *Jak pisać
dokumenty tego repozytorium* — przed pierwszą zmianą dokumentu w sesji; *Briefy dla subagentów* —
przed pierwszym briefem albo uruchomieniem subagenta. Decyzja autora z 2026-09-26. Reguła ogólna:
część dokumentu, która służy jednej czynności, czyta się przed tą czynnością, nie na starcie. Granice
sekcji z numerami linii daje `grep -n '^## '`.

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

**Zielone światło jest per etap.** Nie realizuj kilku etapów jednym zamachem, nawet jeśli widzisz
całą drogę. Proponuj, rekomenduj **jedną** opcję z uzasadnieniem, czekaj.

**Polecenie ruszenia z etapem jest zielonym światłem dla jego zapisanej propozycji.** Decyzja autora
z 2026-09-25. Gdy autor każe ruszyć z etapem, którego propozycja stoi w dokumentach, architekt nie
pyta o zgodę drugi raz: uruchamia wykonawcę i równolegle pokazuje skrót propozycji wraz z tym, co
rozstrzygnął sam przy briefie. Weto przychodzi w trakcie — idzie do wykonawcy wiadomością albo
następną rundą. Etap bez zapisanej propozycji czeka na zgodę jak dotąd.

**Wypisuj osobno to, co rozstrzygnąłeś sam**, bo brief czy polecenie tego nie przesądzało. Ta
lista jest miejscem, w którym autor zakłada weto — bez niej musiałby czytać cały diff, żeby
znaleźć założenia, których nie robił.

**W interfejsie asystent decyduje o tym, co rozstrzyga wiedza o interfejsach; autor — o tym, czego ona
nie rozstrzyga.** Decyzja autora z 2026-09-24, dwukrotnie doprecyzowana tego samego dnia.

* **Asystent** — architekt i wykonawca, każdy na swoim poziomie — projektuje interfejs na podstawie
  profesjonalnej wiedzy o czytelnym i intuicyjnym interfejsie: konwencje platformy, hierarchia,
  czytelność, stany, zachowanie kontrolek, spójność. Tu nie pyta i nie czeka na polecenie; decyzję
  z tego obszaru uzasadnia wiedzą, nie gustem. Makieta autora jest wymaganiem wobec jednego ekranu, nie
  specyfikacją kontrolek — gdy każe coś nieczytelnego albo nieintuicyjnego, asystent mówi to wprost.
* **Autor** decyduje o tym, czego teoria interfejsu nie obejmuje: jak chce, żeby produkt wyglądał
  i działał — **z perspektywy klienta, który chce dostać produkt, nie projektanta, który go
  projektuje.** Jego uwaga po obejrzeniu jest wymaganiem klienta: rozstrzyga, co ma być, a jak to
  zrobić porządnie — to już asystent.
* **Rzemiosło** to obowiązek wykonawcy bez pytania i bez osobnego polecenia w briefie — lista kontrolna
  stoi w definicji wykonawcy (`.claude/agents/wykonawca.md`, *Rzemiosło interfejsu*): każdy stan
  kontrolki obsłużony świadomie, fokus zdejmowany kliknięciem obok i Escape, układ niezależny od
  zawartości, pasek przewijania tylko przy potrzebie i jako nakładka, tekst wyrównany
  w polu, przycisk bez działania wygaszony, strzałka zamiast ręki. Lista jest minimum, nie granicą: błąd rzemiosła spoza niej
  wykonawca też poprawia — w kontrolce, którą i tak zmienia.
* **Rób funkcje osiągalnymi** — moduł, którego nie da się otworzyć z działającej aplikacji, jest dla
  autora bezwartościowy.

**Nowy wygląd powstaje w motywie ramy, nie w widoku.** Widok składa się z kontrolek fundamentu
i istniejących tokenów, dobranych po znaczeniu zapisanym przy tokenie, nie po barwie (reguła —
`architecture.md`, *Niezmiennik interfejsu*). Brakujący element albo kolor dochodzi do motywu
osobnym zleceniem, a element spoza zestawu powstaje w widoku jawnie jako własny i przechodzi do
motywu przy drugim użyciu (`architecture.md`, *Niezmiennik interfejsu*). Cel: wszystko, co autor będzie chciał
zmienić, leży w jednym oczywistym miejscu.

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

**Drobne poprawki czekają na swój obszar.** Decyzja autora z 2026-09-24. Poprawka zgłoszona w trakcie
pracy nie dostaje osobnego zlecenia „w następnej turze" — trafia do `tasks.md`, *Poprawki czekające
na obszar*, pogrupowana po obszarze kodu. Zabiera ją pierwsze zlecenie, które i tak wchodzi w ten
obszar; osobny wykonawca dopiero wtedy, gdy uzbiera się ich tyle, że warto. Architekt przy pisaniu
każdego briefu przegląda tę sekcję. Wykonawca, który już zna miejsce, robi poprawkę bez szukania go
od nowa.

**Auto-udoskonalanie: obieg pracy ma tanieć z sesji na sesję.** Decyzja autora z 2026-09-25.
Asystent zauważa, gdzie praca płaci czas albo tokeny bez potrzeby — lektura, która nic nie wniosła,
pytanie, które dokument mógł uprzedzić, szukanie, które mógł oszczędzić brief, reguła czytana przy
każdym starcie, choć potrzebna raz — i zgłasza to autorowi przy najbliższym raporcie, w jednym–dwóch
zdaniach z propozycją dopisku albo skrótu. Do dokumentów trafia to dopiero po zgodzie autora, jak
każda zmiana. **Uwaga nie zostaje w samej rozmowie** (autor, tego samego dnia) — rozmowa ginie
z sesją. Albo kończy się pytaniem „czy mogę to wpisać?”, albo, gdy czeka na potwierdzenie (pomiar
z kolejnego zlecenia, powtórzenie się zjawiska), zapisuje się ją od razu z wyzwalaczem powrotu —
w *Notkach* dokumentu zadania, a w sesji głównej w `tasks.md`, *Odłożone* — i mówi autorowi, na co
czeka. Zapowiedź „wrócę do tego” bez zapisu i bez wyzwalacza się nie liczy.
Pomiar wygrywa z wrażeniem: gdzie się da, propozycja podaje liczbę (kroki, minuty, tokeny, linie
lektury).
**Propozycja jest uniwersalna** (autor, 2026-09-25): upraszcza obieg każdej pracy, nie tej jednej —
nazywa rodzaj sytuacji i regułę, którą da się zastosować w dowolnym zadaniu, przy dowolnej treści.
„Nie czytaj X, bo tym razem nie był potrzebny” to obserwacja z jednej sesji, nie uproszczenie; staje
się propozycją dopiero jako reguła o tym, **kiedy** taka lektura jest potrzebna.

**Subagentów uruchamiaj w tle.** Blokowanie się na subagencie zabiera mu czas, który wolałby spędzić
na rozmowie o kolejnych decyzjach.

**Jeden wykonawca naraz, chyba że autor prosi o równoległość.** Decyzja autora z 2026-09-23.
Subagent oszczędza okno kontekstu architekta i jest tu wskazany — ale kilku równoległych skraca
czas, nie budżet tokenów, a czas w tej pracy nie gra roli. Kolejne zlecenie idzie więc po
zamknięciu poprzedniego; równolegle tylko na wyraźne polecenie autora.

**Subagentów nie uruchamiaj na najdroższym modelu.** Decyzja autora z 2026-09-14. Zadania, które im
się tu powierza, są z definicji wykonawcze — brief jest długi i precyzyjny właśnie po to, żeby myślenie
zostało po stronie zlecającego. Model wybiera się jawnie przy uruchomieniu, nie zostawia domyślnego.

**Haiku 4.5 do zadań mechanicznych, Opus 5.5 do zadań z rozstrzygnięciami.** Decyzja autora
z 2026-09-23 (Haiku); Opus 5.5 zamiast Sonneta 5 — 2026-09-24. Haiku dostaje zadania, których
poprawności pilnują build i testy — przeprowadzki, zmiany nazw, poprawki odwołań. Zlecenie wymienia
wprost, co przenieść, a co usunąć; liczby z raportu architekt przelicza sam. Opus dostaje zadania,
w których w obrębie briefu trzeba coś rozstrzygnąć — kontrakty, diagnozy, interfejs.
**Wyzwalacz powrotu:** pierwsze dwa–trzy zlecenia na Opusie, zmierzone tak samo (kroki, czas, odczyt),
nie robią wyraźnie mniej kroków na podobnej pracy niż Sonnet — albo wychodzi nowy Sonnet; wtedy
porównanie od nowa.

**Sesja architektoniczna czyta dokumenty, nie źródła.** Decyzja autora z 2026-09-22. Okno kontekstu
asystenta prowadzącego sesję jest jej najcenniejszym zasobem, więc asystent projektuje i przegląda,
a kod pisze i czyta subagent w wąsko zakrojonym zadaniu — wynik asystent weryfikuje, zanim go przyjmie.
Konkret z kodu, potrzebny do decyzji, przynosi `code-state.md` albo subagent. **Deleguj kod, nie
decyzje:** dokumenty tego repozytorium niosą decyzje, więc pisze je asystent sam.

**Odsyłacz do sekcji czyta się jako sekcję, nie jako plik.** Decyzja autora z 2026-09-25. Gdy dokument
albo brief wskazuje sekcję po nazwie, najpierw znajduje się jej nagłówek, potem czyta od niego do
następnego nagłówka tego samego poziomu — nie plik od początku. Całość czyta się tylko wtedy, gdy
wskazano cały dokument.

### Obieg jednego etapu

Ustalony z autorem 2026-09-22.

```
 AUTOR                  ARCHITEKT                       SUBAGENT (Opus 5.5)
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
testy każą. **Przy zleceniu z widokiem** (autor, 2026-09-28) architekt przeszukuje diff pod kątem
elementów własnych — `Border` z zaokrągleniem albo tłem, zasoby wymiarów zdefiniowane w samym widoku —
i sprawdza, że każdy trafiony stoi w raporcie wśród elementów własnych z powodem. Drugi subagent, porównujący wynik z briefem i z pięcioma zakazami, idzie wyłącznie do etapów
dotykających zapisu stanu albo granicy automatyzacji. Decyzja autora z 2026-09-22: przy pozostałych
kosztuje tyle co sama implementacja, a architekt sprawdza zakazy celowanym przeszukaniem diffu —
zapisy wywoływane przez zdarzenia, pola czasu, wybór celów.

**Architekt po weryfikacji scala, nie buduje.** Decyzja autora z 2026-09-25. Build i liczbę testów
przynosi raport wykonawcy; architekt nie powtarza ich na `master` po scaleniu, bo autor i tak buduje
aplikację, zanim ją obejrzy.

**Wygląd interfejsu sprawdza autor w aplikacji, nie testy.** Decyzja autora z 2026-09-24. Zlecenie
zmieniające wygląd kończy się zielonym buildem i istniejącymi testami. Nowych testów renderujących
wykonawca nie pisze, chyba że brief każe wprost — a brief każe dopiero przy błędzie widocznym, który
autor zobaczył **drugi raz**; wtedy przyczyna jest znana i test jest tani. Uwagi autora po obejrzeniu
idą następnym krótkim zleceniem. **Wyjątek — test budujący widok** (autor, 2026-09-28): zlecenie, które
tworzy albo przepisuje widok, kończy się testem stawiającym ten widok w oknie bez ekranu i przechodzącym
przez jego stany (wybór, stan pusty) — bez asercji o wyglądzie. Widok, który się nie otwiera, nie jest
sprawą wyglądu, a tylko okno go wyłapie. Logika bez okna — bez zmian: poprawka z testem, który przed nią nie
przechodzi.

**Nowe testy tylko tam, gdzie błędu nie widać w aplikacji albo niszczyłby dane.** Decyzja autora
z 2026-09-24. Testy granic i testy formatu na wzorcowych paczkach i kampaniach — zawsze. Nowe testy
logiki — przy zapisie kampanii, drodze zmiany stanu, wczytywaniu paczek i poprawce zgłoszonego błędu
logiki; nie przy każdej zmianie. Istniejące testy logiki zostają: ich trzymanie nic nie kosztuje,
dopóki nie zmienia się zachowanie.

**Autor uruchamia wyłącznie `master`.** Decyzja autora z 2026-09-22: pozostałe gałęzie są robocze
i nie podaje mu się poleceń uruchamiających aplikację z kopii subagenta. Wynik zweryfikowany przez
architekta trafia więc do `master` **przed** sprawdzeniem przez autora, a to, co w działającej
aplikacji okaże się złe, naprawia następny commit albo cofnięcie — zgodnie z regułą „koniec kawałka
pracy = commit".

**Etap, który zmienia start albo nawigację, zanim uzna się go za zamknięty, uruchamia autor** — na
`master`, po scaleniu. Testy nie otwierają okna.

**Commit i scalenie robi architekt**, po weryfikacji — subagent zostawia wynik w swojej kopii.
Dokumenty dogania architekt na końcu etapu: `code-state.md` (stan kodu), `tasks.md` (co dalej).

### Dokument zadania

Ustalone z autorem 2026-09-25. Duże, wieloetapowe zadanie — kilkanaście zleceń, kilka sesji, kilka
dni — dostaje własny dokument w `docs/zadania/`. Niesie wszystko, czego architekt potrzebuje, żeby
prowadzić to zadanie dalej w nowej sesji: gdzie stoimy, plan, ustalenia, notki. **Stan zadania żyje
w dokumencie, nie w rozmowie** — sesję da się porzucić po każdym zamkniętym wycinku i zacząć nową
bez strat.

**Dwa rodzaje sesji — rozpoznaje się je po pierwszym poleceniu autora.**

* **Sesja główna** — autor zaczyna bez wskazania dokumentu zadania. Architekt czyta jak dotąd,
  prowadzi kierunek i kolejkę; **zakłada** dokument zadania, gdy zadanie ruszy, i **zamyka** go.
* **Sesja zadania** — autor wskazuje dokument zadania. Architekt czyta `CLAUDE.md`, ten dokument,
  dokument zadania i to, co ten każe w *Do przeczytania* — nie całą architekturę, rejestr ani
  kolejkę. Sprawdza, co przybyło w gicie od commita w nagłówku i jakie kopie robocze istnieją, po czym
  w kilku zdaniach zgłasza, gdzie stoimy i co proponuje. Każde pytanie, które musi zadać, bo dokument
  go nie uprzedził, jest brakiem w dokumencie — uzupełnia go od razu.

**Kiedy aktualizować** — przy zdarzeniu, nie co kilka tur:

* **zamknięcie wycinka** — w commicie, który dogania dokumenty po scaleniu; raport z wycinka kończy
  się linią „Dokument zadania aktualny — można zamknąć sesję";
* **decyzje i uwagi autora** — także uwagi po obejrzeniu wyniku — gdy rozmowa o nich się domknie,
  najpóźniej razem z briefem, który z nich wynika. Nie po każdej turze: rozmowa w toku i tak ma ciąg
  dalszy (autor, 2026-09-25);
* **uruchomienie wykonawcy** — w *Gdzie stoimy*: co robi, jego gałąź i kopia.

**Każdy commit, który zmienia dokument zadania, przesuwa commit w jego nagłówku** na ostatni, który
stan uwzględnia (autor, 2026-09-25). Inaczej nowa sesja przegląda w `git log` zmiany, które dokument
już opisuje.

**Sesja z pracującym wykonawcą nie nadaje się do porzucenia** — wykonawca żyje w sesji architekta
i ginie razem z nią, bez raportu. Gdy autor chce ją zamknąć w takiej chwili, architekt najpierw mówi,
co przepadnie. Porzucenie zawsze kosztuje też ciepłą pamięć ostatniego wykonawcy: poprawkę po nowej
sesji robi nowy wykonawca.

**Granica.** Architekt zadania zmienia swobodnie dokument zadania, `code-state.md` i odnośnik
w `tasks.md`; ustalenie ogólnoprojektowe, które wyszło w zadaniu i które autor zatwierdził, wpisuje
do `architecture.md` i `decisions.md` jak dotąd. Co wykracza poza zadanie — nowy kierunek, cokolwiek
dotykające pięciu zakazów, cudzy obszar, kolejność kolejki — idzie do *Do sesji głównej*, chyba że
autor rozstrzygnie to na miejscu.

**Jeden dom.** Na czas życia dokumentu zadania jego zadanie w `tasks.md` to jedna linia z odnośnikiem.
*Poprawki czekające na obszar* zostają w `tasks.md` — należą do obszaru kodu, nie do zadania.
Wykonawca dokumentu zadania nie czyta: co mu potrzebne, architekt przepisuje do briefu.

**Założenie** — w sesji głównej, gdy zadanie ma ruszyć, jednym commitem:

1. Plik `docs/zadania/<zadanie>.md` — nazwa po polsku, małymi literami, słowa łączone myślnikiem,
   nazywa zadanie, nie etap ani datę (`fundament-interfejsu`, nie `krok-10-etap-3`).
2. Treść zadania **przenosi się** z `tasks.md`, nie kopiuje: plan, ustalenia, pomiary i uwagi idą do
   dokumentu, w kolejce zostaje jedna linia z odnośnikiem. Praca już domknięta zwija się w planie do
   jednej linii; to, co jest w historii gita i nikomu dalej niepotrzebne, nie przechodzi wcale.
3. Odsyłacze do przeniesionej sekcji kolejki — w pozostałych dokumentach i definicjach subagentów —
   przepina się na dokument zadania (wyszukać nazwę sekcji we wszystkich dokumentach).
4. *Do przeczytania* dobiera architekt sesji głównej, bo zna całość: sekcje, bez których nie da się
   napisać poprawnego briefu w tym zadaniu, na start; resztę — na „tylko gdy potrzeba".
5. *Gdzie stoimy* opisuje stan w chwili założenia, łącznie z tym, co pokażą `git worktree list`
   i niezatwierdzone zmiany; nagłówek dostaje commit, na którym ten stan zapisano.
6. Sprawdzian przed commitem: czy nowa sesja z samym tym plikiem i jego lekturami doszłaby do
   propozycji następnego wycinka bez pytania autora o kontekst.

**Zamknięcie.** Po spełnieniu kryterium końca architekt przenosi to, co przeżywa zadanie — według
*Przy zamknięciu* — i usuwa plik w tym samym commicie. Historia zostaje w gicie; katalog nie jest
archiwum.

**Struktura — te sekcje, w tej kolejności.** Sekcja bez treści zostaje z kreską, żeby było widać, że
jest pusta, a nie zapomniana.

1. **Nagłówek** — nazwa, cel w dwóch–trzech zdaniach, **„Stan na: <data>, po `<commit>`"** — ostatni
   commit, który stan uwzględnia — i polecenie sprawdzenia `git log <commit>..master` oraz `git
   worktree list` na starcie.
2. **Gdzie stoimy** — jeden akapit, **przepisywany, nie dopisywany**: co ostatnio zamknięte, co
   w toku (wykonawca, gałąź, kopia), następny krok. Jedyne miejsce, które mówi, co teraz.
3. **Zakres i koniec** — co wchodzi, co nie wchodzi; kryterium zakończenia, po którym dokument umiera.
4. **Do przeczytania** — sekcje innych dokumentów **po nazwie**, każda z tym, po co i kiedy: na
   starcie, przed briefem, tylko gdy potrzeba. Na końcu — czego na starcie nie czytać.
5. **Plan** — wycinki (zlecenie albo porcja) jako lista pól wyboru, w kolejności. Zamknięty zwija się
   do jednej linii; to, co potrzebne później (pomiar, rundy), idzie do tabeli pod listą.
6. **Ustalenia** — co przesądzone dla tego zadania: z datą i kto (autor / architekt). Wiąże briefy.
   Ustalenie ogólnoprojektowe ma dom w `architecture.md` — tu tylko odsyłacz.
7. **Notki** — pułapki, wskazówki do briefów, obserwacje z raportów, rzeczy do sprawdzenia. Dopisuje
   się; notkę, która przestała być prawdziwa, poprawia się albo skreśla — nie prostuje późniejszą.
8. **Do sesji głównej** — sprawy poza zakresem, czekające na sesję główną albo decyzję autora.
9. **Przy zamknięciu** — co i dokąd przenieść, gdy zadanie się skończy; dopisywane w trakcie, gdy
   się pojawia.

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
Każda rzecz podaje **gdzie** (zakładka aplikacji albo sekcja galerii i co kliknąć), **jak było**,
**jak jest teraz** i **na co patrzeć** — jednym zdaniem każde. Raport zbiorczy z kilku przebiegów
(autor ogląda dopiero na końcu) ma tę samą listę, dłuższą, pogrupowaną po miejscu w aplikacji,
a pod nią osobno rzeczy rozstrzygnięte samodzielnie.

Gęstość referencji nie jest dowodem rzetelności — raport ma się czytać bez zaglądania do drugiej
warstwy.

---

## 3. Jak pisać dokumenty tego repozytorium

1. **`CLAUDE.md` nazywa własności, nie dzisiejsze mechanizmy.** „Punkt rozszerzenia deklaruje się
   jawnie" — tak. „Manifest i `Requires`" — nie. Nazwy mechanizmów żyją w `architecture.md`. Test
   przed wpisaniem czegokolwiek: czy to zdanie przetrwa przeprojektowanie tej części systemu?
   Jeśli nie, idzie do dokumentu zmienianego świadomie.

2. **Nie wpisuj reguły, której nie da się dziś wykonać.** Subagent, który raz odbije się od
   niemożliwego wymogu, przestaje traktować całą listę poważnie.

3. **Odsyłacze do sekcji po nazwie, nie po numerze.** Przenumerowanie zrywa linki cicho — nic tego
   nie zgłasza.

4. **Każdy fakt ma jeden dom — nie streszczaj cudzego.** Zanim wpiszesz uzasadnienie, sprawdź, czy
   nie stoi już tam, gdzie należy: „co obowiązuje" w `architecture.md`, „dlaczego" — za przyjętym,
   przeciw odrzuconemu, co było wcześniej — w `decisions.md`, „jak jest dziś" w `code-state.md`, „co
   dalej" w `tasks.md`. Odeślij po nazwie sekcji, zamiast powtórzyć. Tabela własności jest
   w `README.md`.

5. **Deklaracja osobno, uzasadnienie osobno.** Decyzja autora z 2026-09-22. `architecture.md`
   zawiera deklaracje i ich konsekwencje, w czasie teraźniejszym. Argumenty, odrzucone warianty
   i historia („wcześniej…", „do dnia…", „obowiązywało…") idą do `decisions.md`, do sekcji o tej
   samej nazwie co sekcja architektury; w architekturze zostaje odsyłacz. Sprawdzian na każdym
   zdaniu: konsekwencja („poprawka w paczce dociera do istniejących kampanii") zostaje, argument
   („bo zmianę wpisu traktujemy jak patch balansujący grę") idzie do rejestru. **Ten dokument
   podlega tej samej regule** (2026-09-25): reguła tutaj, jej incydent i pomiar — w `decisions.md`,
   *Część C — obieg pracy*.

6. **Stan kodu niesie sądy, nie opis — i musi na siebie zarabiać.** Decyzja autora z 2026-09-23:
   dokument, który na siebie nie zarabia, zmienia istotę albo znika. `code-state.md` trzyma cztery
   rzeczy: ocenę stanu, luki w testach, pułapki i punkty rozszerzeń. Każda pozycja ma zmieniać
   decyzję architekta albo treść briefu. Po etapie usuwa się pozycje rozwiązane i dopisuje nowe sądy;
   dogonienie dłuższe niż kilka zdań znaczy, że dokument wrócił do opisywania. Nawigację po kodzie
   niesie **brief**, nie ten dokument.

---

## 4. Briefy dla subagentów

**Subagentów uruchamia się jako jeden z dwóch własnych rodzajów** z `.claude/agents/` — decyzja
autora z 2026-09-23: `wykonawca` do zleceń zmieniających kod, `zwiadowca` do zwiadu i diagnozy tylko
do odczytu. Ich definicje niosą zakazy niżej, krok zrównania kopii z `master` i stały format
raportu, więc brief już ich nie powtarza — mówi tylko, co zrobić, od czego zacząć i co jest
baseline'em. Model domyślny obu to Opus 5.5; przy zadaniu mechanicznym zlecenie podaje Haiku (reguła
wyboru modelu — sekcja 1). Zmiana zakazu albo formatu raportu idzie do definicji i tutaj naraz:
definicja jest wersją wykonawczą, ten dokument — uzasadnieniem.

**Diagnoza przechodzi w poprawkę przez raport, nie przez wznowienie.** Decyzja autora
z 2026-09-23. Domyślnie diagnozuje `zwiadowca`, a poprawkę robi nowy `wykonawca`, którego brief
niesie gotową diagnozę — raport jest zarazem miejscem, w którym architekt i autor widzą przyczynę,
zanim cokolwiek się zmieni. Wyjątek: gdy z góry wiadomo, że poprawka będzie drobna i zależna od
niuansów trudnych do zapisania w briefie, albo gdy autor prosi o tego samego agenta — wtedy
diagnozę dostaje `wykonawca` z poleceniem „najpierw tylko diagnoza, bez zmian", a po raporcie
kontynuuje poprawkę; jego kopia zostaje (sekcja *Środowisko*).

**Poprawka zaraz po obejrzeniu przez autora idzie do tego samego wykonawcy, jeśli jego pamięć jest
jeszcze ciepła, a poprawka mała albo średnia.** Ustalone z autorem 2026-09-24, z pomiaru. Architekt
odtwarza wtedy usuniętą kopię w tym samym miejscu (`git worktree add -b <gałąź> <ścieżka kopii>
master`) i przekazuje uwagę przez SendMessage. **Nowy wykonawca** dostaje nową porcję, rundę
zmieniającą zakres albo poprawkę po dłuższej przerwie: pamięć podręczna wykonawcy wygasa po
5 minutach, a wznowiony po wygaśnięciu zapisuje cały swój kontekst od nowa — drożej niż nowy
wykonawca z krótkim briefem.

Zakazy, które niosą definicje — wszystkie pochodzą z incydentów:

1. **Nie zabijaj procesów** (`Stop-Process`, `taskkill`). Poprawne zachowanie przy blokadzie to
   zgłosić ją, nie sprzątnąć cudzy proces — patrz „Środowisko".
2. **Nie przeszukuj `bin/`, `obj/` ani niczego poza repozytorium** — pakietów NuGet, źródeł
   bibliotek, reszty dysku. Katalogi wyjściowe zawierają kopie i pochodne, więc odpowiedź jest
   i zaszumiona, i kosztowna. Gdy biblioteka zachowuje się inaczej, niż zakłada brief: najwyżej dwie
   próby, potem pomiar do raportu i dalej z resztą zlecenia. Luka, która pcha wykonawcę poza
   repozytorium albo w serię prób, jest luką briefu.
3. **Nie tłum ostrzeżeń** (`#pragma`, `<NoWarn>`, `SuppressMessage`). `TreatWarningsAsErrors` jest
   włączone celowo — kompilator jest tu walidatorem treści. Ostrzeżenie się naprawia u źródła albo
   zgłasza, nigdy nie wycisza.
4. **Po 20 minutach pracy nic nowego.** Decyzja autora z 2026-09-24. Wykonawca dokańcza bieżącą
   zmianę, zatwierdza to, co ma (co nie przechodzi buildu — commitem „WIP:”), i zgłasza, na czym
   stanął. Commit po każdej zamkniętej części briefu, nie na końcu. Koszt kroku rośnie z długością
   pracy, bo każdy krok czyta od nowa całą dotychczasową rozmowę. Czas, a nie kroki, bo zegar
   wykonawca sprawdzi, a własnych kroków wiarygodnie nie policzy.

**Subagent implementacyjny pracuje w osobnej kopii repozytorium** (worktree), a do głównej gałęzi
trafia wynik zweryfikowany. Decyzja z 2026-09-22. Nie potknie się wtedy o niezatwierdzone zmiany
autora w drzewie roboczym ani o blokadę katalogu wynikowego przez podgląd Ridera (sekcja
*Środowisko*). **Kopia startuje jednak ze zdalnej gałęzi, nie z lokalnej** — brief każe ją najpierw
zrównać z lokalnym `master`; szczegół i powód w tej samej sekcji.

Co jeszcze się sprawdziło:

* **Podawaj baseline liczby testów.** Aktualna liczba stoi w nagłówku `tasks.md`. Bez niej subagent
  nie wie, czy spadek jest regresją — a niezgodna liczba zdradza kopię na starym kodzie.
* **Każ osobno wypisać rzeczy rozstrzygnięte samodzielnie.** Tak wychodzą błędy w briefach.
* **Podawaj warunek zatrzymania jako informację, nie jako przeszkodę.** Gdy brief mówi „jeśli
  referencja okaże się żywa, zatrzymaj się i zgłoś", dopisz, że to właśnie jest wynik, po który
  wysyłasz zadanie. Inaczej subagent traktuje zatrzymanie jako porażkę i próbuje obejść.
* **Ogranicz długość raportu**, gdy budżet jest niski.
* **Podawaj w briefie punkty wejścia** — konkretne pliki i typy, od których zacząć, ze stanu kodu,
  z historii gita albo ze zwiadu; wzorem jest akapit „Stan dziś" z nazwami miejsc do zmiany. Gdy
  zadanie pasuje do wzorca z sekcji „Punkty rozszerzeń" w `code-state.md`, wskaż tę sekcję z nazwy.
  Szukanie, którego brief nie oszczędził, jest kosztem briefu, nie wykonawcy.
* **Poprawka błędu przychodzi z testem, który na starym kodzie nie przechodzi** — w logice zawsze,
  przy wyglądzie tylko wtedy, gdy brief każe (*Wygląd interfejsu sprawdza autor*, sekcja 1). Brief
  każe to sprawdzić i podać w raporcie. Asercje opisują zamierzony kształt równościami („wypełnia
  pasek”), nie ograniczeniem („nie wystaje”) — test ograniczenia potrafi przejść przy błędzie
  widocznym na ekranie.
* **Zadanie „znajdź wszystkie X, które…” przychodzi z poleceniem, które daje tę listę.** Decyzja autora
  z 2026-09-26. Brief podaje wykonawcy gotowe polecenie wyszukiwania (albo każe je napisać i wkleić do
  raportu), a architekt przy weryfikacji uruchamia je jeszcze raz na gałęzi wykonawcy. Zdanie „reszta
  jest w użyciu” w raporcie nie jest dowodem kompletności — polecenie jest.
* **Brief zwiadu pyta tylko o to, czego dokumenty nie mówią.** Decyzja autora z 2026-09-26. Przed
  briefem każde pytanie sprawdza się w dokumentach wskazanych dla etapu. To, co mówią, brief podaje
  jako założenie do potwierdzenia jednym zdaniem, nie jako pytanie do zbadania.
* **Brief z widokiem wymienia z nazwy klocek fundamentu dla każdego elementu, który go ma** (autor,
  2026-09-28) — „składaj z klocków” nie wystarcza, a to, który klocek niesie które znaczenie (odznaka
  wartości czy tag słowa), stoi w komentarzach motywu, których wykonawca nie czyta. Wykonawca wypisuje
  w raporcie elementy własne — złożone z prostych elementów zamiast klocka — z powodem.
* **Gdy brief każe napisać test renderujący, test mierzy położenie względem sąsiadów i widoczność,
  nie tylko wymiar** — i próbkuje piksele wyrenderowanego obrazu tam, gdzie coś ma być widać.

Briefy w tym repo są długie i precyzyjne, i właśnie dlatego łatwo w nich pominąć zakaz, który
wydaje się oczywisty — stąd zakazy w definicjach, nie w briefach.

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

### Rama a system — jego własne uzasadnienia

* **„DungeonApp nie jest jeszcze grą — jest launcherem i ramą technologiczną. Dopiero system jest
  grą i to on powinien posiadać kampanie, wpisy, paczki."** Stąd paczki i kampanie w katalogu swojego
  systemu (2026-09-24). Doprecyzowanie przyjęte przez autora tego samego dnia: rama to raczej
  **silnik** niż launcher — **system jest właścicielem danych, rama jest właścicielem zasad
  obchodzenia się z nimi.** Tworzenie, otwieranie i zamykanie kampanii, jedyna droga zmiany stanu,
  zapis odporny na przerwanie i pięć zakazów zostają w ramie. „System posiada kampanie" nie znaczy,
  że system sam je zapisuje — pierwszy, który by to robił, obszedłby gwarancje ramy.

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
a po niej zwykle przychodzi poprawka — i wznowienie tego samego wykonawcy wymaga istniejącej kopii;
nowy wykonawca zaczyna od zera.

**Kopia robocza subagenta startuje z `origin/master`, nie z lokalnego `master`.** Autor nie wypycha
na bieżąco, więc zdalna gałąź bywa daleko w tyle. **Brief implementacyjny zaczyna się od `git reset
--hard master` w kopii subagenta** i każe podać w raporcie commit, od którego liczony jest diff. Gdy
system uprawnień zablokuje reset, działa `git switch -c <gałąź> master` — brief podaje oba, drugi jako
zapasowy. Kopie robocze leżą w `.claude/worktrees/`, wykluczonym w `.gitignore`; po przeniesieniu
wyniku do `master` asystent usuwa kopię i gałąź subagenta.

**Zadania redakcyjne na długich dokumentach architekt robi sam; subagentom zostaje kod.** Obserwacja,
nie reguła o przyczynie: subagenci na zadaniu przeniesienia tekstu między dokumentami stawali bez
postępu, a architekt, który ma stary tekst w kontekście, robi to w kilka minut.
