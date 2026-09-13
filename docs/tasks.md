# DungeonApp — kolejka pracy

**Status: stan na 2026-09-13.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

Kolejność w sekcji „Następne" jest wiążąca tam, gdzie to zapisano.

**Zakres tego dokumentu: wyłącznie to, co trzeba zrobić przed zamknięciem bieżącego etapu.** Nie jest
spisem funkcji aplikacji i nie zapisuje się tu pracy koncepcyjnej na zapas — całość docelowa mieszka
w [architecture.md](architecture.md), a pytania niezamknięte wraz z warunkami powrotu tam oraz
w [decisions.md](decisions.md). Pozycja wpisana tu przed swoim czasem starzeje się po cichu: nic nie
zmusza do jej przeliczenia, a sam fakt, że stoi zapisana, z czasem zaczyna uchodzić za uzasadnienie.

Gałąź: `master`. Build i 343 testy zielone.

---

## Gdzie jesteśmy

Kroki 1–4 z sekcji „Kolejność prac" [architecture.md](architecture.md) są przejściem
z szablonów jako plików danych na skompilowane typy treści. **Zrobione:**

* `DungeonApp.App` jako korzeń kompozycji — powłoka przestała być plikiem wykonywalnym, więc
  granica „powłoka nie zna żadnego zestawu" jest sprawdzalna testem po referencjach.
* `DungeonApp.Content.Dnd5e` z typami `Monster` i `Gear` na wymaganych polach nazwanych.
* `tests/DungeonApp.Architecture.Tests` — skan słownictwa (silnik i powłoka, `.cs` i `.axaml`,
  słownik z refleksji po zestawie) plus testy po referencjach.
* Wpisy biorą kształt z zestawu: koperta `ContentValues`, `IContentTypeCatalog`,
  `IContentPresentation`, pierwsze zaprojektowane `MonsterCardView` i `GearCardView`.
* `TreatWarningsAsErrors` — kompilator jest walidatorem treści, więc jego ostrzeżenia są
  ostrzeżeniami o treści.

**Domknięte 2026-09-13 (szósta sesja) — magazyn instancji ma konsumenta:**

* **Kolekcja instancji w kampanii.** Dodanie, usunięcie, zmiana nazwy własnej i podmiana łatki,
  każde ze swoim zdarzeniem niosącym sam identyfikator. Odtwarzanie zapisu idzie osobnymi drzwiami
  i nie ogłasza niczego, wzorem bloków danych. Magazyn **nie wie nic o rejestrze treści**.
* **Zapis na dysk.** Plik na instancję w podkatalogu kampanii, wyliczony w manifeście, w tej samej
  transakcji i generacji co bloki danych, z manifestem lądującym ostatnim. Plik po usuniętej
  instancji kasowany **po** zatwierdzeniu, nigdy przed — a że manifest jest indeksem, przerwane
  kasowanie zostawia śmieć, nie zmartwychwstałą instancję. Wersja formatu została przy `1`.
* **Rozwiązywanie wskazania wobec rejestru** jako osobna warstwa odczytu, z czterema powodami
  nierozwiązania. Pytana od nowa przy każdym odczycie, więc zainstalowanie brakującej paczki
  naprawia instancję bez przepisywania kampanii. Łatka nierozwiązanej instancji wychodzi nietknięta.
  Sprawdzanie idzie na wartościach **scalonych**, nie na wartościach wpisu — osobny test pilnuje
  tego, bo napisane odwrotnie wygląda identycznie.
* **Zestaw treści wnosi własne okna biurka** — mechanizm „tool belt". Powłoka daje ramę i wąskie
  okno na otwartą kampanię: instancje, rejestr, rozwiązywanie i te same drzwi zapisu, z których
  korzysta każdy panel. Zestaw oddaje gotową kontrolkę, więc żaden szablon w powłoce nie zna typu
  z zestawu. Rejestr podawany jako obietnica, nie wartość, bo przy budowie korzenia kompozycji
  paczki nie są jeszcze wczytane.
