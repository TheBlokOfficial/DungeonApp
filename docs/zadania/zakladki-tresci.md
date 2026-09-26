# Zadanie: zakładki treści

Zakładki potworów i przedmiotów (i być może zaklęć) dostają nowy wygląd lista–szczegół złożony
z klocków fundamentu interfejsu: listę z filtrami i karty projektowane per typ treści, oparte na tym, co
D&D 5e naprawdę przechowuje przy potworze, przedmiocie i zaklęciu. Paczka przykładowa dostaje
kilkanaście wpisów na zakładkę, z których każdy pokazuje kartę z innej strony. Zadanie kończy się
jawnym werdyktem o Avalonii.

**Stan na: 2026-09-26, po `6afc89f`.** Na starcie sesji: `git log 6afc89f..master` i `git worktree
list` — wszystko, co tam jest, a czego ten dokument nie wymienia, zdarzyło się poza nim.

*Dokument zadania — co to jest, jak go prowadzić i kiedy umiera: [collaboration.md](../collaboration.md),
*Dokument zadania*.*

---

## Gdzie stoimy

Zadanie założone 2026-09-26 (sesja fundamentu, na polecenie autora); nic jeszcze nie ruszyło. **Fundament
interfejsu czeka na obejrzenie 9a+9b przez autora** ([fundament-interfejsu.md](fundament-interfejsu.md),
*Notki*, *Po 9a*). **Następny krok:** na starcie sesji zapytać autora o wynik obejrzenia; po przyjęciu —
zamknąć fundament według jego *Przy zamknięciu* (autor pozwolił zrobić to w sesji tego zadania,
2026-09-26), uwagi — poprawka w fundamencie. Potem wycinek 1 planu (dane D&D 5e).

## Zakres i koniec

**Wchodzi** (krok 10 z kolejki, zakres z 2026-09-24, poszerzony przez autora 2026-09-26):
* **Zakładki potworów i przedmiotów** w kategorii System, z jednego szkieletu biblioteki wpisów:
  wyszukiwanie po nazwie; sortowanie po nazwie i po kluczu systemu, z odwracaniem; filtry po kategorii,
  po paczce i po wartości wskazanej przez system; czyszczenie filtrów; odznaka wiersza; licznik wpisów;
  nagłówek szczegółu (kategoria, nazwa, tagi) rysowany przez szkielet, pod nim karta systemu; sekcje po
  paczce z rzeczami zepsutymi na dole; powód błędu w miejscu karty; przycisk wczytania paczek od nowa.
* **Nowy projekt wyglądu lista–szczegół** (autor, 2026-09-26): praca przede wszystkim projektowa
  i twórcza — pełna swoboda w obrębie koncepcji aplikacji i sensu treści; mockup rejestru
  (`docs/images/mockup_rejestr.png`, `mockup_rejestr.html`) jako luźne odniesienie; składane z klocków
  fundamentu.
* **Sprawdzenie, co D&D 5e przechowuje** przy potworze, przedmiocie i zaklęciu — podstawa kart
  i ewentualnych zmian rekordów systemu.
* **Wpisy przykładowe:** kilka–kilkanaście na zakładkę, każdy pokazuje kartę z innej strony (np. potwór
  z czarami, z akcjami legendarnymi, z odpornościami, bez akcji; przedmiot magiczny z ładunkami, zwykły
  ekwipunek, broń).
* **Zaklęcia — „ewentualnie”** (autor): nowy typ treści i zakładka; decyzja przy etapie (*Notki*,
  *Zaklęcia a drugi zakaz*).
* **Punkt kontrolny o Avalonii** (*Ustalenia*).

**Nie wchodzi:** tworzenie, edycja, kopiowanie, usuwanie wpisów; NPC; filtr po typie treści (bez
zakładki z kilkoma typami nie ma konsumenta); pasek stanu i Ctrl+K z mockupu; akcje jako osobne rzeczy
z formułami (krok 11 — *Notki*); dodanie paczki przeciągnięciem (osobny etap w `tasks.md`).

**Koniec:** zakładki potworów i przedmiotów (i zaklęć, jeśli weszły) przyjęte przez autora, wpisy
przykładowe w paczce, werdykt o Avalonii zapisany w `decisions.md`. Wtedy ten dokument umiera.

## Do przeczytania

Na starcie:
* [architecture.md](../architecture.md), *Zakładki treści* — docelowy kształt zakładek.
* [architecture.md](../architecture.md), *Niezmiennik interfejsu* — reguły, którym podlega każdy widok.

