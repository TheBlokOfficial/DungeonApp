# DungeonApp — kolejka pracy

**Status: stan na 2026-09-22.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

**Zakres: wyłącznie to, co trzeba zrobić przed zamknięciem bieżącego etapu.** Nie jest spisem funkcji
aplikacji i nie zapisuje się tu pracy koncepcyjnej na zapas — całość docelowa mieszka
w [architecture.md](architecture.md), a pytania niezamknięte wraz z warunkami powrotu tam oraz
w [decisions.md](decisions.md). Pozycja wpisana tu przed swoim czasem starzeje się po cichu: nic nie
zmusza do jej przeliczenia, a sam fakt, że stoi zapisana, z czasem zaczyna uchodzić za uzasadnienie.

> **Ten dokument nie prowadzi archiwum.** Praca domknięta znika stąd, gdy tylko przestanie być
> potrzebna do zrozumienia następnego kroku. Co zostało zrobione, mówi historia gita; **dlaczego** —
> `decisions.md` i `architecture.md`; **jak jest teraz** — `code-map.md`. Do 2026-09-14 stała tu
> sesyjna kronika na dziewięćdziesiąt linii, wbrew temu zdaniu, które w tym dokumencie już wtedy było.

Gałąź: `master`. Build bez ostrzeżeń, 239 testów zielonych.

---

## Gdzie jesteśmy

Kroki 1–8 z sekcji „Kolejność prac" [architecture.md](architecture.md) są **zrobione** — krok 8
domknięty 2026-09-14 usunięciem licznika i całej warstwy bloków danych, a 2026-09-15 dogonieniem
go przez dokumenty. Było to przejście z szablonów jako plików danych na skompilowane typy treści,
a następnie osadzenie treści w konkretnej kampanii. Stan, do którego to doprowadziło:

* **Typy treści są kodem.** System `DungeonApp.Content.Dnd5e` niesie `Monster` i `Gear`, ich
  zaprojektowane karty i jedno okno biurka. Deserializator jest jedynym walidatorem.
* **Granice są pilnowane mechanicznie**, nie deklarowane — cztery testy architektoniczne, w tym skan
  słownictwa, który poszerza się sam wraz z przybywającymi typami treści.
* **Kampania ma własny świat.** Instancje z rzadką łatką, zapis całej generacji na dysk w jednej
  transakcji, rozwiązywanie wskazania wobec rejestru jako osobna warstwa odczytu.
* **System wnosi własne okno biurka** — mechanizm pasa narzędzi. Okno „Świat kampanii" jest
  pierwszym prawdziwym narzędziem biurka i pierwszym konsumentem magazynu instancji.
* **Jeden prymityw zapisu atomowego** zamiast trzech ręcznie pisanych kopii.
* **Instancje są jedynym magazynem stanu kampanii.** Warstwa bloków danych odeszła bez następcy;
  kształt, w jakim wróci razem ze swoim konsumentem, stoi w [decisions.md](decisions.md), pozycja
  „Utrzymanie warstwy bloków danych po odejściu jej jedynego konsumenta".

Szczegóły każdej z tych rzeczy — [code-map.md](code-map.md).

**2026-09-22 zapadła przebudowa, której kod jeszcze nie dogonił:** dawny silnik i powłoka stają się
ramą, wspólny kod wychodzi do biblioteki, a system — dawniej zestaw — przejmuje wygląd i zawartość
aplikacji. Model — [architecture.md](architecture.md), *Rama, biblioteka, system*.

---

## Następne

**Krok 9 z „Kolejność prac" [architecture.md](architecture.md): przebudowa na ramę, bibliotekę
i system.** Docelowy kształt stoi w architekturze — sekcje *Rama, biblioteka, system*, *Nawigacja:
ekran wyboru systemu i pasek boczny*, *Gdzie mieszka stan*, *Narzędzia biurka i system okien*.

**Plan etapów — zatwierdzony przez autora 2026-09-22.** Każdy etap dostaje osobne zielone światło;
jak przebiega jeden etap — [collaboration.md](collaboration.md), *Obieg jednego etapu*. Po każdym
etapie aplikacja działa, a testy przechodzą.

