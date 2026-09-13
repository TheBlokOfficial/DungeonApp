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

Gałąź: `master`. Build i 280 testów zielonych.

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

## Następne — jedna pozycja, i jest pilna

**Magazyn instancji stoi w połowie i nie ma konsumenta.** Fundament wszedł 2026-09-13: koperta umie
arytmetykę nakładki, istnieją identyfikator instancji i jej rekord. **Nie istnieje** kolekcja
instancji w kampanii, ich zapis na dysk, rozwiązywanie wskazania na wpis wobec rejestru ani żadne
miejsce w interfejsie, z którego dałoby się instancję zobaczyć albo zmienić.

To jest **nazwany dług, nie stan pośredni**. `architecture.md` w sekcji „Pytania otwarte" wylicza
sześć rzeczy zbudowanych bez konsumenta; wszystkie sześć umarło nieużytych, i z tego wzięła się
reguła **nic nie wchodzi bez konsumenta w tym samym wycinku**. To, co leży dziś w repozytorium, jest
siódmym przypadkiem — z tą jedyną różnicą, że jest zapisany tutaj, zanim zdążył się zestarzeć po
cichu. Sesja skończyła się na budżecie, nie na rozstrzygnięciu.

Wycinek domyka się w tej kolejności, i **ma być domknięty w jednym podejściu**:

1. **Kolekcja instancji w kampanii** — dodanie, usunięcie, podmiana łatki, zmiana nazwy własnej,
   każde ze zdarzeniem niosącym sam identyfikator. Wzór: `CampaignDataBlocks`, łącznie z rozdziałem
   na `Create` i `Hydrate`. **Magazyn nie wie nic o rejestrze treści** — rozstrzygnięte 2026-09-13:
   rozwiązanie wskazania jest sprawą odczytu, nie zapisanego stanu, bo zainstalowanie brakującej
   paczki ma naprawiać instancję bez przepisywania kampanii.
2. **Zapis** — plik na instancję w podkatalogu kampanii, w tej samej transakcji i generacji co bloki
   danych, przez istniejący `AtomicWrite`, z manifestem lądującym ostatnim. Uwaga na jedyne miejsce,
   gdzie może zostać śmieć: `AtomicWrite` przenosi pliki, ale nie kasuje pliku po usuniętej
   instancji.
3. **Rozwiązywanie wskazania wobec rejestru** — osobna warstwa odczytu. Instancja bez paczki, bez
   wpisu albo taka, której scalone wartości zestaw treści odrzuci, ma zostać **oznaczona**: łatka
   nietknięta, kampania otwiera się dalej.
4. **Konsument** — okno biurka z instancjami tej kampanii i jednym polem do zmiany. Pierwszym
   edytowalnym polem są **bieżące punkty życia**, zatwierdzone przez autora 2026-09-13, a razem
   z nimi odpowiedź na pytanie z tabeli niżej: punkty życia i pancerz **nie** są parami
   wartość-plus-źródło. Typ treści deklaruje dwa zwykłe pola — wpis wypełnia maksimum, nakładka
   niesie bieżące — a mechanizm scalania zostaje głupi.

Poza wycinkiem: sloty, zagnieżdżanie, deklaracja paczek przez kampanię, formuły, dokument.

**Dopiero po nim:** usunięcie licznika i weryfikacja, czy `DataBlockShape` nadal zarabia na siebie.
Licznik zostaje do tego czasu świadomie — jest dziś jedyną rzeczą używającą bloków danych, więc jego
usunięcie należy do narzędzia trzymającego własny stan, a nie do tego wycinka. Policzone przy
okazji: mechanizm kształtu **nie umie dziś opisać listy**, a każde narzędzie z kolejką, drużyną albo
składem potyczki zażąda jej jako pierwszej rzeczy. To jest realne świadectwo w tamtym pytaniu.

---

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