Przed wycinkiem danych i przed zmianą rekordu systemu:
* [architecture.md](../architecture.md), *Deklaracja treści* i *Granica automatyzacji* — co wolno
  w rekordzie; pięć zakazów w `CLAUDE.md`.
* [code-state.md](../code-state.md), *Punkty rozszerzeń*, *Nowy typ treści* i *Nowa zakładka systemu*
  (zaklęcia).

Przed każdym briefem:
* [code-state.md](../code-state.md), *Pułapki* — zwłaszcza motyw ramy, okienka, pasek przewijania.
* [tasks.md](../tasks.md), *Poprawki czekające na obszar*.
* `.claude/agents/wykonawca.md`, *Rzemiosło interfejsu* — żeby brief jej nie powtarzał.

Tylko gdy potrzeba:
* [decisions.md](../decisions.md), *Nawigacja i interfejs* (część B) i *Niezmiennik interfejsu* —
  zanim zaproponujesz zmianę konwencji.
* Galeria kontrolek, sekcja „Kompozycje” — wzór listy z filtrami i karty złożonych z klocków.

**Na starcie nie czytaj** całej architektury, rejestru decyzji ani reszty kolejki.

## Plan

Wycinki w kolejności; każdy z osobnym zielonym światłem. Porcja wykonawcy = 20 minut.

- [ ] **0** — obejrzenie i zamknięcie fundamentu (*Gdzie stoimy*).
- [ ] **1 — dane D&D 5e** (architekt; zwiad kodu — `zwiadowca`): co SRD 5.1/5.2 trzyma przy potworze,
  przedmiocie (magicznym i zwykłym) i zaklęciu; co dziś trzymają rekordy systemu i paczki autora;
  luki i propozycja zmian rekordów — sprawdzona z pięcioma zakazami.
- [ ] **2 — projekt** (architekt): lista i karty per typ, z makietą do obejrzenia przez autora przed
  kodem. Wynik dzieli się na porcje wykonawcze.
- [ ] **3 — lista i filtry** (*Ustalenia*, *A*).
- [ ] **4 — karta potwora** (*Ustalenia*, *B*).
- [ ] **5 — karta przedmiotu** (dawniej „po kroku 10”, według mockupu — teraz według projektu z 2).
- [ ] **6 — wpisy przykładowe** — może iść razem z 4 i 5 (wpis pokazuje wariant karty, który porcja
  buduje); miejsce paczki — *Notki*.
- [ ] **7 — wczytanie paczek od nowa**: zwiad, jak żyje rejestr (kto go trzyma, kto dostaje przy
  zakładkach i biurku), potem zlecenie. Przesłankę sprawdzić przed zwiadem.
- [ ] **8 — zaklęcia** (decyzja przy etapie).
- [ ] **9 — punkt kontrolny** — werdykt o Avalonii.

**Pomiar porcji** — materiał do werdyktu. „Rundy" to poprawki od autora, każda oznaczona: **R** — błąd
rzemiosła albo błąd kontrolki fundamentu, **W** — zmiana wymagania po obejrzeniu. „Pomiar" — kroki /
minuty / odczyt z `tools/subagent-usage.py`, suma przebiegów porcji.

| # | Porcja | Rundy | Pomiar |
|---|---|---|---|

## Ustalenia

* **Punkt kontrolny: czy fundament rozwiązał problem** — autor, 2026-09-24; próg — autor, 2026-09-26.
  Po fundamencie i wdrożeniu go w aplikację architekt z autorem **explicite uznają albo odrzucają**
  kierunek „Avalonia w obecnej formie". Problem: interfejs od wykonawców był fuszerką, a jego poprawianie
  zjadało większość budżetu sesji. Autor: „chciałbym to na końcu nazwać […] i rozwiązać, czy zbudowanie
  tych fundamentów rozwiązało problem".
  * **Próg „uznany”** (ustalony przed pierwszą porcją, żeby nie dopasowywać go do wyniku): porcja
    ekranu przyjęta po najwyżej jednej rundzie; żadna runda **R** w kontrolce z fundamentu; żaden
    przebieg ponad limit 20 minut. Rundy **W** nie świadczą przeciw technologii.
  * **Odniesienie** (`tools/subagent-usage.py`, na zapisach od 22.09): typowe zlecenie z kodem — 124–131
    kroków, 17,5–18,1 minuty; trzy najdroższe przebiegi w historii to widoki (najdroższy: 362 kroki,
    53 minuty, 142 mln odczytu). Fundament: przebiegi 40–70 kroków, 6–14 minut — **ale** porcje są celowo
    małe (limit 20 minut od 2026-09-24), więc spadek na przebieg nie jest sam dowodem; tabela fundamentu
    rund nie dzieli na R i W.
  * Wynik → `decisions.md`, wpis o interfejsie w HTML-u (część B, *Nawigacja i interfejs*): **uznany** —
    Avalonia zostaje z uzasadnieniem z liczb; **odrzucony** — wyzwalacz próby z interfejsem w HTML-u na
    jednej zakładce. Otwarte błędy okienek widoczne tylko w aplikacji (fundament) wchodzą do werdyktu,
    jeśli okażą się ograniczeniem biblioteki, nie naszym błędem.