| # | Etap | Co widzi autor | Dlaczego w tym miejscu |
|---|---|---|---|
| 1 | **Ekran wyboru systemu i pasek boczny.** Rama wystawia systemowi deklarację zakładek; biurko i rejestr stają się zakładkami D&D; pozycja kampanii — półka albo strona kampanii; zakładki kampanii zamknięte bez otwartej kampanii; powrót do wyboru w górnym pasku; zakładka kategorii System nie dostaje kampanii. | nowe wejście do aplikacji, pasek z grupami Kampania i System, strona kampanii, kłódki | rama musi przestać sama stawiać biurko, zanim da się je z niej wynieść |
| 2 | **Wyniesienie wspólnego kodu interfejsu do biblioteki:** biurko i system okien, kontrolki kart, widok listy z kartą. Najpierw testy granic, potem przeprowadzka. | nic — zachowanie jak po etapie 1 | mechanizm pilnujący granicy powstaje przed rozbiórką |
| 3 | **Rama zapisuje modele stanu systemu.** Instancje pierwszym takim modelem; kampania pamięta swój system; półka pokazuje kampanie aktywnego systemu. | kampanie w obrębie swojego systemu | rdzeń trzyma dziś instancje wewnątrz kampanii i musi przestać, zanim da się je wynieść |
| 4 | **Wyniesienie wspólnej logiki do biblioteki:** paczki, wpisy, rejestr, instancje, nakładki. Wczytywanie paczek staje się krokiem startowym, który rama uruchamia, nie wiedząc, co robi. | nic | wymaga etapu 3 |

**Ustalenia do etapów** — zapisane, żeby nie trzeba ich było tłumaczyć od nowa:

* **Etap 1 — interfejs bez fajerwerków.** Decyzja autora: ekran wyboru i pasek boczny powstają jako
  technicznie poprawny interfejs w wąskim rozumieniu z [collaboration.md](collaboration.md), *Jak
  zapadają decyzje* — wyłącznie istniejące tokeny, zero zmian w plikach motywu, style lokalne dla
  widoku, bez animacji, kontrolek własnych i liczb wpisanych wprost. Wygląd autor przeprojektuje
  później. Ten etap zastępuje rusztowanie przełączania sekcji w powłoce. **Jeden wyjątek, zgoda
  autora 2026-09-22:** nowa ikona kłódki w motywie, w formacie i kresce istniejących ikon.