* **Okno „Świat kampanii"** z zestawu D&D: lista instancji kampanii, dodawanie z wpisów tego
  zestawu, **edycja bieżących punktów życia** i usuwanie. Instancja nierozwiązana zostaje na liście
  z wyjaśnieniem. Punkty życia to **dwa zwykłe pola** — wpis niesie maksimum, nakładka bieżące —
  a zapis różnicuje edytowany rekord **wobec wartości wpisu**, nie wobec scalonych.

  To jest zarazem **pierwsze prawdziwe narzędzie biurka**, czyli wyzwalacz dwóch rzeczy z sekcji
  niżej, który właśnie się odpalił.

**Domknięte 2026-09-13 (piąta sesja):**

* **Manifest kampanii nie wozi już pustych pól.** `Ruleset` i `ContentPacks` — oba zawsze puste,
  oba bez konsumenta od czterech sesji — zniknęły z kodu razem z testem, który je zamrażał jako
  „kontrakt, nie dekorację". Wersja formatu została przy `1`: odczyt manifestu jest celowo
  pobłażliwy, więc starszy plik niosący te klucze otwiera się dalej. Pilnują tego dwa nowe testy
  (nowy zapis ich nie niesie; stary plik nadal się wczytuje).

  Powód, dla którego to weszło przed magazynem instancji, jest ostrzejszy niż samo sprzątanie:
  **`decisions.md` opisywał tę decyzję jako już wykonaną**, łącznie z testem, który nie istniał,
  podczas gdy kod robił dokładnie odwrotnie. Pozycja została przepisana na to, co jest prawdą,
  z notatką, że czas przeszły w tamtym dokumencie nie jest dowodem stanu repozytorium. Przy okazji
  zweryfikowano akapit „Nawyk do przerwania" w `architecture.md`: z sześciu wyliczonych tam rzeczy
  zbudowanych bez konsumenta **nie istnieje już żadna**, a przewidywał śmierć dwóch.

* **Koperta wartości treści umie już arytmetykę nakładki.** Doszły trzy operacje: nałożenie łatki
  na wartości wpisu, wyliczenie łatki jako różnicy wobec wpisu i zapieczętowanie rekordu zestawu
  treści z powrotem w kopertę. Wszystkie przesuwają całe właściwości po nazwach, które koperta już
  niesie — **żadna nie zapisuje nazwy pola, nie pyta, co ta nazwa znaczy, ani nie czyta wartości**,
  więc stoją na tym samym uzasadnieniu co istniejący generyczny odczyt. Doszły też `InstanceId`
  i rekord `CampaignInstance`.

  Dwie decyzje są nośne i opisane przy kodzie: **zapis pomija `null`** (inaczej każda niewypełniona
  właściwość rekordu nadpisałaby wartość wpisu nullem — a przy okazji daje to pożądane cofanie
  odchylenia przez wyzerowanie właściwości) i **scalanie jest płytkie** (scalanie w głąb wymagałoby
  osądu, czy dwa obiekty pod jedną nazwą to ta sama rzecz — a ten osąd należy do zestawu treści).
  Porównanie wartości idzie przez `JsonElement.DeepEquals`, nie przez surowy tekst.

**Domknięte 2026-09-13 (czwarta sesja):**

* **Jeden prymityw zapisu atomowego.** `AtomicWrite` w silniku zbiera pliki obok ich miejsc
  docelowych i przenosi je dopiero wtedy, gdy wszystkie zserializowały się czysto; przerwanie
  w połowie zostawia każde miejsce docelowe nietknięte, a po sobie nie zostawia pliku roboczego.
  Obsługuje oba potrzebne przypadki — jeden plik i całą generację naraz — bo `Commit` przenosi
  w kolejności dodania. Trzy ręcznie pisane ścieżki (`JsonCampaignRepository` ×2 plus
  `WorkspaceLayoutStore`) korzystają teraz z niego. Wydzielone **przed** magazynem instancji
  właśnie po to, żeby nie dołożył czwartej.
* **Kolejność zatwierdzania jest zamrożona testem.** Na niej stoi „manifest ląduje ostatni", czyli
  wykrywalność rozdartego zapisu — a nie sprawdzał jej dotąd żaden test, bo wzorzec był powtarzany
  trzy razy i weryfikowany wyłącznie pośrednio.
