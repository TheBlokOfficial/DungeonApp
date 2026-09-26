# Zadanie: fundament interfejsu

Każda kontrolka, której aplikacja używa albo będzie używać, dostaje w motywie ramy własny, kompletny
szablon zamiast domyślnego — raz, w jednym miejscu — i jest pokazana w galerii kontrolek we wszystkich
stanach. Potem widoki składa się z gotowych klocków, a nie poprawia kontrolka po kontrolce.

**Stan na: 2026-09-26, po `b57f232`.** Na starcie sesji: `git log b57f232..master` i `git worktree
list` — wszystko, co tam jest, a czego ten dokument nie wymienia, zdarzyło się poza nim.

*Dokument zadania — co to jest, jak go prowadzić i kiedy umiera: [collaboration.md](../collaboration.md),
*Dokument zadania*.*

---

## Gdzie stoimy

Porcje 0–8b obejrzane; rundy 1 i 2 porcji 8b scalone 2026-09-26 — runda 2 (pasek nakładką, kosz
szary → czerwony, wyzwanie bez zakreślenia) czeka na obejrzenie. Kopii roboczych brak. **Następny
krok:** uwagi autora po rundzie 2; po przyjęciu — porcja 9.

## Zakres i koniec

**Wchodzi — pełny zestaw standardowy** (autor: fundament ma objąć „99% wszystkich potencjalnych
elementów interaktywnych"): przyciski we wszystkich odmianach; pola tekstowe, wyszukiwania,
wielowierszowe, liczbowe; pole wyboru, przycisk opcji, przełącznik, suwak; lista rozwijana
pojedyncza, wielokrotna, z wyszukiwaniem; zakładki, przełącznik segmentowy; wiersz listy; menu
i menu kontekstowe; podpowiedź, okienko wysuwane, okno potwierdzenia, powiadomienie; wskaźnik
postępu; tag, chip, odznaka; kafelek, sekcja rozwijana, separator, okruszki, pusta lista; tabela;
pasek przewijania; typografia; skala odstępów.

**Nie wchodzi:** kalendarz i wybór koloru — bez zastosowania przy stole, a kalendarz zaprasza do pól
niosących czas (drugi zakaz). Układ (stosy, siatki, wyrównanie) — bez szablonów, tylko odstępy ze
skali. Z założenia oryginalne, poza motywem: karty treści (projektowane per typ), biurko z oknami,
pasek boczny, ekrany jednorazowe. Zlecenia A i B z kolejki (lista i filtry, karta potwora) — to już
składanie z fundamentu, osobny etap.

**Koniec:** porcje 6–11 (z 8b i 9) przyjęte przez autora. Wtedy ten dokument umiera, a kolejka
przechodzi do zleceń A i B i punktu kontrolnego (`tasks.md`).

## Do przeczytania

Na starcie:
* [architecture.md](../architecture.md), *Niezmiennik interfejsu* — cała sekcja. To reguły, którym
  podlega każda nowa kontrolka: znaczenie kolorów, stany, kursor, pismo, ruch, okienka.
* [code-state.md](../code-state.md), *Pułapki* — większość dotyczy motywu ramy (wysokość przycisku,
  motywy budowane leniwie, `hover-suppressed`, `ReducedMotion.axaml`, przewijanie łańcuchowe).
  Wchodzą do briefu jako ostrzeżenia.

Przed każdym briefem:
* [tasks.md](../tasks.md), *Poprawki czekające na obszar* — zlecenie, które wchodzi w obszar,
  zabiera stamtąd swoje poprawki.
* `.claude/agents/wykonawca.md`, *Rzemiosło interfejsu* — żeby brief jej nie powtarzał.

Tylko gdy potrzeba:
* [decisions.md](../decisions.md), *Niezmiennik interfejsu* — zanim zaproponujesz zmianę konwencji
  albo gdy autor ją kwestionuje (np. otoczka na suwaku). Długie; tam są argumenty za każdą regułą.
* [decisions.md](../decisions.md), *Pytania otwarte i reguła „nic bez konsumenta"* — wyjątek
  fundamentu, gdyby ktoś pytał, czemu kontrolki powstają przed widokiem.
* Mockup `docs/images/mockup_rejestr.html` — brief przytacza selektory, nie każe czytać pliku.

**Na starcie nie czytaj** całej architektury, rejestru decyzji ani reszty kolejki.

## Plan

Porcja = jeden wykonawca w 20 minutach. Autor sprawdza każdą w galerii **i w aplikacji**, bo porcja
od razu zdejmuje stare poprawki nałożone na swoje kontrolki w całej aplikacji.

- [x] **0–7a** — przyjęte; rundy i pomiar w tabeli niżej.
- [x] **7b** — okno potwierdzenia, powiadomienie, wskaźnik
  postępu (kolor wypełnienia z właściwości kontrolki, domyślnie akcent — podaje go układający widok,
  np. pasek PZ; tak jak w suwaku z porcji 4).
- [x] **8** — tag, chip, odznaka (odmiany po znaczeniu: neutralna, wyróżniona akcentem, stany;
  kolor podany przez system dla jego skal; wnętrze krojem liczb, wymiary z motywu), tabela
  (nagłówek, kolumny z wyrównaniem, wcięcia komórek, obramowanie, wyróżnienie pojedynczej komórki
  kolorem podanym przez układającego — pierwszy konsument: cechy potwora, `tasks.md`, *B*).
- [x] **8b** — poprawki po porcji 8; kompozycje przykładowe w galerii (pomysł autora): lista
  z filtrami i wyszukiwaniem, karta z tabelą i odznakami, okno potwierdzenia nad listą.
- [ ] **9** — odcięcie motywu domyślnego biblioteki; usunięcie tokenów bez użycia (lista w raporcie
  0b: m.in. `DungeonSuccessBrush`, `DungeonPaddingXl`, `DungeonNavigationRowHeight`).
- [ ] **10** — klocek: wiersz listy z kreską zaznaczenia. Obejmuje wiersze zrobione dziś z `Button`
  (`content-row-button`, lista kampanii, pasek boczny) — razem ze zleceniem A.
- [ ] **11** — klocek: chip z listą wyboru — chip dostaje najechanie i stan otwarcia (dziś lokalny
  styl chipa przykrywa najechanie z motywu, więc otwarty chip wygląda jak w spoczynku; po rundzie 1
  porcji 5); strzałka jak w liście rozwijanej.

**Pomiar porcji** — materiał do punktu kontrolnego. „Rundy" to poprawki od autora, „pomiar" — kroki
/ minuty / odczyt z `tools/subagent-usage.py`, suma przebiegów porcji.

| # | Porcja | Rundy | Pomiar |
|---|---|---|---|
| 0 | galeria (zakładka), skala odstępów, wymiary z mockupu, typografia z krojem liczb | 1 (obcięte ogonki, grubości kroju nagłówków, siedem stopni pisma); przyjęta | 33 / 5,8 / 3,1 mln + runda 1: 22 / 4,0 / 1,5 mln |
| 0b | powierzchnie i linie: tło, karta, panel, sekcja z obramowaniem; linia pozioma i pionowa, w liście, między sekcjami | 1 (architekt: kontrast przygaszonego tekstu i czerwieni, krój nagłówków od 20; autor: domyślny kolor tekstu); przyjęta — dwa kolory (drugorzędny, ostrzeżenie) przechodzą do porcji 1 | 33 / 6,3 / 2,7 mln + runda 1: 47 / 7,2 / 4,4 mln |
| 1 | przyciski — sześć odmian; wysokość kontrolki według standardu okienkowego (dziś 38 z makiety) → 32, po rundzie 1 → 36 (autor: 32 zbyt ściśnięte); `frame-action` zostaje elementem ramy (pełna wysokość paska), nie przyciskiem (architekt) | 1 (autor: ikony niewidoczne na kolorowych przyciskach, wciśnięcie bez własnego koloru, wyłączony główny szary jak przed porcją, wysokość 36; architekt: przycięcie w galerii, odnośnik w zdaniu, grubość napisów); 2 (autor: ikony konturowe, odnośnik w zdaniu nad linią, najechanie akcentu odbarwia, bez wciśnięcia); 3 (autor: najechanie z wypełnieniem = kolor bez zmian + otoczka 3 px, odnośniki samodzielne szare, bez ręki nigdzie — `decisions.md`; poprawka: przycisk przycinał otoczkę); przyjęta | 52 / 8,4 / 6,1 mln + runda 1: 60 / 8,6 / 6,5 mln + runda 2: 50 / 7,9 / 5,3 mln + runda 3: 30 / 4,9 / 2,4 mln + poprawka otoczki (ten sam wykonawca, wznowiony): 14 / 2,2 / 1,7 mln |
| 2 | pola tekstowe — zwykłe, wyszukiwania, wielowierszowe, liczbowe; kolor zaznaczenia; tekst do zaznaczenia; zakreślenie (styl tekstu na fragmencie, odmiany po znaczeniu: wyróżnione, trafienie wyszukiwania); pole w trakcie pisania ma wyraźną krawędź — to stan edycji, nie wskaźnik fokusu klawiatury (architekt; po rundzie 1 — krawędź z najechania, bez akcentu); znaczenia pędzli `Input*`; fokus zdejmowany w ramie raz dla całej aplikacji | scalona 2026-09-25; poprawka architekta: aplikacja padała po wejściu w system (selektor potomka w motywie pola), test budujący wszystkie motywy; 1 (autor: krawędź edycji i zaznaczenie w akcencie krzykliwe — zaznaczenie niebieskie; ikona, × i strzałki poza obszarem tekstu, bez tła pod myszą; architekt: liczby całkowite z przecinkiem, tekst pod paskiem przewijania, grubość tekstu w polu, wyrównanie galerii); przyjęta 2026-09-25 — drobne uwagi w *Poprawkach czekających na obszar* | 52 / 10,5 / 7,0 mln + runda 1: 39 / 9,1 / 4,1 mln |
| 3 | lista, wiersz listy, pusta lista, pasek przewijania (ustalenia architekta do briefu — w historii `tasks.md` do `7b52a80`); przygaszony akcent naprawiony (16 %, z kanałem alfa) | scalona 2026-09-25, obejrzana — uwagi autora (przewijanie przechodzi wyżej, najechanie na wybranym) poprawione w porcji 5; wiersz ma wysokość najmniejszą 32, nie stałą, bo panel instancji kampanii ma wiersze z polem liczbowym — panel testowy, do usunięcia (autor), więc bez poprawek | 55 / 9,4 / 7,4 mln |
| 4 | pole wyboru, przycisk opcji, przełącznik, suwak — to, co wypełnione akcentem, pod myszą się nie zmienia; wyłączone zaznaczone traci akcent; tor przełącznika 36×18; suwak tylko poziomy | scalona 2026-09-25; przyjęta 2026-09-25 bez rund — pytania autora rozstrzygnięte (`decisions.md`, *Niezmiennik interfejsu*) | 40 / 9,8 / 4,8 mln |
| 5 | okienko wysuwane; lista rozwijana pojedyncza (`ComboBox`), wielokrotna i z wyszukiwaniem (kontrolka ramy `DropDownPicker`); poprawki list z porcji 3 (kółko nie przechodzi wyżej, wybrany bez najechania). Diagnoza „przycisk w wierszu odznacza wiersz": to nie motyw — model widoku panelu instancji przebudowuje wiersze nowymi obiektami, a lista gubi zaznaczenie przy podmianie źródła; panel testowy, bez poprawki (`code-state.md`) | scalona 2026-09-25; 1 (autor: okienko wyśrodkowane, otwierający traci najechanie, mignięcia, strzałka „teleportuje się”, lista otwarta w górę nachodzi na pole; decyzje: strzałka w prawo → w dół, reguła „Ruch” — `architecture.md`; przełącznik z ruchem, wyłączone animacje w systemie); 2 (autor: mignięcie przy zamykaniu kliknięciem — decyzja: otwierający odpoczywa do ponownego wjechania myszą; strzałka wskazuje kierunek otwarcia; okienko wjeżdża ruchem; z kolejki: wariant B wiersza i okienka, uchwyt suwaka) — scalona; przyjęta 2026-09-25 | 55 / 11,4 / 6,4 mln + runda 1: 44 / 8,2 / 4,8 mln + runda 2: 68 / 12,8 / 7,2 mln |
| 6 | zakładki (`TabControl`/`TabStrip`, wspólna podstawa), przełącznik segmentowy (`ListBox` z nazwanym motywem), kafelek (`DungeonTile`, pierwszy konsument — kafelki biurka), sekcja rozwijana, okruszki (kontrolka ramy, przejęta z porzuconej sesji); wyłączone wybrane — tło neutralne „aktywne” (architekt, po raporcie: segment zlewał się z pojemnikiem) | scalona 2026-09-25; przyjęta 2026-09-25 — uwagi (pogrubienie wybranego zmienia szerokość, wyłączony pojemnik, pusty pasek) idą z porcją 7, bez osobnej rundy (autor) | 65 / 12,0 / 8,0 mln |
| 7a | poprawki po porcji 6 (rezerwa pogrubienia — kontrolka `BoldTextReserve`; wyłączony pojemnik przygasza całość raz — przygaszenie po własnym `IsEnabled`, kolory po `:disabled`; pusty pasek bez odstępu); podpowiedź z motywem i pełnym napisem przyciętej etykiety (`TrimmedLabelToolTip`); zakładka najwyżej 240; menu z przycisku, kontekstowe, podmenu, separator; ruch menu w `PopupOpenMotion` | scalona 2026-09-25; obejrzana tego dnia bez rundy — uwagi o menu (skrót nie w linii ze strzałką podmenu, podmenu nachodzi na menu) odłożone do *Poprawek czekających na obszar* (autor: nie na teraz) | 42 / 11,3 / 5,4 mln |
| 7b | przygaszenie wyłączonych raz we wszystkich motywach (po własnym `IsEnabled`); wskaźnik postępu we własnym motywie (nieokreślony rysowany w kodzie); okno potwierdzenia i powiadomienia w warstwie nad oknem (`WindowOverlay`); cały nowy ruch w kodzie, po `SystemMotion.IsReduced` | scalona 2026-09-25; obejrzana tego dnia — uwagi autora (dymki zachodzą na pasek stanu; nieokreślony pasek klatkowany na monitorze 280 Hz) idą z porcją 8, bez rundy | 60 / 11,3 / 6,9 mln |
| 8 | poprawki po 7b (dymki nad paskiem stanu; pasek nieokreślony z `RequestAnimationFrame` zamiast zegara 16 ms); odznaka (`Badge`), tag (`WordTag` — `Tag` zajęte przez Avalonię), odmiany klasami (`.accent`, `.success`, `.warning`, `.danger`, `.custom`); chip (`DungeonChip` na `ToggleButton`); tabela (`Table` po `Grid`, `TableCell`; linie rysują komórki) | scalona 2026-09-25; obejrzana tego dnia — szarpanie paska i dymki na pasku stanu naprawione; uwagi (ramka w galerii myli, tabela cech nie jak projekt, tagi ciasne w pionie) i uwagi architekta idą z porcją 8b, bez rundy | 57 / 11,0 / 6,5 mln |
| 8b | poprawki po 8 (tag i odznaka 22; odznaka neutralna na `DungeonNeutralDim` — biały 8 %; wybrana zakładka i segment tekstem `AccentOnDim`; tabela cech 6 × 3 po 40 × 40 — `AbilityScoresSample`; tabele bez karty); sekcja „Kompozycje”: lista z filtrami, usuwanie z potwierdzeniem i „Cofnij”, karta | scalona 2026-09-25; 1 (autor: pas paska zawsze i przy krawędzi, kosz w wierszu — motyw `DungeonRowAction`, pas pełnej wysokości, czerwony, bez tła i podpowiedzi; architekt: wyzwanie zakreśleniem, pasek wartości w siatce, minus U+2212, jedna warstwa przewijania, menu z pasem przy potrzebie) — scalona 2026-09-26; 2 (autor: pasek nakładką z odstępem od krawędzi, góry i dołu — stały pas zostawiał pusty margines; kosz zawsze widoczny, szary, czerwony pod myszą, wiersz podświetlony nad koszem; wyzwanie bez zakreślenia) — scalona 2026-09-26 | 52 / 8,4 / 5,4 mln + runda 1: 70 / 11,7 / 10,6 mln (z poprawką podwójnych pasów po raporcie, ten sam wykonawca) + runda 2: 40 / 9,1 / 5,8 mln |

## Ustalenia

* **Własny, kompletny szablon zamiast poprawek na domyślnym** — autor, 2026-09-24 („najpierw od tego
  zaczął"). Poprawki nałożone na domyślny motyw przepuszczają niechciane efekty, tekst „prawie" na
  środku i globalne wysokości. W jednym miejscu, raz: stany (spoczynek, najechanie, zaznaczenie,
  wyłączenie — nic więcej), tekst wyśrodkowany w pionie z założenia, wymiary jako zasoby motywu.
* **Profesjonalność, czytelność i intuicyjność przed gustem i wiernością mockupowi** — autor,
  2026-09-24. Mockup jest makietą ekranu rejestru, nie projektem aplikacji; wymiary i zachowanie
  kontrolek z konwencji platformy, mockup jest odniesieniem dla zakładek treści.
* **Galeria kontrolek** — zakładka ramy nad „Ustawieniami", pod nagłówkiem „System" (kategoria
  Aplikacja); każda kontrolka w każdym stanie: zwykła, wyłączona, zaznaczona, długi tekst, pusta.
  Autor sprawdza w niej porcję w minutę. **Zostaje na stałe.**
* **Motyw domyślny biblioteki leży pod spodem do porcji 9.**
* **Element spoza zestawu:** wykonawca buduje go w widoku jawnie jako własny i zgłasza w raporcie;
  przy drugim użyciu przechodzi do motywu osobnym zleceniem.
* **Szablon domyślny Avalonii (MIT, jawny) przejmuje się raz, świadomie** — brief wskazuje, który
  i skąd. To nie jest grzebanie w bibliotekach, którego zabrania definicja wykonawcy.
* **Fokus: dziś żadnego widocznego** — tylko najechanie i zaznaczenie. Autor: aplikacja będzie
  docelowo „keyboard first"; wtedy fokus pokazuje się wyłącznie, gdy użytkownik zaczął używać
  klawiatury. Do zrobienia z obsługą klawiatury, nie w fundamencie.
* **Porcja 6 — rozstrzygnięcia architekta** (2026-09-25; wiążą rundy porcji):
  * *Wybrane = akcent przygaszony w tle + akcent w tekście* — zakładka i segment tak samo jak wybrany
    wiersz, plus pogrubienie z miejscem zarezerwowanym w każdym stanie (autor po obejrzeniu — reguła
    w `architecture.md`, *Niezmiennik interfejsu*, *Stany*); wybrane pod myszą się nie zmienia;
    wyłączone wybrane traci akcent — tło neutralne „aktywne".
  * *Zakładki* jak w mockupie (`.type-tab`): bez krawędzi i tła w spoczynku, wysokość 32, pasek od
    lewej, zawija się; odstęp między zakładkami ze skali (6), nie 3 z mockupu. Ta sama zakładka
    w `TabControl` (z treścią) i w `TabStrip` (sam pasek — filtr).
  * *Przełącznik segmentowy* to `ListBox` z nazwanym motywem: pojemnik jak pole, segmenty równej
    szerokości, wybór zmienia kolor od razu — bez przesuwanej wkładki.
  * *Kafelek* to `Button` z nazwanym motywem, wymiar podaje układający; pierwszy konsument —
    zminimalizowane okna biurka (dziś lokalny styl). Kafelek do zaznaczania — przy pierwszym
    konsumencie.
  * *Sekcja rozwijana*: strzałka po lewej, w prawo → w dół jak w liście rozwijanej, obrót ruchem;
    treść pojawia się od razu (bez animacji wysokości), wcięta do napisu nagłówka; rozwija się tylko
    w dół.
  * *Okruszki* — kontrolka ramy: odcinki nieklikalne (nie ma dokąd nawigować), przy braku miejsca
    skraca się ślad, bieżący odcinek na końcu.
* **Porcja 7a — rozstrzygnięcia architekta** (2026-09-25):
  * *Menu* = menu z przycisku (`MenuFlyout`) i kontekstowe; **paska menu nie ma** — rama nawiguje
    paskiem bocznym, nic by go nie użyło. Powierzchnia jak okienko wysuwane; pozycja jak wiersz listy
    (32, to samo podświetlenie); kolumny: ikona/ptaszek (tylko gdy któraś pozycja ją ma), napis,
    skrót przygaszony, strzałka podmenu. Ptaszek kolorem tekstu, nie akcentem — menu to polecenia,
    nie wybór. Pozycja niszcząca: czerwony napis, podświetlenie neutralne.
  * *Podpowiedź*: pismo 12, najwyżej 320 szerokości i zawija się (nigdy nie przycina), bez ruchu.
    Pełny napis w podpowiedzi tylko przy rzeczywistym przycięciu; jawna podpowiedź widoku wygrywa.
  * *Zakładka* ma najwyższą szerokość 240 (ok. 30 znaków) — dłuższa się przycina z podpowiedzią.
  * *Pogrubienie wybranego* rezerwuje miejsce tylko dla nagłówka tekstowego.
* **Porcja 7b — rozstrzygnięcia architekta** (2026-09-25; zielone światło autora tego dnia):
  1. Najpierw podwójne przygaszenie (`tasks.md`, *Przygaszenie wyłączonych*) we wszystkich motywach.
  2. *Okno potwierdzenia* — kontrolka ramy nad oknem aplikacji: karta na środku, reszta przyciemniona;
     tytuł, jedno–dwa zdania, przyciski w prawym dolnym rogu: „Anuluj” + akcja (główna albo
     `danger`). Escape anuluje; kliknięcie obok nie zamyka; przy akcji niszczącej Enter nie
     potwierdza. Pojawia się wyłonieniem (≤ 150 ms), przy wyłączonych animacjach od razu.
  3. *Powiadomienie* — dymek w prawym dolnym rogu okna, kilka jeden nad drugim; odmiany informacja,
     ostrzeżenie, błąd; informacja i ostrzeżenie znikają po kilku sekundach, błąd zostaje do
     zamknięcia; krzyżyk i najwyżej jeden odnośnik akcji.
  4. *Wskaźnik postępu* — cienki pasek, określony i nieokreślony; kolor wypełnienia z właściwości
     (domyślnie akcent), styl `ProgressBar` z `BuiltInControls.axaml` przechodzi do motywu, pasek
     ładowania przy starcie (`MainWindow.axaml`) na nowy wygląd; nieokreślony przy wyłączonych
     animacjach — spokojny pasek.
  5. Galeria — wszystko w każdym stanie, z przyciskami wywołującymi okno i każde powiadomienie.
  * Doprecyzowane w briefie: czas znikania powiadomienia to stała motywu per odmiana, nie parametr
    wywołania ani pole typu (drugi zakaz — czytany według intencji, ale bez pola nic nie trzeba
    rozstrzygać); mysz nad dymkiem wstrzymuje znikanie; ikona informacji w kolorze tekstu
    drugorzędnego — informacja nie niesie stanu, akcent zostaje dla wybranego i głównego; treść błędu
    do zaznaczenia; nieokreślony wskaźnik przy wyłączonych animacjach — cały tor w przygaszonym
    akcencie.
  * Poza zakresem: okno potwierdzenia nigdzie nie podpięte — usuwanie kampanii idzie do kosza
    systemu (odwracalne); pierwszy konsument przyjdzie z akcją nieodwracalną.
* **Porcja 8 — rozstrzygnięcia architekta** (2026-09-25; propozycja niezapisana, zielone światło
  autora na zakres z planu — rozstrzygnięcia z wiedzy o interfejsie, pokazane autorowi przy starcie):
  * *Odznaka a tag — po tym, czym jest treść:* odznaka = wartość (liczba, „1/2”) — zaokrąglony
    prostokąt, krój liczb; tag = słowo (kategoria, typ, rzadkość) — pigułka, pismo interfejsu. Obie
    wysokości ok. 20, pismo 12, nieklikalne, przycinane z podpowiedzią.
  * *Odmiany obu:* neutralna, akcent, sukces / ostrzeżenie / niebezpieczeństwo (tło — nowe
    przygaszone kolory stanów, 16 %), kolor podany przez układającego (skale systemu).
  * *Chip* = przełącznik filtra (`ToggleButton`), wysokość 28 — zwarta kontrolka w rzędzie filtrów;
    włączony = przygaszony akcent jak `.filter-chip.on`; bez strzałki i listy (porcja 11).
  * *Tabela* — panel ramy z liniami między wierszami, ostre narożniki, opcjonalny nagłówek, linie
    pionowe opcjonalne; komórka z wyrównaniem i tłem wyróżnienia od układającego; nieinteraktywna;
    tekst przycina się, nie zawija. Bez `DataGrid`.
  * Powiadomienia stoją nad paskiem stanu; ruch paska nieokreślonego z pętli renderowania okna.
* **Porcja 8b — propozycja architekta** (2026-09-25; zielone światło autora tego dnia). Doprecyzowane
  w briefie: komórki tabeli cech = skrót, wartość, modyfikator; tło odznaki neutralnej — nowy token
  (biały z małą nieprzezroczystością), to samo dla tagu neutralnego, jeśli ma ten błąd; kontrast
  wybranego sprawdzony też w `ComboBoxItem`, `DropDownPicker`, `MenuItem`; kompozycje na końcu
  galerii, licznik z polską odmianą, filtrowanie zachowuje obiekty wierszy, „Cofnij” przywraca wiersz
  na dawne miejsce, karta bez wyróżnienia modyfikatorów kolorem (kolory skal są tokenami systemu):
  1. *Poprawki po porcji 8:*
     * tag i odznaka wysokości 22 (autor: litery prawie przylegają do krawędzi; 22 zostawia po 5
       w wierszu 32; oba razem, bo stoją obok siebie; 24 zapycha wiersz, pismo 11 odrzucone — tag
       niesie słowo do czytania);
     * odznaka neutralna z półprzezroczystym jasnym tłem zamiast tła wiersza — dziś na wybranym
       wierszu jest ciemną dziurą (architekt);
     * *Kontrast wybranego* z `tasks.md`, *Poprawki czekające na obszar*;
     * tabela cech w galerii według projektu autora: sześć wierszy po trzy **kwadratowe** komórki
       (np. 40 × 40 — wymiary w definicjach wierszy i kolumn, tabela ich nie narzuca), skróty cech
       wersalikami, trzy litery: SIŁ, ZRC, KON, INT, MDR, CHA;
     * tabele w galerii bez karty dookoła — autor brał ją za część tabeli; żadna inna sekcja nie
       wkłada przykładów w ramkę.
  2. *Kompozycje* — nowa sekcja galerii, na danych przykładowych, bez dotykania kampanii:
     * lista z filtrami: pole wyszukiwania, rząd chipów, „Wyczyść filtry” wygaszone bez filtrów,
       licznik wpisów, wiersze z tagiem i odznaką; filtrowanie działa; pusta lista przy braku wyników;
     * karta: nagłówek (kategoria, nazwa, tagi), pasek wartości z odznakami, tabela cech, sekcja
       rozwijana z opisem do zaznaczania;
     * usuwanie z listy: przycisk w wierszu → okno potwierdzenia z akcją niszczącą → wiersz znika,
       powiadomienie z odnośnikiem „Cofnij”.
  * Cel kompozycji: odstępy między klockami, wyrównanie w pionie w jednym rzędzie, kolory obok siebie
    — sprawdzone przed zleceniami A i B, nie w nich.
* **Porcja 8b — runda 1** (2026-09-25; uwagi autora po obejrzeniu + ocena architekta ze zrzutu):
  * *Pasek przewijania rezerwuje miejsce zawsze* (autor): pojawia się i znika, gdy trzeba, ale nie
    oddaje ani nie zabiera miejsca treści — dziś po usunięciu kilku wierszy pasek znika i wiersze
    przeskakują w prawo. Globalnie w motywie `ScrollViewer`, przy widoczności `Auto`. Przy briefie
    wpisać regułę do `architecture.md` (*Niezmiennik interfejsu*) i uzasadnienie do `decisions.md`;
    poprawić rzemiosło w `.claude/agents/wykonawca.md` („Pasek przewijania tylko przy potrzebie”)
    i jego streszczenie w `collaboration.md`, *W interfejsie asystent decyduje…*.
  * *Pasek nie jest wyśrodkowany w swoim pasie* (autor): odstęp od wierszy mniejszy niż od prawej
    krawędzi pojemnika. Rozstrzygnięcie architekta (autor pytał, czy nie przykleić): pasek przy
    prawej krawędzi pojemnika, wiersze sięgają do jego pasa z tym samym odstępem co od lewej krawędzi
    — konwencja okienkowa; pasek należy do pojemnika, nie do treści, i łatwiej go trafić przy krawędzi.
  * *Kosz w wierszu* (autor: biały, bez podświetlenia pod myszą, z niepotrzebną podpowiedzią — tak samo
    w liście kampanii, `Features/CampaignLibrary/CampaignLibraryView.axaml`, własna logika
    `delete-hover`): spoczynek kolorem tekstu drugorzędnego, pod myszą jaśnieje tło i ikona (jak
    `.subtle`), **bez podpowiedzi** — kosz i × są znakami powszechnymi (druga taka decyzja autora, po
    krzyżyku w polu wyszukiwania; wpisać do `architecture.md` jako regułę). Architekt: akcja w wierszu
    widoczna tylko pod myszą i na wybranym wierszu, miejsce zarezerwowane (przezroczystość, nie
    zwijanie). `row-action` z galerii przechodzi do motywu — lista kampanii to drugie użycie.
    Zbadać, czemu `.subtle` w wierszu nie ma najechania, i poprawić u źródła.
    Dopisane po zrzucie listy kampanii (autor): nad i pod koszem jest pas wiersza, więc ruch myszy
    w pionie po koszu przełącza podświetlenie wiersza i kosza; zaokrąglone tło pod koszem wygląda
    obco w wierszu. Rozstrzygnięcie architekta: **obszar trafienia akcji w wierszu = cały pas pełnej
    wysokości wiersza** (kolumna na końcu wiersza), wiersz nie podświetla się, gdy mysz jest w tym
    pasie; **pod myszą jaśnieje sam rysunek, bez tła** — jak ikony w polach (`architecture.md`,
    *Pole do pisania*). **Kolor: kosz czerwony** (autor, 2026-09-25, wbrew rekomendacji szarego):
    czerwień znaczy odtąd także „usuwa” — reguła w `architecture.md`, *Niezmiennik interfejsu*, i komentarz
    przy tokenie niebezpieczeństwa w `Tokens.axaml` do poprawienia w rundzie. Rysunek kolorem tekstu
    niebezpieczeństwa (`DungeonDangerTextBrush`) w każdym stanie, w którym jest widoczny; pod myszą
    jaśniejsza czerwień, bez tła (nowy token, jeśli brak).
  * *Wyzwanie to zakreślenie, nie odznaka* (architekt, po sprawdzeniu `decisions.md`, *Zakreślenie
    a odznaka* — decyzja autora z 2026-09-24): wyzwanie w liście i na karcie → styl `.highlight`
    (`Typography.axaml`). **Ustalenie porcji 8 „odznaka = wartość (liczba, „1/2”)” było z tą decyzją
    sprzeczne** — odznaka niesie licznik przypięty do czegoś (np. liczba wpisów przy filtrze), nie
    wartość z karty. Przykłady w galerii (`LabelsSection`) poprawić, jeśli pokazują wartości.
  * *Pasek wartości karty* (architekt): KP, PW, Szybkość jako tekst krojem liczb, bez odznak;
    pary etykieta–wartość w stałej siatce (dwie w rzędzie) — dziś „Wyzwanie” spada do drugiej linii
    przypadkiem, bo w rzędzie zabrakło miejsca.
  * *Minus w modyfikatorach* — znak „−” (U+2212), nie łącznik.
  * **Pytanie autora:** czy kompozycje mają być ładne, czy tylko pokazywać zestaw. Odpowiedź
    architekta (potwierdzona przez autora 2026-09-26): mają być poprawnie złożone z klocków — odstępy, wyrównanie,
    znaczenie kolorów — bo te same błędy powtórzyłyby się w zleceniach A i B; nie są projektem ekranu
    (ten przyjdzie z A i B). Gdy autor uzna je za brzydkie — dopytać, co konkretnie, i ocenić, czy to
    wada klocka (poprawka w motywie), czy tylko układu galerii.
* **Porcja 8b — runda 2** (2026-09-26; uwagi autora po rundzie 1, zielone światło tego dnia; reguły
  i uzasadnienia już w `architecture.md` i `decisions.md`):
  * *Wyzwanie* zwykłym tekstem krojem liczb, bez zakreślenia — w liście i na karcie (autor).
  * *Pasek przewijania jako nakładka* (autor, architekt poparł): nic nie skacze, brak pustego pasa
    w krótkich listach; jednakowy odstęp paska od krawędzi, góry i dołu; tło wierszy pod paskiem,
    treść przed nim; pasek widoczny zawsze przy przepełnieniu (architekt). Wyjątek menu znika.
  * *Kosz* widoczny zawsze, szary jak strzałka i data (`DungeonTextMutedBrush`), czerwony pod myszą;
    wiersz podświetlony także nad koszem — mechanizm tłumienia podświetlenia wiersza znika (autor).
* **Wyjątek od „nic bez konsumenta"** — konsumentem jest galeria (`decisions.md`, `architecture.md`,
  *Pytania otwarte i reguła „nic bez konsumenta"*).

## Notki

* Porcje trwają zwykle 40–68 kroków i 8–13 minut na przebieg; rundy poprawek — mniej. Poprawka zaraz
  po obejrzeniu idzie do wznowionego wykonawcy, jeśli jego pamięć jest ciepła (`collaboration.md`,
  *Briefy dla subagentów*).
* **Porcje osobno, każda oglądana od razu** — autor, 2026-09-25 (odrzucił łączenie 7 i 8 we wspólne
  oglądanie). Porcja 7 podzielona na 7a i 7b przez architekta: z poprawkami po porcji 6 i podpowiedzią
  przyciętych napisów nie mieściła się w jednym wykonawcy.
* Sesja zadania z jedną porcją, raportem i rozmową o uwagach doszła do ok. 170 tys. tokenów historii —
  autor zamknął ją po porcji 6; nowa sesja od porcji 7a.
* Porcja 6: pięć kontrolek zmieściło się w 12 minutach (architekt przewidywał przekroczenie limitu
  — mylnie). Kroki szły na szukanie w motywie kolorów, których brief nie nazwał (wyłączone, powierzchnia
  karty) — **brief podaje nazwy pędzli stanów**: wyłączone to przezroczystość `DungeonDisabledOpacity`,
  nie osobny kolor; neutralne wybrane — `DungeonSurfaceActiveBrush`; powierzchnia karty —
  `DungeonBackstageCardBrush`. Pismo: 13 — `DungeonBaseFontSize`, 12 — `DungeonHelperFontSize`;
  najechanie bez barwy — pędzel wiersza listy; tekst niebezpieczeństwa — `DungeonDangerTextBrush`
  (porcja 7a: podane w briefie, wykonawca nie szukał). Motywy w `DungeonControls.axaml` test budujący obejmuje sam — brief to
  mówi, żeby wykonawca nie sprawdzał.
* Porcja 7a (raport): **położenie `MenuFlyout` pod przyciskiem ustawia widok** — okienko wysuwane nie
  jest kontrolką, motyw go nie dosięga (tak samo zwykłe okienko z porcji 5). Brief kompozycji
  i zleceń A/B ma to podawać.
* Porcja 7b (raport): **okno potwierdzenia i powiadomienie znajdują warstwę przez okno elementu,
  z którego je wywołano** — wywołane z elementu wewnątrz okienka wysuwanego (menu, lista rozwijana)
  rzucą wyjątkiem. Brief zleceń, które wywołają je z menu, ma to podać (wywołanie z elementu okna,
  nie z pozycji menu) albo zlecić poprawkę.
* Porcja 7b: nowy ruch da się zrobić w kodzie, sprawdzając `SystemMotion.IsReduced` — wtedy nie
  dotyka `ReducedMotion.axaml` ani testu jego czterech reguł (`code-state.md`, *Pułapki*).
* Porcja 8 (raport): tekst akcentu na przygaszonym akcencie ma 4,05:1 — poniżej reguły 4,5:1
  (`architecture.md`, *Kolor tekstu*). Wykonawca dodał jaśniejsze odmiany do tekstu na przygaszonym tle
  (`DungeonAccentOnDim*`, `DungeonDangerOnDim*`) dla odznaki, tagu i chipa. **Wybrana zakładka,
  segment i wiersz listy mają ten sam błąd** — w `tasks.md`, *Poprawki czekające na obszar*.
* Porcja 8: odznaka i tag wyłączone przygasają, ale nie tracą barwy — barwa jest tu informacją
  (rzadkość), nie znakiem działania; reguła „wyłączona traci barwę” dotyczy kontrolek, które coś robią.
* **Pomiar sesji architekta 2026-09-25** (z zapisu sesji, pole `usage`): pamięć podręczna sesji
  architekta ma ważność **1 h** (zapis droższy: 2× zamiast 1,25×; odczyt 0,1× tak samo), wykonawców —
  **5 min**. Przerwy na wykonawcę (8 i 10,5 min) pamięci architekta nie wygasiły — po powrocie odczyt
  całości, zapis tylko przyrostu. Start sesji: ok. 55 tys. sama rama (instrukcje systemu, narzędzia,
  `CLAUDE.md`, pamięć), +28 tys. `collaboration.md` z dokumentem zadania, +30 tys. reszta lektur
  startowych (w tym ok. 13 tys. za `tasks.md` czytany od początku zamiast samej sekcji) → ok. 113 tys.
  przed pierwszym briefem. Zapytanie (każde wywołanie narzędzia) kosztuje ok. 10 % kontekstu; nowa sesja
  zwraca się po ok. jednej porcji. Z pomiaru: reguła „odsyłacz do sekcji czyta się jako sekcję”
  przyjęta (`collaboration.md`); budżet długości dokumentu czytanego co sesję — *Do sesji głównej*.
* Porcja 8b (raport): **odznaka akcentu nie stoi w wierszu listy** (architekt, z wiedzy o interfejsie).
  Na wybranym wierszu jej tekst ma 4,00:1 (przygaszony akcent na przygaszonym akcencie), a akcent
  w wierszu i tak konkuruje z samym wyborem. W wierszu — odznaka neutralna, stanu albo kolor systemu;
  akcent na karcie. Brief zlecenia A i porcji 10 ma to podać.
* Porcja 8b: elementy zbudowane w galerii jako własne — **przycisk w wierszu listy** (`row-action`:
  przycisk ikony bez wypełnienia, 28 × 28, żeby wiersz został 32) i obramowanie listy. Przy drugim
  użyciu (porcja 10 / zlecenie A) przechodzą do motywu. Odmiana „wpis/wpisy/wpisów” jest w galerii
  świadomym duplikatem funkcji z `Library.Entries.Desktop` (rama nie może się do niej odwołać).
* Porcja 8b: nazwy rodzaju wpisu w galerii odrzuca test architektury (`code-state.md`, *Pułapki*) —
  brief z danymi przykładowymi ma to podać.
* Porcja 8b, runda 1 (raport): **`.subtle` w wierszu listy nie ma widocznego najechania** — jego tło pod
  myszą (`DungeonSurfaceActive`) ma tę samą wartość co wiersz pod myszą (`DungeonBackstageRowHover`).
  Kosz go już nie używa; brief porcji 10 ma to rozstrzygnąć dla innych przycisków w wierszu.
* Porcja 8b, runda 1: w wierszu listy kampanii kosz stoi przed strzałką otwierania, nie na końcu —
  zostawione; do uwagi autora albo porcji 10.
* Porcja 8b, runda 2 (raport): **menu jest szersze o strefę paska (16)** także wtedy, gdy nie przewija —
  strefa w motywie `MenuItem` jest stała. Jeśli autor uzna prawy margines menu za zbyt duży — do
  rozstrzygnięcia (np. strefa tylko przy przepełnieniu, bo menu nie zmienia treści w trakcie).
* Porcja 8b, runda 2: kosz w wierszu kampanii dostaje wymiary lokalnym stylem widoku
  (`Button.campaign-delete`), bo motyw akcji niesie wymiary wiersza `ListBoxItem`. Porcja 10 (wiersz
  listy jako klocek) ma to zdjąć.
* Tabela jest pierwszym konsumentem „wyróżnienia komórki kolorem podanym przez układającego" —
  kolory modyfikatora dodatniego i ujemnego to tokeny systemu, nie tokeny stanów (`tasks.md`, *B*).

## Do sesji głównej

* **Budżet długości dokumentu czytanego na starcie każdej sesji** (propozycja architekta, 2026-09-25,
  z pomiaru w *Notkach*): `collaboration.md` ma ok. 500 linii i jest czytany w całości co sesję.
  Propozycja: limit długości dla dokumentów czytanych zawsze, rzadko potrzebne — do części czytanej
  „gdy potrzeba”. Limit ustalić po przejrzeniu, co z dokumentu sesja rzeczywiście używa na starcie.
  Autor: omówić w sesji głównej (przebudowuje dokument wspólny dla wszystkich zadań).

## Przy zamknięciu

* Tabela *Pomiar porcji* → `tasks.md`, punkt kontrolny — z niej werdykt o Avalonii.
* Ustalenia, które przeżywają zadanie — galeria na stałe, element spoza zestawu przechodzi do motywu
  przy drugim użyciu, fokus tylko po użyciu klawiatury — dostają dom w `architecture.md`
  (*Niezmiennik interfejsu*); przepiąć na niego odsyłacze, które dziś wskazują ten dokument
  (`collaboration.md`, *W interfejsie asystent decyduje…*; `decisions.md`, *Interfejs w HTML-u*).