* **Etap 1 — projekt styku, gotowy do briefu (2026-09-22).** Zależności biurka, rejestru i rozgrzewki
  spisał tego dnia subagent; brief pisze się z tego punktu bez ponownego czytania kodu. Zmiana nazw
  „zestaw" → „system" w kodzie jest już zrobiona osobnym commitem (`IGameSystem`, `Dnd5eSystem`).
  - **Styk rama → system.** `IGameSystem` dostaje `DisplayName`, `SystemTabs` i `CampaignTabs` —
    stałe deklaracje `(Id, Title, IconResourceKey, fabryka)`, czytane raz przy wyborze systemu, id
    z prefiksem systemu. Fabryka zakładki systemu dostaje `SystemTabContext` (dziś wyłącznie rejestr —
    przejściowo, do etapu 4); fabryka zakładki kampanii jest asynchroniczna i dostaje
    `CampaignTabContext` (id kampanii, instancje, magistrala zdarzeń, drzwi zapisu, rejestr). Obie
    zwracają `ITabContent : IDisposable` z gotową kontrolką — zakładka, która nie umie sprzątać, się
    nie kompiluje. `CreateTools` znika z interfejsu.
  - **Biurko** wystawia systemowi jedno publiczne wejście: zakładka z kontekstu kampanii, magazynu
    układów i listy narzędzi. Samo wczytuje swój układ; zwolnienie zapisuje oczekujący układ.
    Magazyn układów tworzy korzeń kompozycji i podaje systemowi w konstruktorze — ścieżka na dysku
    bez zmian. `CampaignToolContext` powstaje z kontekstu kampanii; narzędzia podaje wyłącznie wybrany
    system. `CampaignToolProvider` znika.
  - **Rejestr** to zakładka systemu D&D z dzisiejszym ekranem, rysowana prezentacją tego systemu.
  - **Rama.** Ekran wyboru bez paska bocznego i górnego; pasek stanu zostaje, bo niesie ostrzeżenia
    startu. Po wyborze grupy Kampania (pozycja kampanii + zakładki kampanii) i System, rozdzielone
    separatorem, bez nagłówków; kategoria Aplikacja nie pokazuje się, dopóki nie ma pozycji. Otwarcie
    kampanii: zakładki otwarte, pozycja kampanii → strona kampanii (nazwa, data utworzenia) z nazwą
    kampanii jako etykietą. Zawartość zakładki powstaje przy pierwszym pokazaniu i żyje do zamknięcia
    kampanii albo powrotu do wyboru; zamknięcie, powrót i wyjście z programu ją zwalniają. Gdzie stoją
    „Zmień system" i „Zamknij kampanię" — pytanie do autora niżej. Błąd utworzenia zakładki —
    komunikat na pasku stanu, nie awaria.
  - **Start.** Przed ekranem wyboru tylko wczytanie paczek. Po wyborze: półka, wczytanie kampanii
    z półki z wyprzedzeniem (pamięć podręczna już tylko kampanii — część z układem odchodzi do biurka)
    i rozgrzewka: rama buduje zakładki kampanii dla pierwszej kampanii z półki ukryte i je zwalnia.
    Placeholder i jego rozgrzewka znikają.
  - **Znikają** też łańcuch przełączania sekcji, sztywne pozycje paska z podmianą na „Biblioteka
    kampanii" i agregat prezentacji, jeśli nie zostanie mu konsument.
  - **Miara „rama nie stawia biurka".** Powłoka, start, półka i kompozycja w `Desktop` nie odwołują się
    do biurka, systemu okien ani ekranu rejestru — grep w raporcie; test granic przychodzi w etapie 2.
  - **Testy.** Cykl życia zakładek na podstawionym systemie: zamknięte bez kampanii, otwarte po
    otwarciu, zawartość tworzona raz i zwalniana przy zamknięciu, powrocie, wyjściu i po rozgrzewce.
    Biurko utworzone i zwolnione bez gestu nie zapisuje układu. Test architektoniczny: kontekst
    zakładki systemu nie wystawia niczego z kampanii. Liczba testów nie niższa niż baseline.
  - **Rozstrzygnięte przez architekta**, do weta autora: nazwa `IGameSystem`; kłódka zamiast ikony
    zakładki; separator zamiast nagłówków grup; kategoria Aplikacja ukryta bez pozycji; bez
    rozgrzewki biurka, gdy półka jest pusta (dziś rozgrzewa się sam szkielet).
  - **Znane ograniczenie.** Zakładka rejestru rysuje karty prezentacją swojego systemu, więc przy
    drugim systemie wpis cudzego systemu nie dostanie karty — wraca z pytaniem „Paczka a system"
    w architekturze.
  - **Przegląd przed startem (2026-09-22, subagent, tylko odczyt).** Projekt zderzony z kodem:
    - **Górnego paska nie ma na ekranie** — widok istnieje w kodzie, ale nic go nie wyświetla;
      zamknięcie kampanii żyje dziś w pasku bocznym jako podmiana na „Biblioteka kampanii", którą ten
      etap usuwa. **Pytanie do autora na start sesji**, rekomendacja architekta: „Zamknij kampanię"
      na stronie kampanii, „Zmień system" jako pierwsza pozycja kategorii Aplikacja — obie rzeczy
      należą do ramy, a górnego paska ten etap nie włącza. Bez odpowiedzi brief nie rusza: inaczej po
      etapie nie da się zamknąć kampanii.
    - **Zapis układu jest już bezpieczny**: odtworzenie i dopasowanie układu nie oznaczają go do
      zapisu, robią to wyłącznie gesty. Wymóg „utworzone i zwolnione bez gestu nic nie zapisuje" ma
      tylko przetrwać przebudowę — test go pilnuje.
    - **Rozstrzygnięte po przeglądzie:** kontekst zakładki kampanii nie niesie resolvera —
      `CampaignToolContext` buduje go sam z rejestru i katalogu systemu, jak dziś; system dostaje
      w konstruktorze gołe `WorkspaceLayoutStore`, a wyliczenie katalogu danych aplikacji przenosi się
      w całości do `Program.cs`; zamknięta zakładka: etykieta pędzlem `DungeonTextMutedBrush`, kłódka
      piórem `DungeonIconPen` (format jak `DungeonIconUsers`); ikona pozycji paska staje się stanem
      pochodnym (dziś jest stała); ekran wyboru zastępuje wiersz treści okna (pasek boczny + obszar
      treści), wiersz paska stanu zostaje.
    - **Półka nie ma blokady usunięcia otwartej kampanii** — chroni ją wyłącznie to, że przy otwartej
      kampanii jej nie widać. Strona kampanii w miejscu półki musi tę niewidoczność zachować.
    - **Jedna zmiana, bez punktów pośrednich:** interfejs systemu, biurko, przygotowanie kampanii
      i rozgrzewka przechodzą w jednym kroku — nie da się ich rozłożyć na kompilujące się etapy.
      Rozsypią się testy: pas narzędzi (cztery, do usunięcia z mechanizmem), podstawiony system,
      wszystkie miejsca tworzące system D&D bez argumentu, pamięć przygotowania (pole układu).
      Pomocnicze do nowych testów: katalogi tymczasowe jak w testach magazynu układów, repozytorium
      w pamięci z testów D&D, podstawiony system po aktualizacji.
  - **Obieg.** Jeden przebieg subagenta w kopii zrównanej z lokalnym `master`, z dziennikiem postępu
    w tej kopii (przerwanie w połowie nie gubi stanu); drugi subagent porównuje wynik z tym punktem
    i z pięcioma zakazami.