* **Projektuje architekt, buduje wykonawca** (architekt, 2026-09-26; `collaboration.md`, *Deleguj kod,
  nie decyzje*): układ list i kart to decyzja — architekt proponuje ją z makietą, autor przyjmuje,
  wykonawca składa z klocków. Swoboda twórcza (autor) dotyczy projektu, nie pominięcia zgody na etap.
* **„Instancje” z polecenia autora to wpisy** (architekt, 2026-09-26 — do potwierdzenia przez autora):
  pozycje w paczce, nie egzemplarze w kampanii (`architecture.md`, *Słownik*).
* **Treść przykładowa z SRD albo własna** (architekt, 2026-09-26): SRD 5.1/5.2 jest na licencji CC-BY
  4.0 — paczka niesie przypisanie autorstwa; nazwy i opisy tłumaczymy sami. Nie przepisujemy potworów
  ani przedmiotów spoza SRD.
* **Wymiary z `docs/images/mockup_rejestr.html`**, nie z obrazka — tam, gdzie projekt z wycinka 2
  bierze coś z mockupu. Pasek tytułu okna w mockupie nie jest projektem paska górnego. **Mockup jest
  prawie kwadratowy** — na ekranie 16:9 lista trzyma szerokość z mockupu, treść szczegółu swoją
  największą szerokość, wyrównana do listy.
* **Kolory po znaczeniu:** treść niewczytana bierze kolor „niebezpieczeństwa"; kolory rzadkości
  i modyfikatorów — własne tokeny w systemie.
* **A. Lista i filtry** (z kolejki; część zrobił fundament):
  * **Wysokość i szerokość wiersza nadaje biblioteka**, nie zawartość — dziś przedmioty (pigułka
    rzadkości) mają wyższe wiersze niż potwory.
  * **Filtr = lista wartości z polami wyboru**, bez pozycji „Wszystkie"; nic nie zaznaczone = wszystko.
    Wewnątrz filtra „lub", między filtrami „i" (architekt). Istniejące testy filtrów dostosować do nowej
    semantyki.
  * **Chipy filtrów na chip z listą** (`DropDownPicker` w stroju chipa — fundament, runda 1c): jedne
    reguły napisu (nazwa filtra / wartość / pierwsza + „+N”); pismo chipów z `ContentChipFontSize`
    (11,5 — poza skalą pisma) na motyw. Chip „Kolor” w galerii (`DungeonChipOpener` z `ListBox`) nie
    odznacza — przechodzi tu razem z chipami.
  * „Wyczyść filtry" wygaszone, gdy nie ma czego czyścić.
  * **Odwracanie sortowania** — klocek `SortPicker` stoi w zakładce z wygaszonym przyciskiem kierunku
    (`CanReverse=False`): model widoku dostaje kierunek i rodzaj pól (`NumericOptions` — napis „A–Z” /
    „rosnąco” w menu i podpowiedzi).
