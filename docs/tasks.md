# DungeonApp — kolejka pracy

**Status: stan na 2026-09-14.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
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

Gałąź: `master`. Build bez ostrzeżeń, 343 testy zielone.

---

## Gdzie jesteśmy

Kroki 1–7 z sekcji „Kolejność prac" [architecture.md](architecture.md) są **zrobione**; krok 8 jest
w połowie — pierwsze prawdziwe narzędzie biurka istnieje, ale usunięcie licznika i weryfikacja
pytania o kształt bloku danych stoją niżej jako pozycja 2. Było to przejście z szablonów jako plików
danych na skompilowane typy treści, a następnie osadzenie treści w konkretnej kampanii. Stan, do
którego to doprowadziło:

* **Typy treści są kodem.** Zestaw `DungeonApp.Content.Dnd5e` niesie `Monster` i `Gear`, ich
  zaprojektowane karty i jedno okno biurka. Deserializator jest jedynym walidatorem.
* **Granice są pilnowane mechanicznie**, nie deklarowane — cztery testy architektoniczne, w tym skan
  słownictwa, który poszerza się sam wraz z przybywającymi typami treści.
* **Kampania ma własny świat.** Instancje z rzadką łatką, zapis na dysk w jednej transakcji
  z blokami danych, rozwiązywanie wskazania wobec rejestru jako osobna warstwa odczytu.
* **Zestaw treści wnosi własne okno biurka** — mechanizm pasa narzędzi. Okno „Świat kampanii" jest
  pierwszym prawdziwym narzędziem biurka i pierwszym konsumentem magazynu instancji.
* **Jeden prymityw zapisu atomowego** zamiast trzech ręcznie pisanych kopii.

Szczegóły każdej z tych rzeczy — [code-map.md](code-map.md).

---

## Następne

Kolejność **nie** jest wiążąca — obie pozycje są niezależne.

### 1. Kampania wybiera zestawy przy zakładaniu

Rozstrzygnięte przez autora 2026-09-13, po tym jak wyszło, że biurko pokazujące okna wszystkich
wkompilowanych zestawów zrobi się bałaganem przy kilku systemach naraz.

Model stoi w `architecture.md`, „Narzędzia biurka i system okien". Czym różni się od odrzuconego
wcześniej wariantu „kampania wybiera system" i co z tamtych argumentów nadal obowiązuje — w
`decisions.md`, „Kompilator zna listę systemów RPG, kampania wybiera jeden".

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

**Druga przesłanka, świeższa:** okno „Świat kampanii" **nie jest** narzędziem domenowym i nie używa
żadnego bloku danych. Kontrakt narzędzia domenowego nie dostał więc dotąd ani jednej weryfikacji
poza licznikiem — po jego usunięciu nie będzie miał żadnej.

---

## Odłożone — trzy pozycje o interfejsie

Wszystkie czekają, aż autor zechce zaprojektować dla nich miejsce na ekranie. Żadna nie blokuje
wycinka powyżej.

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