* **Etap 3 — kształt drogi zapisu najpierw do autora.** Na tej drodze stoją czwarty i piąty zakaz,
  więc przed briefem asystent przynosi autorowi jej kształt opisany prostym językiem.
* **Etap 3 — stare kampanie to dane testowe.** Decyzja autora: kampanie zapisane bez systemu stają
  się niedostępne — widoczne, nie znikają ([architecture.md](architecture.md), *Gdzie mieszka
  stan*) — i zakłada się je od nowa. Żadnego kodu przypisującego im system.
* **Między etapami nic nie wchodzi na zapas.** Dodatki powstają z pierwszym prawdziwym dodatkiem
  (niżej, „Odłożone"), formuły, sloty i dokument — po etapie 4.

Formuły, sloty i dokument są odtąd krokiem 10 i czekają na przebudowę, żeby powstać od razu
w bibliotece.

---

## Odłożone

### Czeka na pierwszy prawdziwy dodatek: mechanizm dodatków

Model i reguły — [architecture.md](architecture.md), *Dodatki*; od 2026-09-22 forma dodatku należy
do ramy, a jednostką włączania jest wariant pod nagłówkiem dodatku. Mechanizm powstaje razem
z pierwszym prawdziwym dodatkiem, nie wcześniej: dziś nie ma ani jednego. Przykład, na którym
rozmawiano — złoto w sakiewkach zamiast na postaci — wymaga licznika złota, którego też jeszcze nie
ma.

**Czekanie nie kosztuje nic, i to jest własność repozytorium, nie prognoza.** Deserializacja
manifestu kampanii jest celowo pobłażliwa, więc dołożenie listy włączonych dodatków później nie
podnosi wersji formatu i nie wymaga migracji — patrz [decisions.md](decisions.md), „Zarezerwowane
pola `Ruleset` i `ContentPacks` w manifeście kampanii".

**Wyzwalacz:** pierwszy wariant zasad, który autor chce mieć w konkretnej kampanii.

### Czekają na miejsce na ekranie — trzy pozycje o interfejsie

Wszystkie czekają, aż autor zechce zaprojektować dla nich miejsce na ekranie. Żadna nie blokuje
przebudowy — ale przebudowa zmienia ekran, na którym dwie pierwsze by stanęły: rejestr przechodzi
do zakładek treści systemu. Warto więc rozważyć je przy projektowaniu tych zakładek, nie osobno.

1. **Odrzucone paczki nigdzie się nie pokazują.** Loader je odnotowuje, ekran rejestru ich nie
   wyświetla — świadoma decyzja autora z 2026-09-12. Jedyna pozycja z tabeli „Co się dzieje, gdy
   treść jest zepsuta", o której Mistrz Gry nie dowiaduje się z aplikacji: paczka odrzucona za
   literówkę w manifeście znika dziś po cichu.
2. **Nagłówek `NIE WCZYTANE` czyni pierwszy zepsuty wiersz wyższym od pozostałych**, bo niesie go ten
   wiersz, a nie prawdziwy nagłówek sekcji. Cena za „jedna lista, jeden szablon", zostawiona
   świadomie 2026-09-13.
3. **Nie da się nazwać okazu.** Silnik umie zmienić nazwę własną instancji, jest to przetestowane
   i **nikt tego nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, mimo
   że lista pokazuje właśnie nazwę własną, gdy jest. Konsument jest jednym polem tekstowym stąd.

---

## Archiwum

`archive/pre-pivot-content-layer` (`f5d5b7a`) — **nigdy nie scalana**. Praca sprzed
przeprojektowania warstwy treści: `SummaryContract`, `ContractFill`,
`SummaryContractResolution`, grupowanie rejestru, rozbudowane testy loadera i fixture'y.
Trzy fixture'y wpisów zostały stamtąd przywrócone; resztę trzyma się tylko po to, żeby nic
nie zginęło.