* **Magazyn układu biurka trzymany za słowo.** Obiecuje, że odczyt nigdy nie rzuca, a czego nie
  da się zrozumieć, zamienia w układ domyślny — i tej obietnicy nie sprawdzał żaden test. Teraz
  sprawdza ją dwadzieścia jeden, każdy po obu ścieżkach odczytu, bo mapowanie jest w magazynie
  napisane dwa razy i obie kopie muszą się zgadzać. Kod produkcyjny nietknięty.
* **Powód odrzucenia mówi co, nie który plik.** Ekran rejestru pokazywał nazwę zepsutego pliku dwa
  razy — jako tytuł wiersza i ponownie w treści komunikatu. Naprawione u źródła: `Location` mówi
  który plik, `Reason` mówi co jest nie tak. Odrzucenie paczki nadal nazywa plik samo, bo tam
  żaden tytuł wiersza tej roli nie przejmuje. Wyjątkiem pozostaje kolizja id, gdzie komunikat
  wymienia pliki kolidujące — ale już nie ten własny.
* **Nazwa pliku roboczego ujednolicona** na `.writing.tmp` w obu magazynach. Układ biurka używał
  wcześniej nazwy z `Guid`, która po awarii aplikacji zostawiała śmieć na zawsze; nazwa
  deterministyczna jest nadpisywana przy następnym zapisie. To jedyna zmiana zachowania
  w całym kroku.

**Domknięte 2026-09-12 (trzecia sesja):**

* **Odrzucanie per plik wpisu.** Defekt jednego pliku oznacza od teraz ten jeden plik; reszta paczki
  wczytuje się normalnie. Na poziomie paczki zostaje tylko to, co jest własnością paczki jako
  całości: manifest, niedający się wylistować katalog wpisów, limit liczby plików, kolizja id
  z inną paczką. Zduplikowany id wewnątrz paczki odrzuca **wszystkie** kolidujące pliki — żaden nie
  wygrywa po cichu, a rejestr nie dostaje dwóch wpisów pod jednym adresem.
* **Pliki niewczytane widoczne w rejestrze.** Lądują na końcu tej samej listy co wpisy, pod
  nagłówkiem `NIE WCZYTANE`, z komunikatem oprawiającym diagnostykę loadera zamiast karty.
  Rejestr złożony wyłącznie z zepsutych plików nie jest już „pusty".

**Domknięte 2026-09-12 (druga sesja):**

* **Zero zestawów treści jest awarią głośną.** Strażnik w `App.Initialize()` sprawdza sam warunek
  (pusta lista), nie pośrednika (który konstruktor zadziałał), więc łapie także korzeń kompozycji
  przekazujący `[]` jawnie. Stoi przed ładowaniem XAML-a, co czyni go sprawdzalnym testem bez
  uruchamiania Avalonii.
* **Stary mechanizm treści rozebrany** — dwanaście typów bez konsumenta zniknęło z repozytorium.
  Żadna referencja nie okazała się żywa; kompilator jest tego dowodem.
* **`code-map.md` przepisany od zera** wobec kodu, sekcja po sekcji. Przy okazji obalił własne
  twierdzenie z poprzedniej wersji: pole `Ruleset` w manifeście kampanii nigdy nie zniknęło,
  a `PackReference` nigdy nie istniał.

---

## Następne

Kolejność **nie** jest wiążąca — obie pozycje są niezależne.

### 1. Kampania wybiera zestawy przy zakładaniu

Rozstrzygnięte przez autora 2026-09-13, po tym jak wyszło, że biurko pokazujące okna wszystkich
wkompilowanych zestawów zrobi się bałaganem przy kilku systemach naraz.