* **B. Karta potwora** (z kolejki):
  * Wyzwanie — zwykły tekst krojem liczb, samo „1/2" (bez zakreślenia — `decisions.md`, *Wyzwanie bez
    zakreślenia*); PD obok, przygaszone. Tagi kategorii („Humanoid") — tag z fundamentu.
  * Opisy, akcje i cechy szczególne stylem tekstu do zaznaczenia; powód błędu w miejscu karty także.
  * Nagłówki „Cechy szczególne"/„Akcje" jasne i wyraźne; liczby KP/PZ/szybkości i cech wyróżnione;
    ikonki przy KP, PZ, szybkości — punkt wyjścia, projekt z wycinka 2 może to zmienić.
  * **Wyzwanie i PD to osobne pola w paczce** (autor, 2026-09-24) — dziś jeden napis „1/2 (100 PD)";
    odznaka i chip pokazują samo wyzwanie. Autor przerabia swoje paczki po zmianie.
  * **Cechy potwora — projekt autora 2026-09-24:** kwadratowa tabela z klocka tabeli, trzy kolumny,
    sześć wierszy, ostre narożniki (wzór: galeria, `AbilityScoresSample`); komórka modyfikatora
    malowana: dodatni — zielone tło, ujemny — czerwone. Kolory **nie** z tokenów stanów — system
    dostaje własne tokeny „modyfikator dodatni"/„modyfikator ujemny" z zapisanym znaczeniem. Malowanie
    to prezentacja w kodzie karty, nie wyrażenie w danych — pierwszego zakazu nie dotyczy.

## Notki

* **Zaklęcia a drugi zakaz** (architekt, 2026-09-26): zaklęcie niesie czas rzucania, czas trwania,
  koncentrację i zasięg. Drugi zakaz: „nigdzie nie ma pola oznaczającego czas trwania […] czas w treści
  jest tekstem na karcie". Litera zabrania pola „czas trwania” nawet jako tekstu; intencja — pola, po
  którym cokolwiek liczy albo filtruje. Rozjazd zgłosić autorowi przed projektem rekordu zaklęcia
  (`collaboration.md`, *Lekcja dla asystenta*), nie rozstrzygać samemu — *Do sesji głównej*.
* **Akcje jako osobne rzeczy to krok 11** (kolejka, 2026-09-24): nazwy akcji, pogrubione premie
  i nagłówki akcji z mockupu wymagają akcji jako osobnych pozycji rekordu, a formuły i sloty przyszły
  po kroku 10. Jeśli projekt z wycinka 2 ich potrzebuje — *Do sesji głównej* (kolejność kolejki).
* **Wpis JSON nie zmienia formatu ani o znak** (`collaboration.md`, *Warstwa treści*): koperta wpisu
  stoi; zmieniają się najwyżej pola `values` rekordu typu (tak jak wyzwanie i PD). Sprawdzić
  z *Deklaracją treści* przed propozycją zmian.
* **Gdzie mieszkają wpisy przykładowe — do ustalenia w wycinku 1.** Paczek dostarczanych z systemem nie
  ma w kodzie (`code-state.md`, *Dług*); paczki autora leżą w jego katalogu dokumentów, poza
  repozytorium. Testy formatu działają na paczkach wzorcowych w repozytorium.
* **Czeka na autora** (z kolejki, 2026-09-24): ręczne przeniesienie kampanii do
  `Dokumenty\DungeonApp\dnd5e\campaigns\` (stare `Packs` i `Campaigns` nie są czytane); dopisanie
  `group` potworom.
* **Położenie `MenuFlyout` pod przyciskiem ustawia widok** (fundament, porcja 7a) — okienko wysuwane nie
  jest kontrolką, motyw go nie dosięga. Brief ma to podawać.
* **Okno potwierdzenia i powiadomienie wywołane z pozycji menu rzucą wyjątkiem** (fundament, porcja 7b)
  — szukają warstwy przez okno elementu wywołującego. Brief podaje: wywołanie z elementu okna albo
  poprawka.
* **Odznaka akcentu nie stoi w wierszu listy** (fundament, porcja 8b): na wybranym wierszu jej tekst ma
  4,00:1, a akcent konkuruje z samym wyborem. W wierszu — odznaka neutralna, stanu albo kolor systemu;
  akcent na karcie. Brief listy ma to podać.

## Do sesji głównej

* **Zaklęcia a drugi zakaz** — *Notki*. Dotyka pięciu zakazów: autor rozstrzyga, zanim powstanie rekord.
* **Akcje jako osobne rzeczy przed krokiem 11?** — tylko jeśli projekt z wycinka 2 ich potrzebuje.

## Przy zamknięciu

* Werdykt o Avalonii → `decisions.md` (*Ustalenia*, *Punkt kontrolny*); tabela *Pomiar porcji* razem
  z nim.
* Rozstrzygnięcia o kartach i liście, które przeżywają zadanie → `architecture.md`, *Zakładki treści*.
* Odnośnik w `tasks.md` znika; kolejka przechodzi do kroku 11.
