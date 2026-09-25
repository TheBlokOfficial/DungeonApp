# Zadanie: fundament interfejsu

Każda kontrolka, której aplikacja używa albo będzie używać, dostaje w motywie ramy własny, kompletny
szablon zamiast domyślnego — raz, w jednym miejscu — i jest pokazana w galerii kontrolek we wszystkich
stanach. Potem widoki składa się z gotowych klocków, a nie poprawia kontrolka po kontrolce.

**Stan na: 2026-09-25, po `abfa3aa`.** Na starcie sesji: `git log abfa3aa..master` i `git worktree
list` — wszystko, co tam jest, a czego ten dokument nie wymienia, zdarzyło się poza nim.

*Dokument zadania — co to jest, jak go prowadzić i kiedy umiera: [collaboration.md](../collaboration.md),
*Dokument zadania*.*

---

## Gdzie stoimy

Porcje 0–7a obejrzane przez autora; uwagi po 7a (układ menu) odłożone przez autora do `tasks.md`,
*Poprawki czekające na obszar*, bez rundy. Kopii roboczych brak. **Następny krok:** brief porcji
7b — zabiera z `tasks.md` podwójne przygaszenie treści wyłączonego pojemnika.

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

- [x] **0–6** — przyjęte; rundy i pomiar w tabeli niżej.
- [ ] **7a** — poprawki po porcji 6 (`tasks.md`); menu, menu kontekstowe, podpowiedź (także pełny
  napis przyciętej zakładki, segmentu i przycisku).
- [ ] **7b** — okno potwierdzenia, powiadomienie, wskaźnik
  postępu (kolor wypełnienia z właściwości kontrolki, domyślnie akcent — podaje go układający widok,
  np. pasek PZ; tak jak w suwaku z porcji 4).
- [ ] **8** — tag, chip, odznaka (odmiany po znaczeniu: neutralna, wyróżniona akcentem, stany;
  kolor podany przez system dla jego skal; wnętrze krojem liczb, wymiary z motywu), tabela
  (nagłówek, kolumny z wyrównaniem, wcięcia komórek, obramowanie, wyróżnienie pojedynczej komórki
  kolorem podanym przez układającego — pierwszy konsument: cechy potwora, `tasks.md`, *B*).
- [ ] **8b** — kompozycje przykładowe w galerii (pomysł autora): lista z filtrami i wyszukiwaniem,
  karta z tabelą i odznakami, okno potwierdzenia nad listą.
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
* Tabela jest pierwszym konsumentem „wyróżnienia komórki kolorem podanym przez układającego" —
  kolory modyfikatora dodatniego i ujemnego to tokeny systemu, nie tokeny stanów (`tasks.md`, *B*).

## Do sesji głównej

—

## Przy zamknięciu

* Tabela *Pomiar porcji* → `tasks.md`, punkt kontrolny — z niej werdykt o Avalonii.
* Ustalenia, które przeżywają zadanie — galeria na stałe, element spoza zestawu przechodzi do motywu
  przy drugim użyciu, fokus tylko po użyciu klawiatury — dostają dom w `architecture.md`
  (*Niezmiennik interfejsu*); przepiąć na niego odsyłacze, które dziś wskazują ten dokument
  (`collaboration.md`, *W interfejsie asystent decyduje…*; `decisions.md`, *Interfejs w HTML-u*).