Model: **wszystko jest zestawem, nie ma zestawu bazowego ani wyróżnionego rodzaju.** Zestaw może
zadeklarować, że wymaga innego zestawu — i wtedy „system" to zestaw, który nie wymaga niczego,
a „rozszerzenie" to zestaw, który wymaga jednego. Role czyta się z grafu zależności; **nie ma pola
z rodzajem zestawu**, bo pole z rodzajem jest tym, po czym zaczyna się rozgałęziać (ten sam kształt
został już raz usunięty z paczek — patrz `decisions.md`, „Rozdział na paczkę systemową i paczkę
treści"). Kampania zaznacza zestawy przy zakładaniu, wybór pilnuje zależności, a biurko pokazuje
okna wyłącznie zaznaczonych zestawów.

To uchyla część wcześniejszego rozstrzygnięcia „kampania wybiera system" — czym się różni i co
z tamtych argumentów nadal obowiązuje, jest w `decisions.md`.

**Pytanie otwarte, świadomie niezamknięte:** co rozszerzeniu wolno zobaczyć u zestawu, od którego
zależy. Dziś zestawy nie mogą się nawzajem referencjonować i pilnuje tego test, więc rozszerzenie
nie odczyta pól cudzego typu treści — może wnieść własne typy, własne okna i narzędzia czytające
neutralne kontrakty. Czy to wystarczy, rozstrzygnie **pierwszy prawdziwy drugi zestaw**, nie
rozmowa przed nim.

**Warto wiedzieć przed wyceną:** większość tego, co u innych bywa „rozszerzeniem", jest tutaj
**paczką, nie zestawem** — bestiariusz, nowe przedmioty, treść z dodatku to wpisy, czyli dane, i
działają dziś bez żadnej nowej maszynerii. Zestaw jest potrzebny dopiero na nowy kształt albo nowe
narzędzie.

**Sprawdź przed wykonaniem:** czy przy jednym wkompilowanym zestawie pole w kampanii i filtr na
biurku mają co robić. Filtr nie ma dziś czego odsiać, a dwa pola zostały z manifestu kampanii
usunięte właśnie za to, że nie miały konsumenta.

### 2. Usunięcie licznika i weryfikacja, czy `DataBlockShape` zarabia na siebie

**Wyzwalacz odpalił się 2026-09-13:** pierwsze prawdziwe narzędzie biurka istnieje. Licznik był
jawnym rusztowaniem i jedyną rzeczą używającą bloków danych.

Policzone przy okazji i nadal aktualne: mechanizm kształtu **nie umie opisać listy**, a każde
narzędzie z kolejką, drużyną albo składem potyczki zażąda jej jako pierwszej rzeczy. To jest realne
świadectwo w tamtym pytaniu.

Uwaga: usunięcie licznika zabiera ostatniego użytkownika bloków danych. Zanim to zrobisz,
rozstrzygnij, czy blok danych zostaje bez konsumenta — czy odchodzi razem z nim.

## Odłożone — dwie pozycje, obie o interfejsie

Obie czekają na to, aż autor zechce zaprojektować dla nich miejsce na ekranie. Żadna nie blokuje
wycinka powyżej.

1. **Odrzucone paczki nigdzie się nie pokazują.** Loader je odnotowuje, ekran rejestru ich nie
   wyświetla — świadoma decyzja autora z 2026-09-12. Jedyna pozycja z tabeli „Co się dzieje, gdy
   treść jest zepsuta", o której Mistrz Gry nie dowiaduje się z aplikacji: paczka odrzucona za
   literówkę w manifeście znika dziś po cichu.
2. **Nagłówek `NIE WCZYTANE` czyni pierwszy zepsuty wiersz wyższym od pozostałych**, bo niesie go ten
   wiersz, a nie prawdziwy nagłówek sekcji. Cena za „jedna lista, jeden szablon", zostawiona
   świadomie 2026-09-13.

Cztery pozycje, które stały tu wcześniej, zostały **zamknięte** 2026-09-13 po sprawdzeniu przesłanek
— testy sekwencji startowej, skala odstępów, domknięcie nad powłoką przy starcie i podwójna nazwa
pliku w komunikacie o odrzuceniu. Uzasadnienia zamknięcia żyją w `decisions.md` i `architecture.md`;
tutaj nie wracają, bo kolejka ma mówić, co dalej, a nie prowadzić archiwum.

---

## Archiwum

`archive/pre-pivot-content-layer` (`f5d5b7a`) — **nigdy nie scalana**. Praca sprzed
przeprojektowania warstwy treści: `SummaryContract`, `ContractFill`,
`SummaryContractResolution`, grupowanie rejestru, rozbudowane testy loadera i fixture'y.
Trzy fixture'y wpisów zostały stamtąd przywrócone; resztę trzyma się tylko po to, żeby nic
nie zginęło.
